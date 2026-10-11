# SGDE Architecture Decision Log

-   **Task:** SGDE-ARCH-001
-   **Status:** **OPEN ITEMS --- ARCHITECTURE NOT FROZEN**
-   **Decision authority:** Project owner / designated specification
    owner. Architecture AI may propose options but cannot approve
    requirements on their behalf.

## Decision status vocabulary

-   **OPEN:** evidence is insufficient or conflicting.
-   **PROPOSED:** candidate recommendation only; not approved.
-   **APPROVED:** explicitly approved by the authorized owner and
    reflected in the source specification.
-   **REJECTED:** owner rejected the option.
-   **BLOCKING:** must be resolved before architecture freeze or
    implementation of affected behavior.

## Issue list

  ------------------------------------------------------------------------------------------------------------------------------------
  ID               Status /     Specification evidence     Ambiguity / candidates  Architecture impact     Required approver
                   priority                                                                                
  ---------------- ------------ -------------------------- ----------------------- ----------------------- ---------------------------
  D-001 Man state: OPEN /       Supplemental notes require A: Candy-only maps to a Status truth table,     Project owner /
  Candy only       BLOCKING     aggregate overlap analysis dedicated state; B:     sticky flags,           behavior-spec owner
                                and say both Candy+Ghost   Candy-only maps to      verification expected   
                                means happy                default/non-happy       values                  
                                                           state; C: other                                 
                                                           encoding                                        

  D-002 Man state: OPEN /       Same evidence as D-001     A: Ghost-only dedicated Same as above; possible Project owner /
  Ghost only       BLOCKING                                state; B:               priority interactions   behavior-spec owner
                                                           default/non-happy; C:                           
                                                           other encoding                                  

  D-003 Man        PROPOSED /   Supplemental notes: valid  A: happy if both sticky Whether independent     Project owner /
  overlaps both    BLOCKING     overlaps with both Candy   flags set anywhere in   aggregate flags suffice behavior-spec owner
  types                         and Ghost yield happy      scene; B: require       or coordinate-paired    
                                final state                simultaneous overlap at logic is required       
                                                           one coordinate; C:                              
                                                           other rule                                      

  D-004 Multiple   OPEN /       Notes say multiple         A: logical OR over all  State storage, scan     Project owner
  overlaps /       BLOCKING     Ghost/Candy may arrive     valid overlaps; B:      count, ordering         
  multiple                      sequentially and decision  count overlaps; C:      sensitivity, cycle      
  instances                     is aggregated after all    per-object priority; D: count                   
                                input completes            latest overlap wins                             

  D-005 Man live   PROPOSED /   Supplemental notes: Man    A: live mask only for   Separate status and     Project owner /
  mask vs original BLOCKING     overlap validity uses      status, original Man    compositing paths;      graphics-spec owner
  mask                          Man(live) mask as sole     mask for visual         ROM/mask data use       
                                judgment basis; other      compositing                                     
                                objects use original masks (recommended                                    
                                                           separation); B: live                            
                                                           mask for both; C: other                         

  D-006 Status     PROPOSED /   Notes interpret status as  A: geometric overlap    Status pass may be      Project owner
  independent of   BLOCKING     valid absolute-coordinate  independent of          independent from        
  occlusion                     mask intersection          occlusion               compositor; test oracle 
                                independent of final       (recommended); B: only  differs                 
                                compositing order          visible overlap counts                          

  D-007 Sprite ROM PARTIALLY    `SR.v`/SGDE shell use      Width: 13 bits is       Pixel extraction and    Graphics-spec owner
  width and bit    KNOWN /      13-bit `SR_Q`;             established by supplied color correctness       
  format           BLOCKING     supplemental note corrects interface/model. Exact                          
                                "384\*12" diagram typo to  RGB channel bit                                 
                                384×13; note says mask bit slicing/order must be                           
                                0 and RGB payload 12 bits  confirmed against the                           
                                                           authoritative sprite                            
                                                           format                                          

  D-008 ROM        OPEN /       ROM capacity is 384×13; no A: fixed contiguous     Address generator,      Project owner / ROM-content
  address map and  BLOCKING     complete tile              regions per type; B:    object-local            owner
  sprite                        map/dimensions in          packed object images    coordinates, scan       
  dimensions                    available excerpts         with table; C: other    count, out-of-range     
                                                           map                     handling                

  D-009 SR         PROPOSED /   `SR.v` latches             A: request in cycle N,  Adds latency per read   RTL/verification owner plus
  synchronous read BLOCKING     address/control at rising  consume after N edge    unless pipelined;       project owner for cycle
  schedule                      edge and models 1 ns       plus access delay; B:   affects metadata        budget
                                clock-to-Q; setup/hold and consume at N+1 edge     alignment and max       
                                3 ns period checks exist   (conservative); C:      frequency               
                                                           registered valid                                
                                                           pipeline                                        

  D-010 SR CEN     PROPOSED     Model behavior: `CEN=0`    Preserve active-low     Control polarity and    RTL owner to confirm
  polarity         from         reads; `CEN=1` holds       read enable, unless     ROM traffic             against exact `SR.v`;
                   supplied     output                     actual supplied source                          project owner if interface
                   model                                   differs from excerpt                            spec conflicts

  D-011            OPEN /       Supplemental note requires A: full overwrite of    Write count, memory     Project owner
  Framebuffer      BLOCKING     all 4096 RGB values be     every pixel with final  schedule, correctness   
  initial contents              stored and uncovered areas color (recommended); B: before first frame      
                                be background; `FB.v` is a background prefill then                         
                                RAM model, no guaranteed   sprite updates; C:                              
                                scene initialization       read-modify-write                               
                                specified                  existing contents                               

  D-012 Background PROPOSED /   Supplemental notes specify A: output background at A gives deterministic   Project owner /
  guarantee        BLOCKING     background RGB             every uncovered pixel;  full-frame result; B    verification owner
                                `12'b110011110000`;        B: initialize then only requires complete       
                                testbench XPM mapping      draw opaque sprite      prefill before          
                                includes this color        pixels                  composition             

  D-013 FB         PROPOSED     `FB.v` is synchronous      A: avoid reads and      RMW costs cycles and    RTL owner to verify exact
  read/write edge  from         single-port 4096×12,       full-overwrite; B:      must respect one-port   model; project owner for
  semantics        supplied     edge-sampled               schedule read then      schedule                behavior
                   model        controls/address/data, 1   later write; C: use                             
                                ns clock-to-Q and          readback only after                             
                                setup/hold checks          done as testbench does                          

  D-014 `ready`    OPEN /       Top-level shell only       A: ready for each       Input throughput,       Interface-spec owner
  meaning          BLOCKING     declares output `ready`;   descriptor; B: ready    descriptor capture, FSM 
                                testbench drives           only in idle; C: ready  states                  
                                descriptors when           for operation launch;                           
                                `ready==1`                 D: other                                        

  D-015 `sprite`   OPEN /       Testbench sets `sprite=1`  A: accept when          Prevent duplicate/drop, Interface-spec owner
  handshake        BLOCKING     while `ready==1` then      `ready && sprite` at    descriptor count, cycle 
                                presents descriptor        rising edge; B:         count                   
                                values; exact sampling     `sprite` itself starts                          
                                contract not fully stated  a multi-cycle transfer;                         
                                                           C: other                                        

  D-016 `start`    OPEN /       Testbench asserts `start`  A: closes descriptor    FSM transition,         Interface-spec owner
  meaning and      BLOCKING     after descriptor list;     list and starts         last-descriptor/start   
  ordering                      shell includes both        processing; B: start is collision, restart      
                                `sprite` and `start`       operation request with  rules                   
                                                           descriptor stream                               
                                                           already complete; C:                            
                                                           other                                           

  D-017 Descriptor OPEN /       Testbench declares         A: max 20; B: 21        Descriptor storage      Project owner /
  count and        BLOCKING     `n_sprite_max=20` and      entries including       size, overflow policy,  verification owner
  maximum                       `data[0:n_sprite_max]`;    header/count; C:        scan cycles             
                                this alone does not        spec-defined different                          
                                establish legal hardware   limit                                           
                                maximum                                                                    

  D-018            OPEN /       `X`,`Y` are 6-bit; screen  A: clip pixels outside  Coordinate arithmetic,  Project owner
  Coordinates and  BLOCKING     is 64×64; no full clipping screen; B: reject       edge pixels, status     
  clipping                      rule provided              invalid object origin;  overlap                 
                                                           C: wrap; D: other                               

  D-019 Layer      PROPOSED /   Supplemental notes say     A: later descriptor     Compositor priority     Project owner /
  order and input  BLOCKING     input order defines layer  overlays earlier        storage and             graphics-spec owner
  order                         ordering, but direction is (common painter's       deterministic result    
                                not explicit               order); B: earlier                              
                                                           descriptor overlays                             
                                                           later; C: fixed type                            
                                                           priority                                        

  D-020 Man visual OPEN /       Notes distinguish Man live A: original mask for    Pixel visibility and    Graphics-spec owner
  mask             BLOCKING     mask for overlap judgment; visual transparency; B: image output            
                                do not fully define        live mask; C: another                           
                                visible Man mask           visual mask                                     

  D-021 Reset      OPEN /       SGDE shell names `reset`;  A: active-high          All state               Clock/reset-spec owner
  polarity and     BLOCKING     testfixture pulses reset   synchronous; B:         initialization and      
  behavior                      0→1→0, but this does not   active-high             testbench compatibility 
                                conclusively establish     asynchronous; C:                                
                                active polarity or         active-low; D:                                  
                                asynchronous/synchronous   spec-defined                                    
                                behavior                                                                   

  D-022 Reset      OPEN /       No complete reset          A: abort immediately    Partial framebuffer     Clock/reset-spec owner /
  during operation BLOCKING     interruption semantics     and restart idle; B:    contents and restart    project owner
                                available                  finish current write;   safety                  
                                                           C: unspecified/illegal                          

  D-023 Clock      OPEN /       Testbench `CYCLE=20` under A: change testbench     Timing-check failures,  Project owner / STA owner
  target and       BLOCKING     `timescale 100ps/1ps`      period; B:              feasible frequency,     
  timing                        means a 2 ns period in     change/replace model    cycle metric            
  constraints                   this testbench             timing only if                                  
                                configuration; supplied    authorized; C: official                         
                                memory model has 3 ns      clock is specified                              
                                period checks, creating a  elsewhere                                       
                                potential mismatch if both                                                 
                                are active                                                                 

  D-024 Library,   OPEN /       Notes say TA may provide   A: obtain official      Prevents credible       Project owner /
  PVT, constraints BLOCKING for `synopsys_dc.setup`,       files/spec; B: optimize area/timing estimates   synthesis-physical-design
  and score        PPA claims   `SGDE.tcl`,                under explicitly chosen and optimization        owner
  formula                       MMMC/LEF/Encounter files;  provisional target      ranking                 
                                contents not reviewed                                                      
                                here. No official scoring                                                  
                                equation or signoff                                                        
                                reports in supplied                                                        
                                context                                                                    

  D-025 SRAM model OPEN /       `FB.v` has edge-based      A: schedule             Write/read cycle count  Memory-model owner / RTL
  control timing   BLOCKING for operation, setup/hold,     controls/address/data   and STA constraints     owner
                   exact        clock-to-Q delay and a 3   before active edge; B:                          
                   scheduling   ns period check            rely on a different                             
                                                           macro contract if                               
                                                           official library                                
                                                           differs                                         

  D-026 Background PROPOSED     Notes permit               A: full raster          Area and cycles; full   Project owner
  initialization                designer-defined           overwrite with          overwrite avoids        
  method                        implementation flow where  background fallback     old-state dependency    
                                spec is silent             (recommended); B:                               
                                                           prefill then overlays                           

  D-027            OPEN /       Testbench sequence appears A: forbid simultaneous  Off-by-one descriptor   Interface-spec owner
  Simultaneous     BLOCKING     to send descriptors then   assertion; B: capture   count or dropped final  
  final descriptor              start, but exact same-edge descriptor and close    object                  
  and start                     behavior is not specified  list same edge; C:                              
                                                           define explicit                                 
                                                           priority                                        

  D-028 Done/ready OPEN /       Testbench waits on         A: one-cycle done after Testbench               Interface-spec owner
  cycle relation   BLOCKING     `posedge done` then starts final write; B: level   synchronization and     
                                readback; no exact pulse   until next start; C:    re-entrancy             
                                width or next-operation    other                                           
                                rule in shell                                                              

  D-029 RGB        PARTIALLY    Background value is        A: fixed RGB444 field   Correct rendered colors Graphics-spec owner
  background and   KNOWN        `12'b110011110000`; sprite order as source spec;   and golden-image match  
  output bit                    payload is 12 bits; exact  B: alternate order                              
  ordering                      channel field mapping                                                      
                                needs authoritative                                                        
                                confirmation                                                               

  D-030 Testbench  OPEN /       `timescale 100ps/1ps` and  A: run at ≥3 ns pending Sim failures may be     Verification/STA owner
  and memory-model BLOCKING for `CYCLE=20` imply 2 ns;     official target; B: use timing-check violations 
  period mismatch  simulation   supplied model timing      official target and     unrelated to functional 
                   setup        checks include 3 ns        resolve model mismatch  RTL                     
                                minimum period             with owner-approved                             
                                                           model/constraint change                         
  ------------------------------------------------------------------------------------------------------------------------------------

## Required decision package before freeze

1.  Approved input handshake (`ready`, `sprite`, `start`) including
    same-edge cases.
2.  Reset polarity, synchronization, and reset-during-operation
    behavior.
3.  ROM address map, sprite dimensions, exact 13-bit pixel format, and
    ROM latency contract.
4.  Man state truth table for Candy-only, Ghost-only, both, none,
    repeated overlaps, and multiple instances.
5.  Layer priority direction and Man visual mask semantics.
6.  Maximum descriptor count and coordinate clipping rules.
7.  Framebuffer initialization/write policy and exact done timing.
8.  Official clock period/SDC, target library/PVT, memory macro timing
    contract, and score formula.

**No item is marked APPROVED** because no explicit project-owner
decision was provided in the available context.
