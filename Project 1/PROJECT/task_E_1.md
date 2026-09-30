==================================================

TASK PROMPT

==================================================

TASK ID:
P1-ARCH-REVIEW

TASK TYPE:
Architecture Review / Freeze Readiness Assessment

PHASE:
P1 — Architecture Review

OWNER:
AI-1 System Architect

OBJECTIVE:

對目前已建立的 Triangle Rendering Engine Architecture artifact
進行正式 Architecture Review，確認其：

1. 是否與 Project Specification 一致
2. 是否與已確認的 interface / timing / reset behavior 一致
3. Candidate C bounded-scan architecture 是否內部一致
4. E12 / E23 arithmetic definition 與 signedness / width requirement
   是否足以直接約束 RTL implementation
5. busy timing、output ordering、scan behavior、reset behavior
   是否沒有未解決的 architecture ambiguity
6. 哪些事項可以進入 Human / Project Owner approval
7. 哪些事項仍需要 Architecture clarification 或 Human Decision

本 Task 的目的不是重新設計 Architecture，
而是確認目前 Architecture artifact 是否具備
進入 approval / freeze gate 的條件。


CURRENT PROJECT STATE:

Architecture artifact 已建立。

目前 Architecture 狀態為:

UNDER REVIEW / NOT FROZEN

Candidate C bounded scan 為目前 working architecture candidate，
但尚未取得可核對的 Human / Project Owner approval。

目前已有 `triangle.v` RTL candidate，
但其 correctness / verification status 為 NOT VERIFIED。

目前沒有足夠 evidence 支持：

- Architecture FROZEN
- RTL VERIFIED
- Synthesis PASS
- STA PASS
- P&R PASS
- DRC/LVS PASS


CURRENT BASELINE:

Candidate C — PROPOSED / WORKING ARCHITECTURE BASELINE

不是 Frozen Baseline。

不得將 Candidate C 在本 Task 中自行升級為
Approved / Frozen Baseline。


CURRENT RTL VERSION / CANDIDATE:

可辨識 RTL artifact:

triangle.v

正式 RTL version number / candidate ID:
UNKNOWN

不得自行建立新的正式 version number。

本 Task 不是 RTL implementation / modification task，
不得修改 RTL。


SOURCE OF TRUTH:

依既有 hierarchy：

1. Project Specification
2. 已確認 Interface / Timing / Clock-Reset decisions
3. Architecture Document
4. Verification requirements
5. RTL
6. RTL Coding Standard

若發現 authoritative artifacts 之間存在衝突：

DO NOT SILENTLY RESOLVE.

必須明確列出：

CONFLICT
SOURCE A
SOURCE B
AFFECTED DECISION
REQUIRED RESOLUTION OWNER


REQUIRED INPUT ARTIFACTS:

1. Project Specification
2. Current Architecture Document
3. Interface definition
4. Clock / Reset definition
5. Verification requirements / plan（若目前已有）
6. RTL Coding Standard
7. Current identifiable `triangle.v` RTL candidate
8. Existing task / handoff / decision evidence（若可取得）


TASK SCOPE:

IN SCOPE:

1. Review Architecture against Specification。
2. Review interface and cycle-level behavior。
3. Review input protocol:
   - N: P1
   - N+1: P2
   - N+2: P3
4. Review busy timing:
   - N+1 rising edge captures P2
   - busy becomes 1 after that edge
   - P3 is captured at N+2
5. Review reset semantics:
   - active-high asynchronous reset
   - reset/idle => busy=0, po=0
6. Review Candidate C bounded scan:
   - y1 -> y3
   - x=min(x1,x2) -> max(x1,x2)
7. Review output ordering:
   - y ascending
   - x strictly increasing within each y
8. Review E12 / E23 mathematical definitions。
9. Review signed arithmetic constraints:
   - coordinate/difference width
   - product width
   - edge-result width
   - explicit sign extension
   - explicit signed comparisons
10. Review E31 elimination rationale。
11. Review final scan -> IDLE / busy release behavior。
12. Identify all remaining architecture-level ambiguities。
13. Determine whether Architecture is ready for Human / Project Owner
    approval and freeze consideration。
14. Compare the Architecture constraints against the existing RTL
    only to identify obvious architecture mismatch.
15. Produce an explicit review result and required decisions。


OUT OF SCOPE:

- Do not modify RTL.
- Do not modify Architecture Document.
- Do not freeze Architecture.
- Do not approve Architecture on behalf of Human / Project Owner.
- Do not redesign Candidate C.
- Do not introduce Candidate B implementation.
- Do not change interface.
- Do not change latency.
- Do not change protocol.
- Do not change busy semantics.
- Do not change reset behavior.
- Do not change output ordering.
- Do not optimize arithmetic.
- Do not perform RTL coding.
- Do not claim simulation / synthesis / STA / P&R evidence that was not actually executed.
- Do not infer verification PASS from RTL inspection.


DEPENDENCIES:

1. Current Specification must remain authoritative.
2. Current Architecture artifact must be available for review.
3. Any unresolved Specification / Architecture conflict must be
   explicitly reported rather than silently resolved.

If an essential authoritative artifact cannot be identified,
report the missing artifact and classify the affected review item
as NOT VERIFIED / BLOCKED.


FROZEN CONSTRAINTS:

Architecture is NOT YET FROZEN.

However, the following currently documented architecture decisions
must be treated as the working constraints under review and must not
be silently changed:

- top-level interface:
  triangle(clk, reset, nt, xi, yi, busy, po, xo, yo)
- Verilog RTL implementation baseline
- coordinate domain 0~7
- legal triangle x1=x3, y1<y2<y3
- input sequence N/N+1/N+2
- busy assertion at N+1 edge
- asynchronous active-high reset
- Candidate C bounded scan
- y ascending
- x ascending within each y
- E12 / E23 cross-product formulation
- no E31 datapath for Candidate C
- signed arithmetic width discipline
- po=1 means xo/yo valid
- po=0 means xo/yo don't-care
- current final scan -> IDLE working decision

If any of these requires change, report:

ARCHITECTURE CHANGE REQUIRED

and do not silently modify the decision.


IMPLEMENTATION / VERIFICATION REQUIREMENTS:

This is an architecture review task.

No RTL implementation is required.

No simulation PASS may be claimed unless actual simulation evidence
is available.

No synthesis / STA / P&R result may be claimed unless actual evidence
is available.

The review must distinguish:

CONFIRMED
VERIFIED
NOT VERIFIED
PROPOSED
UNKNOWN
CONFLICT
BLOCKED

Architecture reasoning alone must not be labelled VERIFIED.


EXPECTED OUTPUTS:

Produce an Architecture Review Result containing at least:

1. Architecture Review Status
2. Specification Consistency
3. Interface Consistency
4. Timing / Cycle Consistency
5. Reset Consistency
6. Candidate C Consistency
7. Arithmetic / Signedness Consistency
8. Output Ordering Consistency
9. Final Busy / IDLE Assessment
10. RTL-to-Architecture Observations
11. Open Architecture Issues
12. Human Decision Required items
13. Freeze Readiness Assessment
14. Recommended next project action

For every identified issue include:

- Issue
- Severity
- Source
- Why it matters
- Affected artifact
- Required owner
- Required decision
- Status


REQUIRED EVIDENCE:

The task result must include:

- Architecture artifact identity reviewed
- Architecture candidate identity if available
- Specification identity reviewed if available
- RTL artifact identity inspected, if inspected
- No invented formal version number
- Review findings with traceability to source sections / decisions
- Explicit list of unresolved issues
- Explicit list of Human Decision Required items
- Explicit statement whether Architecture is:
  - NOT READY
  - READY FOR HUMAN REVIEW
  - READY FOR FREEZE CONSIDERATION

If RTL was inspected, explicitly identify:

RTL artifact:
triangle.v

RTL formal version / candidate:
UNKNOWN, unless an actual documented identity is found.

Do not convert code inspection into functional verification evidence.


EXIT CRITERIA:

Task is COMPLETE only when:

1. Architecture review has been performed against available
   authoritative artifacts.
2. No known architecture conflict is silently left unresolved.
3. All significant architecture ambiguities are explicitly listed.
4. Human Decision Required items are explicitly identified.
5. Freeze readiness is explicitly classified.
6. Any RTL-to-Architecture mismatch found by inspection is recorded.
7. No Architecture or RTL modification was performed.


FAILURE / BLOCKING RULES:

If Specification and Architecture conflict:

STOP and report CONFLICT.

If required authoritative artifact is unavailable:

report BLOCKED / NOT VERIFIED for the affected review item.

If a decision requires Human / Project Owner approval:

report:

HUMAN DECISION REQUIRED

Do not approve it autonomously.

If current RTL cannot be reliably identified:

do not invent a version or candidate identity.

If review discovers that the current architecture requires a
substantive change before implementation:

report:

ARCHITECTURE CHANGE REQUIRED


FORBIDDEN ACTIONS:

- No RTL modification.
- No Architecture modification.
- No baseline freeze.
- No version invention.
- No silent conflict resolution.
- No implementation optimization.
- No Candidate B implementation.
- No verification PASS claim without evidence.
- No synthesis / STA / P&R claim without evidence.
- No Human approval simulation.
- No assumption that an existing RTL candidate is verified merely
  because it exists.


HANDOFF REQUIREMENTS:

Return the completed Architecture Review Result to AI-0 Project Manager.

The handoff must explicitly state:

SOURCE ROLE:
AI-1 System Architect

DESTINATION ROLE:
AI-0 Project Manager

SOURCE ARTIFACT:
Architecture Review Result

ARCHITECTURE STATUS:
UNDER REVIEW / READY FOR HUMAN REVIEW / NOT READY

CURRENT ARCHITECTURE CANDIDATE:
Candidate C, unless the review finds a documented conflict

RTL ARTIFACT INSPECTED:
triangle.v, if actually inspected

RTL VERSION / CANDIDATE:
actual identifiable identity, otherwise UNKNOWN

VERIFICATION STATUS:
NOT VERIFIED unless actual evidence exists

OPEN ISSUES:
explicit list

HUMAN DECISION REQUIRED:
explicit list

NEXT ACTION:
one recommended next action for AI-0 to evaluate

Do not modify any project baseline or frozen decision as part of
this handoff.

==================================================

END TASK PROMPT

==================================================