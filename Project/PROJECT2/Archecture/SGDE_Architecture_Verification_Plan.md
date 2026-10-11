# SGDE Architecture Verification Plan

-   **Task:** SGDE-ARCH-001
-   **Status:** **PROPOSED --- NOT FROZEN**
-   **Purpose:** Define verification obligations for the architecture
    and the eventual RTL. This is a plan, not evidence that any
    simulation, lint, synthesis, CDC, STA, or physical check has run.

## 1. Verification layers and evidence labels

-   **Reasoned:** reviewed logically against the architecture.
-   **Simulated:** demonstrated by a reproducible simulation with
    recorded inputs, outputs, and logs.
-   **Lint-Verified:** passed the selected lint tool/configuration.
-   **Synthesized:** passed the selected synthesis flow.
-   **Timing-Verified:** passed STA with identified
    constraints/library/corners.
-   **Physically Verified:** relevant implementation checks (such as
    placement/routing/DRC/LVS) passed with reports.

Do not use a higher evidence label based on a lower one. The current
architecture has only reasoned/proposed content; RTL behavior is **NOT
VERIFIED**.

## 2. Interface protocol checks

After the owner approves the handshake, verify: 1. A descriptor is
captured exactly once only on the specified acceptance edge. 2. `X`,
`Y`, and `type` are sampled from the same descriptor transaction. 3.
`sprite` held high for multiple cycles follows the defined protocol and
does not cause accidental duplicate capture. 4. `ready` deassertion
prevents unaccepted descriptors from being consumed. 5. `start` is
accepted/rejected in every defined FSM phase as specified. 6.
Simultaneous `sprite` and `start`, including the final descriptor,
matches the approved rule. 7. `done` pulse width and its relation to the
final framebuffer write match the interface spec. 8. Reset during idle,
descriptor loading, ROM scan, composition, and framebuffer write follows
the approved reset/abort contract. 9. No illegal state transition or
counter wrap causes an unintended memory access.

## 3. Memory model protocol checks

### 3.1 Sprite ROM

-   Check `SR_CEN` polarity against the actual supplied `SR.v`.
-   Ensure address and enable meet model setup/hold requirements.
-   Verify `SR_A` remains in the legal initialized address range
    `[0,383]` for every actual read unless a larger legal map is
    specified.
-   Check the request-to-`SR_Q` latency against the model; do not
    compare data in the same delta cycle as address/control changes.
-   Tag every request with its object, local coordinate, destination
    screen coordinate, and consumer purpose; assert returned data uses
    the matching tag.
-   Verify no output word is lost when requests are consecutive.
-   Test first and last valid ROM addresses and every type/base-address
    boundary from the approved map.
-   Ensure simulation has a valid `SR_verilog.rcf`; missing
    initialization must fail clearly rather than silently pass.

### 3.2 Frame Buffer

-   Check all controls/address/data are stable for the model's required
    setup/hold interval at the sampling edge.
-   Verify `FB_CEN`/`FB_WEN` truth table against the actual `FB.v`
    implementation, not assumptions about conventional SRAM polarity.
-   Ensure every write uses `FB_A` in `[0,4095]`.
-   Count accepted writes and check that all addresses are covered
    exactly as the selected policy requires.
-   For full overwrite, verify each address is written once with its
    final color (or document intentional rewrites).
-   For prefill-and-overlay, verify every address receives background
    before any dependency on prior contents, and that all sprite writes
    are correctly scheduled.
-   Verify final write is accepted before `done` asserts.
-   Ensure the testbench's post-`done` readback override does not
    contend with active SGDE writes.

## 4. Architecture invariants / assertions

The exact SVA syntax depends on the chosen RTL clocking and reset
semantics. Implement equivalent assertions/checkers once the relevant
protocol is frozen.

  -----------------------------------------------------------------------
  ID                      Invariant               Verification method
  ----------------------- ----------------------- -----------------------
  INV-01                  Top-level interface     Elaborate/interface
                          names and widths are    review
                          unchanged               

  INV-02                  Each accepted           Transaction scoreboard
                          descriptor is captured  
                          once                    

  INV-03                  Descriptor order is     Descriptor queue
                          retained unless spec    comparison
                          says otherwise          

  INV-04                  Every ROM response is   Tagged-latency
                          paired with its request scoreboard
                          metadata                

  INV-05                  ROM reads use legal     Assertions + model
                          address and active read timing checks
                          enable                  

  INV-06                  No framebuffer address  Assertion
                          outside 0..4095 is      
                          written                 

  INV-07                  A completed frame       Memory scoreboard after
                          defines all 4096 RGB    `done`
                          locations               

  INV-08                  Uncovered or fully      Golden-frame comparison
                          transparent pixels      
                          equal                   
                          `12'b110011110000`      

  INV-09                  Mask=0 means            Directed mask tests
                          transparent or opaque   
                          according to the        
                          approved bit-format     
                          spec; no polarity       
                          assumption before       
                          approval                

  INV-10                  Status accumulators     Independent reference
                          include all accepted    model
                          scene objects before    
                          final state is emitted  

  INV-11                  Valid overlap is an     Coordinate-level
                          absolute-coordinate     reference model
                          mask intersection       

  INV-12                  Status outcome is       Paired tests with same
                          independent of          geometry/different
                          compositing occlusion   layer order
                          if D-006 is approved    
                          that way                

  INV-13                  Man live mask is used   Mask-difference
                          for status judging if   directed tests
                          D-005 is approved       

  INV-14                  Final pixel follows     Pixel-level reference
                          approved layer ordering model
                          for every multi-cover   
                          coordinate              

  INV-15                  No stale framebuffer    Initialize RAM to
                          contents affect output  X/random before
                          under full-overwrite    operation
                          policy                  

  INV-16                  `done` occurs only      Write monitor + done
                          after the last required monitor
                          write has been accepted 

  INV-17                  `ready` and `done`      Cycle-level scoreboard
                          conform to approved     
                          pulse/level protocol    

  INV-18                  Reset returns           Reset assertions in
                          controller and internal every phase
                          metadata to approved    
                          state                   

  INV-19                  Counters neither skip   Address coverage/count
                          nor repeat required     assertions
                          coordinates             

  INV-20                  Background value is     Directed and random
                          exactly                 golden comparison
                          `12'b110011110000` for  
                          uncovered pixels        
  -----------------------------------------------------------------------

## 5. Directed functional tests

Required after the relevant specification decisions are approved:

### Input and control

-   Zero objects, one object, maximum legal number of objects.
-   Descriptor count boundary and overflow/invalid count.
-   `sprite` asserted for one cycle and for the legal multi-cycle
    pattern.
-   `start` immediately after the final descriptor, with legal gaps, and
    simultaneous with the final descriptor if allowed.
-   `start` asserted while busy / during output / during done, per
    approved acceptance rule.
-   Reset at idle and in every active phase.

### ROM and pixel format

-   First and last address for each sprite region.
-   Every type code, including unsupported type values.
-   Pixels with mask 0 and mask 1, and distinct RGB values.
-   Address mapping at row/column boundaries.
-   ROM output changes only according to the synchronous model contract.
-   Back-to-back requests and request stalls.

### Geometry and clipping

-   Sprite at `(0,0)`, `(63,0)`, `(0,63)`, `(63,63)`.
-   Sprite partially outside right/bottom boundaries if the approved
    object dimensions allow it.
-   Object exactly touching another object's mask boundary without
    intersecting.
-   One-pixel overlap and full overlap.
-   Overlap caused by Man live mask but not by Man original mask, and
    vice versa.

### Man status

-   No Candy or Ghost overlap.
-   Candy-only overlap.
-   Ghost-only overlap.
-   Both Candy and Ghost overlap at the same coordinate.
-   Candy and Ghost overlap at different coordinates.
-   Multiple Candy objects; multiple Ghost objects; repeated overlap
    pixels.
-   Input-order permutations with unchanged geometry.
-   Same overlap geometry with different compositing order to verify
    status/compositing separation.
-   Man mask with all-zero/all-one patterns if representable by the ROM
    content.

### Compositing and framebuffer

-   No objects: every pixel is background.
-   Single opaque sprite.
-   Fully transparent sprite.
-   Two and three objects overlapping with distinct colors and masks.
-   Layer order reversed.
-   Sprite pixels at framebuffer address 0 and 4095.
-   Background around object borders and inside transparent holes.
-   All 4096 addresses covered exactly as required; no stale X values
    after `done`.
-   Compare entire 64×64 output against an independent golden model, not
    only a sample of pixels.

## 6. Random and metamorphic testing

Use a deterministic seed and record it for every failing run.

-   Random legal descriptor lists up to the approved maximum.
-   Random object types, coordinates, mask patterns and RGB payloads
    constrained by the approved ROM map.
-   Compare against a software/reference model implementing the frozen
    geometry, status truth table, and layer priority.
-   Translation property: where clipping does not occur, translating all
    objects by the same delta should translate the image and preserve
    overlap relationships.
-   Input permutation property: if layer order is defined by input
    order, permuting descriptors may change visible pixels but should
    not change status when geometry and object identities are unchanged,
    provided status is approved as order-independent.
-   Adding a fully transparent object should not change output pixels or
    overlap status.
-   Changing only hidden/occluded RGB should not change final output at
    coordinates where the object is fully covered by a higher-priority
    opaque object.
-   Re-running the same scene after randomizing framebuffer initial
    contents should produce the same final frame under a full-overwrite
    policy.

## 7. Timing and performance verification

-   Confirm testbench timescale interpretation: `timescale 100ps/1ps`
    and `CYCLE=20` imply a 2 ns clock period in the supplied testbench
    configuration.
-   The supplied memory model excerpts include a 3 ns period check. A 2
    ns testbench period may trigger timing violations. Resolve this with
    the owner and actual model before treating simulation failures as
    RTL functional failures.
-   Run gate-level timing only after official SDC, library, PVT, and
    macro timing models are identified.
-   Report the critical path and clock period from actual STA. Do not
    infer timing closure from RTL inspection.
-   Measure cycle count using a monitor from the approved
    operation-acceptance event through the approved `done` event.
-   Report area only from a named synthesis/implementation run with
    tool, library, constraints, and report provenance.

## 8. Acceptance criteria

Architecture-level acceptance requires: 1. All blocking decisions in
`SGDE_Architecture_Decision_Log.md` are explicitly resolved by the
authorized owner or formally deferred with a documented impact. 2. The
source-of-truth hierarchy and exact interface/reset/clock protocols are
unambiguous. 3. A cycle-level FSM table and memory timing schedule are
reviewed. 4. A reference model can independently predict status and
every framebuffer pixel. 5. All invariants have a defined checker or
review procedure. 6. Verification coverage includes all directed tests
relevant to approved requirements. 7. No requirement is marked passed
without reproducible evidence.

RTL verification acceptance requires, at minimum, passing directed and
random simulation against the independent golden model, zero unexplained
model timing violations, lint review, and documented unresolved
warnings. Synthesis/STA/physical acceptance are separate signoff stages
and require their own reports.

## 9. Current verification status

-   Architecture reasoning: **PROPOSED**
-   RTL simulation: **NOT RUN / NOT VERIFIED**
-   Lint: **NOT RUN / NOT VERIFIED**
-   Synthesis: **NOT RUN / NOT VERIFIED**
-   STA: **NOT RUN / NOT VERIFIED**
-   Physical implementation / DRC / LVS: **NOT RUN / NOT VERIFIED**
