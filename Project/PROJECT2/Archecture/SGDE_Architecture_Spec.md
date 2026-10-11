# SGDE Architecture Specification

-   **Task:** SGDE-ARCH-001
-   **Status:** **PROPOSED --- NOT FROZEN**
-   **Role:** Architecture definition / RTL handoff preparation
-   **Scope:** Sprite Game Display Engine (SGDE), sprite ROM interface,
    frame-buffer interface, overlap/status logic, compositing and
    control.
-   **Authority note:** This document is a proposal based on the SGDE
    top-level shell, the supplied `SR.v`, `FB.v`, `testfixture.v`, and
    supplemental project notes. It is not a substitute for the complete
    Project/Architecture/Interface/Clock-Reset/Verification
    specifications. Conflicts and missing decisions are listed in
    `SGDE_Architecture_Decision_Log.md`. No unresolved item is silently
    promoted to an approved requirement.

## 1. Goals and non-goals

SGDE accepts a sequence of sprite descriptors, reads sprite pixels from
a synchronous ROM, determines Man status from valid mask overlaps,
composites the scene, writes a 64×64 RGB frame to a 4096×12 frame
buffer, and signals completion.

The architecture must preserve the existing top-level interface exactly.
It must not add ports, change port widths, change the memory model,
assume unprovided clock constraints, or claim synthesis/timing/physical
results without tool evidence.

## 2. Existing top-level interface (must not change)

``` verilog
module SGDE (
  ready, done, clk, reset, sprite, start, type, X, Y,
  SR_CEN, SR_A, SR_Q, FB_CEN, FB_WEN, FB_A, FB_D, FB_Q
);
input clk, reset, sprite, start;
input [1:0] type;
input [5:0] X, Y;
input [12:0] SR_Q;
input [11:0] FB_Q;
output ready, done;
output SR_CEN, FB_CEN, FB_WEN;
output [8:0] SR_A;
output [11:0] FB_A, FB_D;
endmodule
```

`ready`/`done` are scalar status outputs. `SR_A` is 9-bit and `SR_Q` is
13-bit. `FB_A`/`FB_D`/`FB_Q` are 12-bit. The precise input handshake,
reset polarity, and cycle-level meaning of `ready` and `done` still
require confirmation (see Decision Log).

## 3. Evidence and certainty labels

-   **Provided fact:** directly present in supplied
    interface/model/notes.
-   **Derived:** follows from provided widths, capacity, or synchronous
    model behavior.
-   **Proposal:** candidate microarchitecture requiring owner approval.
-   **Open:** cannot be settled from available evidence.

The `SR.v` and `FB.v` models include timing checks and a 1 ns
clock-to-output delay, with 3 ns clock-period checks and 1 ns
pulse-width checks in the supplied model excerpts. These model checks
are not evidence that the final design meets any selected clock target.

## 4. Proposed module decomposition

  ------------------------------------------------------------------------------
  Module / block                 Responsibility          Key state or interface
  ------------------------------ ----------------------- -----------------------
  `input_frontend`               Capture each descriptor Descriptor registers;
                                 `{X,Y,type}` when the   object count/index
                                 external handshake says 
                                 it is valid; count      
                                 descriptors             

  `operation_controller`         Top-level FSM,          FSM, descriptor index,
                                 `ready`/`done`, phase   pixel/scan counters
                                 sequencing, counters,   
                                 memory enables          

  `sprite_address_gen`           Map sprite type and     Sprite base/format
                                 local pixel coordinate  mapping; local x/y
                                 to ROM address          

  `sprite_rom_sequencer`         Drive `SR_CEN/SR_A`,    Request/address
                                 account for synchronous pipeline, valid
                                 read and output latency tracking

  `scene_descriptor_store`       Retain accepted object  Descriptor array or
                                 descriptors for later   equivalent storage
                                 scene-wide              
                                 analysis/compositing    

  `overlap_status_engine`        Compute                 Man-live-mask samples,
                                 absolute-coordinate     overlap flags
                                 mask intersections and  
                                 aggregate Man status    

  `pixel_compositor`             Select the visible      Candidate pixel, mask,
                                 pixel according to      priority/order
                                 approved layer priority 
                                 and transparency        

  `framebuffer_writer`           Generate `FB_A`,        Linear address, write
                                 `FB_D`, `FB_WEN`,       schedule
                                 `FB_CEN` for all 4096   
                                 pixels                  

  `background/pixel_scheduler`   Ensure every            Pixel address and
                                 frame-buffer location   write-valid
                                 receives either         
                                 background or final     
                                 composited RGB          

  `protocol/status`              Enforce legal           `ready`, `done`
                                 acceptance and          generation
                                 completion pulses       
  ------------------------------------------------------------------------------

These are logical responsibilities, not a mandate for one RTL module per
row. Combining blocks is permitted if behavior, observability, and
verification obligations remain equivalent.

## 5. Proposed high-level data/control flow

1.  **Accept descriptors:** While the input interface is ready, capture
    each asserted `sprite` descriptor (`X`, `Y`, `type`) exactly once
    according to the handshake clarified by the owner. Retain all
    descriptors needed for global overlap analysis and final
    composition.
2.  **Close input:** On an explicitly defined end-of-list event
    (candidate: `start`), stop descriptor acceptance and freeze the
    scene list.
3.  **Prepare scene:** Establish a defined background policy and
    initialize any per-scene status accumulators.
4.  **Analyze Man status:** Iterate through relevant sprite
    pixels/coordinates, apply mask rules, and aggregate Candy/Ghost
    overlap flags. Do not infer status from final visible RGB pixels.
5.  **Composite frame:** Visit each framebuffer coordinate in a
    deterministic order; determine all covering objects, apply each
    object's own transparency mask, resolve layer priority/order, and
    choose final RGB.
6.  **Write frame:** Write exactly one final RGB value to each of 4096
    framebuffer addresses, unless an explicitly approved
    read-modify-write strategy is required.
7.  **Complete:** Assert `done` according to a cycle-accurate protocol
    approved by the owner; ensure it cannot be mistaken for a level that
    remains high indefinitely.

The order of status analysis and composition may be shared or
interleaved only if it preserves the invariant that final Man status is
based on the complete scene, not a partial prefix of input descriptors.

## 6. FSM proposal

The following states are a scheduling proposal, not approved cycle
counts.

  -------------------------------------------------------------------------------
  State                Entry condition   Main behavior          Exit condition
  -------------------- ----------------- ---------------------- -----------------
  `IDLE`               Reset released    Advertise `ready` only First valid
                       and no operation  when a new             descriptor or
                       active            descriptor/operation   operation-start
                                         may legally be         event
                                         accepted               

  `LOAD_DESC`          Descriptor        Capture descriptor;    More descriptors
                       handshake         increment descriptor   expected, or
                       accepted          count; validate        end-of-list event
                                         supported              
                                         type/coordinates if    
                                         required               

  `SEAL_SCENE`         Input list closed Freeze descriptor      Internal
                                         list; clear overlap    initialization
                                         accumulators and scan  complete
                                         indices                

  `STATUS_INIT`        Status pass       Initialize             Ready to scan
                       begins            Man-live-mask and      
                                         Candy/Ghost aggregate  
                                         flags                  

  `STATUS_SCAN_REQ`    Need next status  Set ROM address and    Request launched
                       pixel             chip enable for the    
                                         selected object/local  
                                         coordinate             

  `STATUS_SCAN_WAIT`   Synchronous ROM   Wait through ROM       Data is valid at
                       request launched  edge/access delay;     the designated
                                         maintain address/valid sampling edge
                                         alignment              

  `STATUS_ACCUM`       Status pixel      Apply Man live mask    More samples or
                       valid             and target object's    status pass
                                         original mask; update  complete
                                         aggregate overlap      
                                         flags                  

  `COMPOSE_INIT`       Status pass       Initialize frame scan  First pixel
                       complete          address and            scheduled
                                         background/write       
                                         policy                 

  `COMPOSE_REQ`        Next output pixel Resolve covering       ROM request
                       needed            objects and launch     launched
                                         required ROM reads     

  `COMPOSE_WAIT`       ROM request       Wait for valid sprite  Pixel data valid
                       outstanding       data; align metadata   
                                         with returned pixel    

  `COMPOSE_RESOLVE`    All required      Apply                  Pixel result
                       candidate data    mask/transparency and  ready
                       available         approved layer         
                                         priority/order; select 
                                         final RGB              

  `FB_WRITE`           Final pixel       Drive FB               Write accepted;
                       available         address/data/write     advance address
                                         controls with correct  
                                         setup before active    
                                         edge                   

  `DONE`               All 4096          Pulse `done` according Return to `IDLE`
                       addresses have    to approved protocol;  
                       been written and  deassert busy/ready as 
                       status is final   specified              
  -------------------------------------------------------------------------------

### 6.1 `ready` and `done`

-   **Open:** The shell alone does not define whether `ready` means "can
    accept one descriptor," "accepting descriptor stream," or "idle and
    can accept an operation."
-   **Open:** The shell alone does not define whether `start` closes the
    descriptor list, launches composition, or both, nor whether `start`
    may overlap the final descriptor.
-   **Proposal:** Use a single, documented acceptance condition and
    capture each descriptor only on a clock edge where the condition is
    true. Avoid implicit event controls or hidden multi-cycle
    handshakes.
-   **Proposal:** `done` should be a one-clock pulse after the final
    framebuffer write has been accepted. This is not approved until the
    interface specification confirms it.
-   **Required:** Define whether `ready` is high during `DONE`, and when
    the next scene may be accepted.

## 7. Sprite ROM access architecture

### 7.1 Supplied model facts

-   `SR_A[8:0]` addresses a 384×13 ROM (`SR_Q[12:0]`).
-   The ROM is synchronous: on `posedge CLK`, it latches `CEN` and
    address; when the latched chip enable indicates read, it updates the
    internal output register from the selected memory word. The supplied
    model has a 1 ns clock-to-output delay and setup/hold checks.
-   `CEN` is active-low in the supplied model convention: `CEN=0`
    performs a read; `CEN=1` holds the output. This polarity must be
    preserved.
-   Pixel word width is 13 bits. The 12-bit RGB value and one-bit
    transparency mask must be extracted according to the authoritative
    sprite-format definition. Supplemental notes identify bit 0 as the
    mask and bits `[12:1]` as the 12-bit pixel payload; confirm the RGB
    bit ordering with the project specification before freezing.
-   ROM initialization is external through
    `$readmemb("SR_verilog.rcf", mem)`; that file is required for
    simulation.

### 7.2 Address and latency rules

-   The exact mapping from `type`, sprite identity, and local `(u,v)` to
    ROM address must be documented. The ROM has 384 entries total; do
    not assume a particular sprite tile size or base-address map without
    the sprite layout specification.
-   Set `SR_A` and `SR_CEN` early enough to meet model setup/hold checks
    at the sampling edge.
-   Because `SR_Q` changes after a rising edge with a modeled 1 ns
    delay, logic must not treat it as a zero-delay combinational
    response to a newly assigned address.
-   The proposed safe schedule is: drive address/chip-enable in the
    cycle before the read edge; regard data as available only after that
    edge plus the specified access delay; sample/consume it at a later
    edge or otherwise prove a safe timing window. A next-rising-edge
    sample is a conservative candidate, not a finalized cycle count.
-   Pipeline metadata (object id, local coordinate, destination
    framebuffer address, mask/status purpose, and layer rank) alongside
    the ROM request so returned data cannot be paired with the wrong
    pixel.
-   Do not issue a new request if doing so would overwrite the only
    needed result before it is consumed, unless an explicitly verified
    pipeline is used.

## 8. Frame Buffer access architecture

### 8.1 Supplied model facts

-   `FB` is a synchronous single-port 4096×12 RAM with 12-bit
    address/data/output.
-   `CEN` and `WEN` are scalar active-level controls interpreted by the
    supplied model; preserve the exact model semantics. The testbench
    reads through `CEN=0`, `WEN=1` (as driven by its readback override),
    then compares `FB_Q` with expected data.
-   The supplied model has a 1 ns clock-to-output delay, setup/hold
    checks, a 3 ns clock-period check, and 1 ns pulse-width checks.
-   A write is accepted only according to the model's edge-sampled
    control/address/data behavior. Do not assume a write occurs
    immediately when `FB_A` or `FB_D` changes.
-   The top-level shell has no independent FB address/data read mux:
    `FB_A`, `FB_D`, `FB_CEN`, and `FB_WEN` are directly controlled by
    SGDE, while the testbench may override FB address and controls for
    readback after `done`.

### 8.2 Addressing and full-frame guarantee

-   Frame size is 64×64 = 4096 pixels.
-   Proposed row-major address: `FB_A = y*64 + x`, equivalently
    `{y[5:0], x[5:0]}`. This is a derived candidate and must be
    confirmed against the golden-image layout; the testbench iterates
    linear addresses and derives coordinates with `x=j%64`,
    `y=floor(j/64)`.
-   Background RGB is specified by supplemental notes as
    `12'b110011110000`. Every address must contain a defined final RGB
    value at completion.
-   Two candidate strategies:
    -   **Full overwrite (preferred baseline):** calculate final RGB for
        every address, selecting background when no visible sprite
        covers the coordinate. This avoids reading old pixels and
        guarantees all 4096 locations are initialized by the current
        operation.
    -   **Background prefill + sprite updates:** write background to all
        addresses, then write covered sprite pixels. This is easier for
        some raster pipelines but costs extra writes.
-   Do not depend on uninitialized SRAM contents. No framebuffer read is
    needed for full overwrite; if a read-modify-write approach is
    chosen, it must explicitly model the single-port read latency and
    schedule the later write.
-   The `FB_Q` output is not a combinational reflection of `FB_A`;
    account for synchronous read and output delay during any internal
    readback use.

## 9. Overlap and layer semantics

### 9.1 Man status calculation

Supplemental project notes specify the following candidate
interpretation: - Man's **live mask** is the sole mask basis for
deciding whether Man overlaps another object. - Other object masks
(Ghost/Candy/Flower) use their original masks. - A valid overlap exists
when the relevant mask=1 regions intersect at the same absolute screen
coordinate. - The status decision is based on geometric/mask
intersection, not on whether the other object is visible after
compositing. - If the completed scene contains valid overlaps with both
Candy and Ghost, the final Man state is happy.

Still requiring owner confirmation: - Exact encodings for Man's possible
states. - Whether Candy-only and Ghost-only have unique states and which
takes priority if both are present. - Whether multiple instances of
Candy/Ghost are aggregated with logical OR, counts, or another rule. -
Whether an object type can occur more than once and how out-of-bounds
coordinates are treated.

A safe accumulator structure is `has_candy_overlap` and
`has_ghost_overlap`, each initialized once per scene and set sticky when
a valid overlap is found. Derive the final state only after the entire
relevant scan completes. This is a proposal; it does not settle the
state encoding or priority.

### 9.2 Final pixel compositing

-   Status evaluation and final compositing are separate functions.
-   Each object's original transparency mask determines whether its own
    pixel contributes to the composite, except where the approved
    sprite-format specification explicitly says otherwise.
-   At a given screen coordinate, choose the visible pixel using an
    approved layer priority. The supplemental notes say input order
    defines layer ordering, but the direction (later object over
    earlier, or the reverse) must be confirmed.
-   Man live mask must not accidentally replace Man's original mask for
    visual compositing unless explicitly required by the image
    specification.
-   Ghost/Candy/Flower use their original masks.
-   If all masks indicate transparent or no object covers a coordinate,
    output background RGB `12'b110011110000`.
-   If multiple opaque objects cover a coordinate, choose exactly one
    according to the approved priority/order. Do not merge RGB channels
    unless specified.

## 10. Storage strategy proposal

Minimum logical information required: - All accepted object descriptors
until both status and composition no longer need them. - Current scan
coordinate and pixel address. - ROM request metadata sufficient to align
each `SR_Q` word to its consumer. - Sticky overlap flags and final Man
state. - Optional row/column or object-local coordinate counters.

Descriptor count, maximum supported object count, duplicate object
policy, and sprite dimensions are not fully specified. The testbench
parameter `n_sprite_max=20` declares `data [0:n_sprite_max]`, but this
alone does not prove the legal maximum scene count. The actual pattern
convention and project spec must settle the limit.

A full 4096×12 on-chip pixel buffer would require 49,152 bits (excluding
control/metadata) if implemented in registers; it is not required if the
design streams final pixels directly into the supplied framebuffer. A
descriptor store may be smaller, but its exact size depends on the
approved maximum object count.

## 11. Timing, performance and physical risks

No official target clock, SDC, PVT corner, library, area objective
formula, or synthesis/STA report is available in the supplied context.
Therefore no numeric maximum-frequency, area, or signoff claim is made.

Cycle count depends on: - sprite dimensions and ROM address mapping; -
number of objects and whether each is scanned independently; - whether
ROM reads can be pipelined; - whether status and compositing share pixel
scans; - whether background is prefilled or integrated into a full-frame
overwrite; - exact input handshake and done protocol.

Timing risks: - address/control-to-ROM setup and ROM clock-to-output
delay; - pixel metadata alignment through the synchronous ROM; - layer
selection mux depth; - overlap comparison and coordinate arithmetic; -
high-fanout enables/reset/control; - frame-buffer write setup/hold; -
target period potentially below the supplied memory model's 3 ns period
check.

These are risk observations, not measured critical paths.

## 12. Proposed invariants

1.  The top-level port list and widths remain unchanged.
2.  Every accepted descriptor is captured exactly once.
3.  No descriptor is silently dropped or duplicated.
4.  Each ROM result is consumed with the metadata of its request.
5.  ROM reads occur only with the model's active read control and legal
    address.
6.  Every framebuffer write address is in `[0,4095]`.
7.  On successful completion, every address `[0,4095]` contains a
    defined final RGB value for the current scene.
8.  Every uncovered/fully transparent pixel resolves to the specified
    background color.
9.  Man status is based on the complete scene's valid mask
    intersections, not the input prefix.
10. Status calculation does not depend on final layer occlusion unless
    the owner explicitly specifies that behavior.
11. Final compositing chooses one visible RGB value according to the
    approved layer order.
12. `done` cannot assert before all required framebuffer writes have
    been accepted.
13. `ready` and `done` follow the approved cycle-level protocol and do
    not depend on simulator scheduling races.
14. No framebuffer content is assumed valid before the current operation
    initializes or overwrites it.

## 13. RTL handoff restrictions

Do not change the top-level interface. Do not invent sprite
dimensions/address maps, status encodings, maximum descriptor count,
reset polarity, or layer priority. Do not assume asynchronous ROM/RAM
behavior. Do not introduce a framebuffer read-modify-write dependency
without a cycle-accurate schedule. Do not add a clock target or claim
timing/area results absent project constraints and EDA reports.

## 14. Freeze checklist

Architecture remains **PROPOSED / NOT FROZEN** until the project owner
resolves the blocking issues in the Decision Log, especially input
protocol, reset polarity, sprite ROM map/format, Man state truth table,
layer ordering, and completion timing. A human/project-owner approval is
required before marking the architecture FROZEN.
