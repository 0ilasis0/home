# SGDE Architecture Trade-offs

-   **Task:** SGDE-ARCH-001
-   **Status:** **PROPOSED --- NOT FROZEN**
-   **Purpose:** Compare viable implementation strategies. All numbers
    below are analytical counts or explicitly labeled estimates, not EDA
    results.

## 1. Assumptions and limits

Known: - Display is 64×64 = 4096 pixels. - Each framebuffer pixel is
12-bit RGB. - Sprite ROM model is synchronous, 384×13, with 1 ns
clock-to-output and a 3 ns period check in the supplied model excerpt. -
Framebuffer is synchronous single-port 4096×12, with 1 ns
clock-to-output and a 3 ns period check in the supplied model excerpt. -
Background RGB is `12'b110011110000`. - Top-level interface is fixed.

Unknown: - Exact sprite dimensions/address map and number of ROM pixels
per object. - Maximum legal descriptor count. - Official clock target,
SDC, target library/PVT, memory macro implementation and scoring
formula. - Exact handshake, status truth table, layer priority
direction, and reset contract.

Therefore cycle formulas use `P` for total pixel/object samples required
by a chosen algorithm, `N` for accepted descriptors, and `L` for
effective ROM request-to-consume latency in cycles. No absolute latency
or area is claimed.

## 2. Candidate A --- Full-frame overwrite with descriptor store and streamed composition

### Structure

-   Store the accepted descriptor list.
-   Perform a status pass (or a shared scan that is proven to preserve
    final-scene semantics).
-   Raster scan all 4096 screen coordinates.
-   For each coordinate, identify covering sprites, fetch needed sprite
    words through the synchronous ROM, resolve transparency/layer
    priority, and write one final 12-bit value to the framebuffer.
-   Emit background for coordinates with no visible opaque sprite.

### Storage estimate

-   Framebuffer storage is external: 4096×12 = **49,152 bits** in the
    supplied RAM model.
-   A full internal frame buffer is not needed.
-   Descriptor storage is proportional to `N`; exact bit count depends
    on the object ID/type, origin, size/map and count format.
-   A small request metadata pipeline is required to align ROM results
    to screen coordinate/object.

### Cycle model

-   Minimum write count: **4096 accepted framebuffer writes** per frame.
-   If one final pixel is produced per cycle after pipeline fill, the
    composition/write phase lower bound is approximately
    `4096 + pipeline_fill` cycles, but ROM latency, multi-object lookup,
    and any bubbles can increase it.
-   If each object must be scanned independently for status, status work
    is proportional to the total mask samples examined. Let that count
    be `P_status`; status cycles are at least the required number of ROM
    reads and may be more if reads are not pipelined.
-   Total:
    `C_A ≈ C_input(N) + C_status(P_status,L) + C_compose(4096,N,L) + C_control`.
    This is a symbolic estimate only.

### Advantages

-   Every framebuffer address is defined in the current operation.
-   No dependency on old framebuffer contents and no internal 49,152-bit
    pixel store.
-   Simple final-frame golden comparison and robust recovery from
    arbitrary RAM initialization.
-   Background handling is local to final pixel selection.

### Risks / costs

-   Per-screen-pixel determination of which sprites cover a coordinate
    may be expensive if implemented as a large combinational scan over
    descriptors.
-   Multiple overlapping objects can require several ROM reads per
    screen coordinate unless the address/data path is carefully
    scheduled.
-   Layer selection and coordinate transforms can create critical paths.
-   Exact latency depends strongly on ROM layout and pipeline policy.

## 3. Candidate B --- Background prefill followed by object-oriented drawing

### Structure

1.  Write background to all 4096 addresses.
2.  Traverse objects in approved layer order.
3.  For each object pixel, fetch ROM data and write only opaque/valid
    pixels at absolute screen coordinates.
4.  Run status analysis either as a separate pass or in parallel with
    object traversal, provided the complete-scene rule is preserved.

### Storage estimate

-   External framebuffer still holds 49,152 bits.
-   Descriptor storage and ROM metadata are similar to Candidate A.
-   No full internal framebuffer is required.

### Cycle model

-   Background writes: **4096 writes**.
-   Sprite writes: up to `P_visible_candidates` writes, fewer if
    transparent pixels are skipped.
-   ROM reads: approximately the number of object pixels scanned,
    `P_objects`, with effective throughput depending on whether one
    request can be launched every cycle.
-   Total:
    `C_B ≈ C_input(N) + 4096 + C_objects(P_objects,L) + C_status + C_control`.
-   If final composition requires later objects to overwrite earlier
    objects, traversal order must match the approved layer direction.

### Advantages

-   Natural object-oriented painter's algorithm when input order defines
    layering.
-   Transparent pixels need not cause framebuffer writes.
-   Address generation may be simpler than testing every object at every
    screen coordinate.
-   Easy to reason about background as a guaranteed baseline.

### Risks / costs

-   Adds at least 4096 background writes in addition to sprite writes.
-   Correctness depends on background prefill completing before any
    operation assumes valid framebuffer content.
-   Status still must aggregate all relevant objects; drawing order must
    not prematurely finalize Man state.
-   A sprite whose geometry extends outside the screen needs approved
    clipping behavior.

## 4. Candidate C --- Full internal 64×64 pixel buffer, then bulk framebuffer write

### Structure

-   Render/composite into an internal 4096×12 array or memory.
-   Once complete, stream the full internal buffer into the external
    framebuffer.

### Storage estimate

-   Pixel storage alone: `4096 × 12 = 49,152 bits` (6 KiB), excluding
    memory overhead, ports, and control.
-   If implemented as flip-flops, area may be large; if mapped to SRAM,
    a suitable memory macro and access constraints are required and are
    not provided.

### Cycle model

-   Render phase: depends on object pixels/overlap and chosen algorithm.
-   Flush phase: at least **4096 writes** to external framebuffer.
-   Total: `C_C ≈ C_render + 4096 + C_control`.
-   This can separate render scheduling from external writes, but it
    does not remove the need to obey synchronous ROM latency.

### Advantages

-   Decouples composition from external framebuffer write scheduling.
-   May simplify multiple updates to one pixel and layer-order
    resolution.
-   Final frame can be checked in internal storage before committing.

### Risks / costs

-   Significant storage area if no suitable SRAM is available.
-   May add a second full-frame memory structure and complicate
    synthesis/physical design.
-   No supplied requirement demands atomic frame commit, so this
    buffering may be unnecessary.
-   Exact memory inference and port semantics must be proven from the
    synthesis flow.

## 5. Comparison table

  ---------------------------------------------------------------------------------
  Dimension         Candidate A: full       Candidate B:      Candidate C: internal
                    overwrite stream        background +      full-frame buffer
                                            object draw       
  ----------------- ----------------------- ----------------- ---------------------
  External writes   Exactly 4096 baseline   4096 background   At least 4096 flush
                    writes if one final     writes + opaque   writes, plus internal
                    write per pixel         sprite writes     writes

  Extra 49,152-bit  Not required            Not required      Required unless an
  storage                                                     approved memory macro
                                                              is used

  Old FB contents   No                      No, after prefill No
  required                                                    

  Transparent pixel Select                  Skip write for    Resolve in internal
  handling          background/underlying   transparent       buffer
                    object in compositor    object pixel      

  Layer order       Explicit priority       Natural painter's Explicit priority or
                    resolution per          traversal if      painter's traversal
                    coordinate              order is approved 

  ROM traffic       May repeat reads for    One scan per      Depends on internal
                    overlapping objects     object pixel in   render method
                                            typical           
                                            implementation    

  Control           Medium to high          Medium            High storage/control
  complexity                                                  integration

  Potential         Coverage checks +       Address/clip +    Internal
  critical path     priority mux + RGB      mask/write        composition + memory
                    select                  control           interface

  Main risk         Multi-object            Extra cycles and  Area/memory inference
                    coverage/ROM scheduling incomplete        and physical
                                            prefill           feasibility

  Current           **Preferred baseline,   Viable            Use only if
  recommendation    conditional**           alternative       buffering/atomicity
                                                              requirement justifies
                                                              it
  ---------------------------------------------------------------------------------

## 6. Area and performance estimate policy

No gate count, cell area, maximum frequency, power, or physical area
estimate is reported because the target library, PVT, synthesis
constraints, macro availability and score formula are unknown. The only
quantitative estimates are exact capacity/write counts and symbolic
cycle formulas.

Before comparing PPA: 1. Obtain official `SDC`/clock target, synthesis
setup and target library/PVT. 2. Confirm whether the supplied
`SR.v`/`FB.v` are behavioral timing models or correspond to the actual
macros used for implementation. 3. Read the actual `synopsys_dc.setup`,
`SGDE.tcl`, MMMC and physical collateral rather than inferring their
contents from filenames. 4. Define the scoring formula, including any
frequency, area, power or cycle-count terms. 5. Run the same constraints
and evidence level for each candidate.

## 7. Recommendation

**Candidate A is the provisional baseline** because it guarantees every
framebuffer address is assigned a final value without depending on prior
RAM contents, while avoiding a 49,152-bit internal frame buffer and
avoiding a mandatory background prefill pass. This recommendation is
conditional on confirming that a raster-oriented compositor can access
the sprite ROM map efficiently enough.

Candidate B may be preferable if the ROM layout and sprite dimensions
strongly favor object-oriented scans and the cycle budget allows the
extra 4096 background writes. Candidate C should not be selected without
a concrete requirement for full-frame staging/atomic commit or evidence
that its storage maps efficiently to an available memory macro.

This is an architectural trade-off recommendation only, not an
EDA-verified area/timing optimum.
