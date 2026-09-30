==================================================

TASK PROMPT

==================================================

TASK ID:
RTL-VERIF-001

TASK TYPE:
Independent RTL Functional Verification

PHASE:
RTL Increment Verification

OWNER:
AI-3 RTL Verification Engineer

OBJECTIVE:

對目前已完成的 RTL increment 執行獨立 functional verification。

Verification target 是本次 handoff 提供的 `triangle` RTL source。

本 Task 的目的不是驗證整個 Triangle Rendering Engine，
而是確認目前已實作 increment 的 reset、input capture、FSM、
busy timing、SCAN skeleton 與 output-inactive behavior 是否符合
目前已批准的 Specification / Architecture。

AI-3 必須依照 intended behavior 建立獨立 verification environment，
不得直接把 RTL implementation 本身當作 reference model。

CURRENT PROJECT STATE:

Current Phase:
RTL Increment → Independent Verification

Architecture:
目前採用：
IDLE → CAPTURE_P2 → CAPTURE_P3 → SCAN → IDLE

Current RTL Increment:
P1/P2/P3 capture、FSM、busy timing、reset 與 SCAN skeleton。

Geometry rendering:
NOT IMPLEMENTED in this RTL increment。

Verification Status:
NOT VERIFIED

Compilation Status:
UNKNOWN

RTL Version:
UNKNOWN

RTL Baseline:
UNKNOWN

RTL UNDER VERIFICATION:

Artifact:
`triangle` top-level Verilog RTL source supplied in the current handoff.

RTL ARTIFACT:

`triangle` module source supplied with this task.

RTL VERSION:

UNKNOWN

RTL BASELINE:

UNKNOWN

SOURCE OF TRUTH:

1. Project Specification
2. Approved Architecture
3. Interface definition
4. Clock / Reset definition
5. Verification Plan
6. Current RTL artifact under verification
7. RTL Coding Standard

若不同來源發生 conflict，不得自行改變 design intent。

若 conflict 會影響本 Task 的 expected behavior，
立即停止該部分 verification，回報：

ARCHITECTURE CHANGE REQUIRED
或
SPECIFICATION AMBIGUITY

並將 failure / blocker 回傳 AI-0。

REQUIRED INPUT ARTIFACTS:

1. Exact RTL artifact supplied with this task
2. Current approved Architecture
3. Project Specification
4. Interface definition
5. Clock / Reset definition
6. Verification Plan
7. RTL Coding Standard
8. Available RTL compilation/elaboration environment

不得默默替換成後續 RTL revision。

如果 verification 過程中發現 RTL source 已經被修改：

Old RTL version
↓
New RTL version
↓
New verification baseline
↓
Re-verification required

不同 RTL version 的 evidence 不得混合。

IMPLEMENTED FUNCTIONAL SCOPE:

目前實際 RTL 已實作：

1. Top-level `triangle` interface
2. Active-high asynchronous reset
3. `IDLE` state
4. P1 capture when `nt=1`
5. `CAPTURE_P2` state and P2 capture
6. `CAPTURE_P3` state and P3 `yi` capture
7. `SCAN` state
8. `SCAN → IDLE` transition
9. Registered `busy`
10. Registered `po/xo/yo`
11. Illegal/default FSM recovery to `IDLE`
12. `po=0` for this increment

VERIFICATION SCOPE:

## 1. RESET

Verify:

* reset asserted asynchronously drives the design to `IDLE`
* `busy=0` during reset
* `po=0` during reset
* reset does not require a clock edge to take effect
* reset release allows normal operation

Do not infer behavior of unspecified `xo/yo` during reset as a functional
requirement unless the applicable specification explicitly requires it.

## 2. IDLE / INPUT PROTOCOL

Verify:

* design starts in an idle-ready condition after reset release
* `busy=0` in `IDLE`
* `nt=1` in `IDLE` causes P1 capture
* P1 values come from `xi/yi`
* P1 capture causes transition to `CAPTURE_P2`
* input protocol behavior matches the approved timing definition

## 3. P2 CAPTURE

Verify:

* P2 is captured from `xi/yi`
* P2 capture occurs in the expected cycle
* P2 capture causes transition to `CAPTURE_P3`
* `busy` becomes asserted according to the approved cycle definition

## 4. P3 CAPTURE

Verify:

* P3 `yi` is captured in `CAPTURE_P3`
* `busy` remains asserted during the implemented processing state
* transition to `SCAN` occurs at the expected clock edge

## 5. FSM TRANSITIONS

Verify the implemented state sequence:

IDLE
→ CAPTURE_P2
→ CAPTURE_P3
→ SCAN
→ IDLE

Verify no unexpected state transition occurs during legal input sequences.

If illegal/default-state behavior can be stimulated or observed through
the verification environment, verify that the default behavior returns
the design to `IDLE` with `busy=0`.

## 6. BUSY TIMING

Verify cycle-level behavior of `busy`.

At minimum verify:

* before accepting P1: `busy=0`
* after P2 capture edge: `busy=1`
* during P3 capture / SCAN skeleton: `busy=1`
* after `SCAN → IDLE`: `busy=0`

The checker must distinguish pre-edge and post-edge values.

Do not reinterpret the Architecture's timing semantics.

## 7. OUTPUT VALIDITY

For this increment:

* `po` must remain `0`
* no point output is expected
* `xo/yo` must NOT be treated as valid when `po=0`

Do not create expected triangle-point output for this increment.

## 8. INTERFACE STABILITY

Verify that the RTL presents the expected top-level interface:

`clk, reset, nt, xi, yi, busy, po, xo, yo`

Do not modify the production RTL interface to facilitate verification.

OUT OF SCOPE:

The following are explicitly OUT OF SCOPE:

* E12 implementation
* E23 implementation
* E31 implementation
* signed arithmetic datapath
* edge-function calculation
* multiplication
* inside/outside classification
* x/y scan iteration
* triangle boundary generation
* complete triangle rendering
* output point ordering
* complete geometry correctness
* Candidate B optimization
* synthesis optimization
* timing optimization
* physical design
* P&R
* DRC
* LVS
* future RTL increments
* architecture changes
* interface changes
* production RTL modification

VERIFICATION REQUIREMENTS:

AI-3 must perform independent verification.

Verification must determine whether the implemented RTL behavior matches
the intended behavior defined by the applicable Specification and
Architecture.

Do NOT:

* copy the RTL state-transition implementation into the reference model
* derive expected values by simply observing RTL internal state
* use RTL outputs as the expected result
* modify RTL to make tests pass
* declare correctness based only on compilation
* perform code review as a substitute for functional verification

TEST REQUIREMENTS:

At minimum create directed tests covering:

1. Reset assertion from idle
2. Reset assertion during an active capture sequence
3. Reset release followed by a legal P1/P2/P3 sequence
4. Legal P1 capture
5. P2 capture
6. P3 capture
7. Busy timing across P1/P2/P3/SCAN
8. SCAN → IDLE transition
9. `po=0` throughout this increment
10. Multiple legal transactions separated by return to IDLE
11. Boundary coordinate values relevant to capture, including 0 and 7
12. Representative legal coordinate combinations consistent with the
    project input domain

The testbench must not require geometry output because geometry is not
implemented in this increment.

REFERENCE MODEL REQUIREMENTS:

Build expected behavior independently from the RTL implementation.

The reference/checking logic should be based on:

* architectural state sequence
* specified capture timing
* specified busy timing
* specified reset behavior
* specified output-valid behavior

Do not reproduce RTL expressions, state register logic, or next-state
implementation merely under different signal names.

CHECKER / SCOREBOARD REQUIREMENTS:

At minimum check:

* reset state
* P1 capture timing
* P2 capture timing
* P3 capture timing
* state progression at architectural observation level
* busy timing
* return to idle
* `po=0`

Checks must identify:

* transaction/case
* expected cycle
* actual cycle
* expected value
* actual value
* first divergence

PROTOCOL / TIMING CHECKS:

Explicitly check:

* `nt` is accepted only in the expected idle condition
* P1 is sampled on the correct clock edge
* P2 is sampled on the following required cycle
* P3 is sampled on the following required cycle
* busy assertion occurs at the specified timing
* busy deassertion occurs when the implemented scan skeleton returns to IDLE
* asynchronous reset behavior occurs independently of clock edge
* no false `po` assertion occurs

Pre-edge and post-edge observations must be clearly distinguished.

BOUNDARY / CORNER CASES:

At minimum cover:

* xi = 0
* yi = 0
* xi = 7
* yi = 7
* representative mixed boundary values
* reset during each implemented state
* consecutive legal transactions after return to IDLE
* `nt=0` while idle
* `nt=1` outside the legal idle acceptance condition, if applicable
* default/illegal FSM recovery if the verification environment permits
  safe stimulation of such a condition

REQUIRED EVIDENCE:

AI-3 must record actual verification evidence.

Required evidence:

1. RTL artifact/version under verification
2. Testbench/source version
3. Simulation command or reproducible execution information
4. Tests executed
5. Test result for each required test
6. Expected vs actual comparison
7. First divergence for every failure
8. Relevant waveform or equivalent reproducible evidence for failures
9. Final verification status

Allowed status:

PASS
PARTIALLY VERIFIED
FAILED
NOT VERIFIED

Do not report PASS if the required tests were not actually executed.

FAILURE CLASSIFICATION:

For every failure, classify the failure as one of:

* RTL BUG
* ARCHITECTURE MISMATCH
* SPECIFICATION AMBIGUITY
* TESTBENCH BUG
* REFERENCE MODEL BUG
* CHECKER BUG
* TOOL / ENVIRONMENT ISSUE
* UNKNOWN

For every FAIL:

1. Preserve the failure.
2. Identify first divergence.
3. Report expected value.
4. Report actual value.
5. Identify failing cycle/transaction.
6. Provide reproducible evidence.
7. Classify the failure.
8. Do not modify production RTL.

If the evidence indicates an Architecture or Specification conflict,
stop treating the issue as an RTL fix and report the appropriate
architecture/specification escalation to AI-0.

EXIT CRITERIA:

This task is complete only when:

1. All required tests for the implemented increment have been executed.
2. Required reset checks have been executed.
3. Required capture checks have been executed.
4. Required FSM/timing checks have been executed.
5. Required busy checks have been executed.
6. Required output-validity checks have been executed.
7. Expected behavior has been compared against actual behavior.
8. Every failure has been classified.
9. Required evidence has been recorded.
10. No unresolved verification failure remains if final status is PASS.

If verification was not actually executed:

NOT VERIFIED

If only part of the required verification was completed:

PARTIALLY VERIFIED

Do not convert partial verification into FULL PASS.

FORBIDDEN ACTIONS:

AI-3 MUST NOT:

* modify production RTL
* modify Architecture
* modify Specification
* modify Interface definition
* modify Clock / Reset definition
* redefine intended behavior
* add debug ports to RTL
* add testbench-specific RTL behavior
* insert simulation-only RTL hacks
* change FSM behavior to satisfy a failing test
* silently switch RTL version
* combine evidence from different RTL versions
* declare geometry implemented
* verify features that are not present in this increment as though they
  were implemented

HANDOFF REQUIREMENTS:

When complete, return the verification result to AI-0.

Handoff must contain:

1. TASK ID
2. RTL artifact
3. RTL version/baseline
4. Verification environment/version
5. Tests executed
6. PASS / FAIL / PARTIALLY VERIFIED / NOT VERIFIED
7. First failure, if any
8. Failure classification
9. Evidence references
10. Open issues
11. Whether the current RTL is eligible for the next project increment

If FAIL:

Do not modify RTL.

Return the failure to AI-0 for classification and next-task assignment.

If PASS:

Return the verified evidence to AI-0.

AI-0 will determine the next task.

==================================================

END TASK PROMPT

==================================================
