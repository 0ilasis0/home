==================================================

TASK PROMPT

==================================================

TASK ID:
RTL-INC-001

TASK TYPE:
Incremental RTL Implementation

PHASE:
RTL Implementation

OWNER:
AI-2 RTL Engineer

OBJECTIVE:

Implement the first RTL increment of the Triangle Rendering Engine.

The objective is to establish the architecture-compliant top-level RTL skeleton, asynchronous reset behavior, P1/P2/P3 input-capture protocol, FSM foundation, and required busy timing.

This task must NOT implement the geometry scan datapath yet.

---

CURRENT PROJECT STATE:

* Project Specification is established.
* Architecture baseline is Candidate C — Bounded Coordinate Scan.
* Arithmetic definition is FROZEN.
* RTL Coding Standard is established.
* RTL implementation has not yet been established as a verified artifact.
* No verified RTL simulation evidence is currently available.
* No synthesis, STA, gate-level, P&R, DRC, or LVS evidence is available.

---

CURRENT BASELINE:

Candidate C — Bounded Coordinate Scan.

The final architecture behavior is:

IDLE
→
CAPTURE_P2
→
CAPTURE_P3
→
SCAN
→
IDLE

There is NO RELEASE state.

---

SOURCE OF TRUTH:

Use the following hierarchy:

1. Project Specification
2. Frozen Architecture decisions
3. Frozen Arithmetic definition
4. RTL Coding Standard
5. This task prompt

If a conflict is discovered, STOP and report it.

Do not silently resolve an architecture/specification conflict.

---

REQUIRED INPUT ARTIFACTS:

1. Project Specification
2. Current Architecture Document
3. Frozen Arithmetic definition
4. RTL Coding Standard

If any required artifact available to the project is inconsistent with the above constraints, do not proceed with an implementation decision. Report the conflict.

---

TASK SCOPE:

### IN SCOPE

Implement only the first RTL increment containing:

1. Exact top-level module:

   module triangle (
   clk,
   reset,
   nt,
   xi,
   yi,
   busy,
   po,
   xo,
   yo
   );

2. Explicit Verilog-2001 port declarations.

3. Asynchronous active-high reset.

4. FSM foundation using `localparam`.

5. Required architectural states:

   * IDLE
   * CAPTURE_P2
   * CAPTURE_P3
   * SCAN

6. P1 capture:

   At Cycle N, when `nt=1` and the design is in IDLE:

   * capture P1 `(x1,y1)`
   * remain protocol-compliant with `busy=0`
   * transition toward P2 capture.

7. P2 capture:

   At Cycle N+1:

   * capture P2 `(x2,y2)`
   * after the N+1 rising edge, `busy` must be HIGH.

8. P3 capture foundation:

   At Cycle N+2:

   * capture P3 y-coordinate.
   * x3 must NOT be stored as a dedicated register because architecture defines `x3=x1`.
   * transition to SCAN initialization/foundation as required by the architecture.

9. Busy behavior foundation:

   * IDLE: `busy=0`
   * after P2 capture: `busy=1`
   * busy remains asserted through P3 capture / non-IDLE operation.

10. Reset behavior:

    During active reset:

    * `busy=0`
    * `po=0`

    Reset state must be IDLE.

11. RTL coding discipline:

    * Verilog only.
    * `.v` RTL.
    * No SystemVerilog constructs.
    * Sequential logic uses non-blocking assignment.
    * Combinational logic uses blocking assignment.
    * Explicit widths.
    * Explicit signedness where applicable.
    * No implicit nets.
    * No latch.
    * No multiple drivers.
    * No combinational loop.
    * No `initial`.
    * No delays.
    * No testbench-only constructs.
    * No SVA.
    * No arbitrary generated/gated clock.

12. Use meaningful `_q` / `_d` naming where applicable.

13. Keep module hierarchy appropriate to the actual architecture. Do NOT create tiny modules solely for this increment.

---

OUT OF SCOPE:

* Geometry scan implementation.
* E12 calculation.
* E23 calculation.
* E31 calculation.
* Inside/outside classification.
* Boundary arithmetic.
* `xo/yo` point generation.
* Output ordering implementation beyond keeping `po=0` in this increment.
* Candidate B optimization.
* Pipeline modification.
* Resource sharing optimization.
* Timing optimization.
* Area optimization.
* Architecture redesign.
* Interface modification.
* Protocol modification.
* Verification environment modification.
* Testbench hacks.
* Synthesis optimization.
* Any change to Frozen Arithmetic.

---

DEPENDENCIES:

This task depends on:

* Frozen Architecture.
* Frozen Arithmetic.
* RTL Coding Standard.

No dependency on Candidate B is permitted.

No future optimization decision is required.

---

FROZEN CONSTRAINTS:

The implementation MUST NOT change:

* top-level interface
* port widths
* input protocol
* N/N+1/N+2 timing
* busy timing
* asynchronous active-high reset
* FSM architectural behavior
* x1=x3 assumption
* coordinate domain 0~7
* arithmetic definition
* output protocol
* output ordering
* final SCAN → IDLE behavior
* omission of E31
* Candidate C architecture

The implementation must not introduce a RELEASE state.

The implementation must not store a dedicated x3 register.

---

IMPLEMENTATION / VERIFICATION REQUIREMENTS:

### RTL implementation

Produce the RTL source for this increment as a synthesizable Verilog-2001 `.v` file.

The implementation must be structurally clear enough for independent code review.

The FSM should follow the Coding Standard:

* `localparam` state encoding
* state register
* next-state logic
* default assignments
* explicit default/illegal-state handling

If output/control logic is separated from next-state logic, avoid multiple drivers.

### Basic self-check

Before declaring the increment complete, inspect at minimum:

* reset sensitivity
* reset state
* P1 capture timing
* P2 capture timing
* P3 capture timing
* busy assertion timing
* no unintended x3 storage
* no unintended po assertion
* no latch
* no multiple driver
* no implicit signedness/width issue
* no SystemVerilog syntax
* no simulation-only RTL construct

---

EXPECTED OUTPUTS:

1. RTL source for the increment.

2. Module/file name consistent with the required top-level `triangle` module.

3. Brief implementation change record containing:

   * What changed
   * Why it changed
   * Module changed
   * Architecture dependency
   * Interface impact
   * Timing/latency impact
   * Verification status

4. Any discovered issue must be explicitly classified rather than silently patched.

---

REQUIRED EVIDENCE:

1. RTL source artifact/version.
2. Compilation/elaboration result.
3. RTL Coding Standard self-review result.
4. Change record:
   - What changed
   - Why
   - Module changed
   - Architecture dependency
   - Interface impact
   - Timing/latency impact
   - Resource impact if applicable
5. Explicit statement:

   RTL IMPLEMENTATION COMPLETE
   FUNCTIONAL VERIFICATION = NOT VERIFIED

Formal simulation evidence is owned by RTL Verification Engineer.

---

EXIT CRITERIA:

This task is COMPLETE when:

- required RTL increment is implemented;
- required top-level interface is unchanged;
- P1/P2/P3 capture logic is implemented according to Architecture;
- busy timing logic is implemented according to Architecture;
- reset behavior is implemented according to Architecture;
- RTL Coding Standard self-review is complete;
- RTL compilation/elaboration passes;
- no unresolved RTL compile/elaboration issue remains.

Formal functional simulation is NOT an exit criterion of this task.

The RTL implementation must be handed off to RTL Verification Engineer for
independent simulation and verification.

If simulation has not yet been performed:

Status = IMPLEMENTED / NOT VERIFIED

The RTL Engineer must not claim functional verification PASS.

---

FAILURE / BLOCKING RULES:

If compilation fails:

* classify the failure;
* fix only issues within this increment;
* rerun compilation.

If simulation fails:

* identify the first divergence;
* classify the failure as appropriate:

  * RTL BUG
  * ARCHITECTURE MISMATCH
  * TESTBENCH BUG
  * REFERENCE MODEL BUG
  * CHECKER BUG
  * TOOL / ENVIRONMENT ISSUE
  * UNKNOWN

Do not modify RTL merely to force the expected simulation result.

If the failure indicates an Architecture conflict:

STOP.

Report:

ARCHITECTURE CHANGE REQUIRED

Do not redesign the architecture inside this task.

---

FORBIDDEN ACTIONS:

* Do not implement the entire Triangle RTL.
* Do not implement SCAN geometry in this increment.
* Do not modify Frozen Arithmetic.
* Do not add E31.
* Do not add a RELEASE state.
* Do not change busy timing.
* Do not change input protocol.
* Do not change output protocol.
* Do not change top-level interface.
* Do not introduce SystemVerilog.
* Do not introduce testbench logic into RTL.
* Do not add debug ports.
* Do not use simulation-only constructs.
* Do not optimize architecture.
* Do not create unnecessary tiny modules.
* Do not declare PASS without executed evidence.
* Do not hide verification failures.

---

HANDOFF REQUIREMENTS:

When this task is complete, return:

1. RTL artifact/version.
2. Compilation evidence.
3. Basic simulation evidence.
4. Coding Standard review result.
5. Change record.
6. Any remaining issue.
7. Explicit status:

   VERIFIED

or

NOT VERIFIED

The next RTL increment must not be started by this task.

AI-0 will determine the next task only after reviewing the returned evidence.

==================================================

END TASK PROMPT

==================================================
