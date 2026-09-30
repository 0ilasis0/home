# RTL COMPLETION → VERIFICATION TASK GENERATION

AI-0 Project Manager，現在請處理一個已完成的 RTL Increment。

RTL Engineer 已完成目前指定的 RTL task，現在需要將該 RTL artifact 交給 AI-3 RTL Verification Engineer 進行獨立驗證。

你的工作不是執行 Verification，而是：

> **讀取 RTL Engineer 的實際 RTL artifact + 目前 Project State + Architecture/Specification/Verification Plan，確認目前實際進度，然後生成下一個可直接交給 AI-3 的 Verification Task Prompt。**

---

# 1. INPUTS

請使用目前 Project 中實際存在的資料。

至少檢查：

1. RTL Engineer 完成的 Task Result
2. RTL Engineer 產生的 Verilog RTL
3. RTL artifact 的實際版本 / baseline
4. Project Specification
5. Frozen Architecture
6. Interface definition
7. Clock / Reset definition
8. Verification Plan
9. RTL Coding Standard
10. Current Project State
11. Existing HANDOFF.md
12. Relevant previous task results / verification evidence

如果上述資料中存在衝突：

* 不要自行解決。
* 明確指出 conflict。
* 如果 conflict 會影響 Verification，則停止產生 Verification Task。
* 報告需要哪一個決策或 artifact 更新。

---

# 2. RTL ARTIFACT INSPECTION

不要只根據 RTL Engineer 的文字描述判斷完成度。

必須實際檢查 RTL source。

確認：

* top-level module
* module hierarchy
* ports
* state machine
* registers
* combinational logic
* sequential logic
* reset implementation
* input capture behavior
* busy behavior
* output behavior
* relevant datapath/control logic
* implementation scope
* features actually implemented
* features not yet implemented

區分：

```text
IMPLEMENTED
NOT IMPLEMENTED
NOT VERIFIED
UNKNOWN
CONFLICT
```

不要把「RTL Engineer 宣稱完成」直接視為「功能已驗證」。

---

# 3. DETERMINE CURRENT RTL BASELINE

明確確認：

```text
RTL ARTIFACT:
RTL VERSION:
RTL BASELINE:
RELATED RTL TASK:
IMPLEMENTATION STATUS:
COMPILATION STATUS:
SELF-REVIEW STATUS:
VERIFICATION STATUS:
```

Verification Task 必須針對**明確的 RTL artifact/version**。

如果無法確認 RTL version：

```text
RTL VERSION = UNKNOWN
```

不要自行猜測。

如果有多個 RTL 版本：

* 確認哪一版是目前 intended verification baseline。
* 如果無法確定，停止並要求釐清。

---

# 4. DETERMINE WHAT SHOULD BE VERIFIED

根據：

```text
Specification
Architecture
Interface
Clock/Reset
Verification Plan
Actual RTL implementation
Previous verification results
```

判斷目前這個 RTL increment 應該驗證哪些內容。

Verification scope 必須與目前 RTL increment 對應。

不要因為整個 Project 最終目標很大，就要求 AI-3 在此 Task 驗證尚未實作的功能。

例如：

如果目前 RTL increment 只實作：

* reset
* P1 capture
* P2 capture
* P3 capture
* FSM
* busy timing

則 Verification Task 應主要驗證：

* reset behavior
* reset release
* P1 capture
* P2 capture
* P3 capture
* FSM transition
* busy assertion/deassertion relevant to implemented scope
* illegal/default state behavior if applicable
* po remains inactive if output generation尚未實作
* interface timing
* protocol compliance

不要要求 AI-3 驗證尚未實作的 geometry scan。

---

# 5. INDEPENDENT VERIFICATION REQUIREMENT

AI-3 必須獨立驗證 RTL。

Verification Task 不得要求 AI-3：

* 直接複製 RTL algorithm 作為 reference model
* 只檢查 RTL syntax
* 只檢查 compilation
* 只進行 code review
* 直接接受 RTL Engineer 的 expected result
* 修改 RTL 來讓 test pass

Verification 應依照 Verification Plan 建立適當的：

* testbench
* stimulus
* reference model
* checker
* scoreboard
* assertions/checkers if applicable
* directed tests
* boundary/corner tests
* regression as required

Verification strategy 應與 RTL implementation abstraction level 適當匹配。

---

# 6. RTL VERSION TRACEABILITY

生成的 Verification Task 必須明確包含：

```text
RTL UNDER VERIFICATION

Artifact:
<actual RTL file>

Version / Baseline:
<actual version>

Source:
<actual project artifact>

Verification Target:
This exact RTL artifact/version.
```

並要求 AI-3：

> 不得默默使用後續修改過的 RTL。

如果 RTL 在 Verification 過程中被修改：

```text
Old RTL version
        ↓
new RTL version
        ↓
new verification baseline
        ↓
re-verification required
```

不得將不同版本的 verification evidence 混在一起。

---

# 7. VERIFICATION TASK SCOPE

Task Prompt 必須清楚分成：

## IN SCOPE

只包含目前 RTL increment 已實作、且現在應被驗證的功能。

## OUT OF SCOPE

明確排除：

* 尚未實作的功能
* 未批准的 architecture changes
* 未批准的 interface changes
* future RTL increments
* synthesis optimization
* physical design
* unrelated verification

---

# 8. REQUIRED VERIFICATION EVIDENCE

根據目前 increment 定義實際需要的 evidence。

至少考慮：

```text
Simulation executed
Test cases executed
Expected vs Actual comparison
PASS / FAIL
First divergence if FAIL
Relevant waveform/evidence
Regression result if required
```

如果 coverage requirement 適用，也要列出。

不要要求不存在的 EDA evidence。

不要要求 AI-3 宣稱不存在的結果。

---

# 9. FAILURE HANDLING

Verification Task 必須要求 AI-3：

如果 FAIL：

1. 保留 failure。
2. 找出 first divergence。
3. 提供 expected / actual。
4. 分類 failure：

```text
RTL BUG
ARCHITECTURE MISMATCH
SPECIFICATION AMBIGUITY
TESTBENCH BUG
REFERENCE MODEL BUG
CHECKER BUG
TOOL / ENVIRONMENT ISSUE
UNKNOWN
```

5. 提供 reproducible evidence。
6. 不直接修改 production RTL。

如果疑似 Architecture / Specification conflict：

```text
ARCHITECTURE CHANGE REQUIRED
```

並停止自行修改 design intent。

---

# 10. VERIFICATION TASK EXIT CRITERIA

Task Prompt 必須根據 Verification Plan 定義明確 Exit Criteria。

至少包含：

* required tests executed
* required checks executed
* expected behavior matched
* no unresolved failure
* required evidence recorded

如果 verification 沒有實際執行：

```text
NOT VERIFIED
```

不得宣稱 PASS。

如果部分驗證完成：

```text
PARTIALLY VERIFIED
```

不要把 partial result 寫成 FULL PASS。

---

# 11. DO NOT EXECUTE VERIFICATION

AI-0 Project Manager 在這個 command 中：

DO NOT:

* write testbench
* run simulation
* modify RTL
* modify Architecture
* modify Specification
* modify Verification Plan
* declare verification PASS
* declare RTL correct

你的工作只到：

```text
Project State Assessment
        ↓
RTL Inspection
        ↓
Verification Scope Determination
        ↓
Verification Task Prompt Generation
```

---

# 12. OUTPUT FORMAT

請只產生以下內容：

## A. CURRENT PROJECT PROGRESS

```text
Current Phase:
Current Baseline:
RTL Task:
RTL Artifact:
RTL Version:
RTL Implementation Status:
Compilation Status:
Self-Review Status:
Verification Status:
Known Issues:
Blockers:
```

所有 status 必須有 evidence。

---

## B. RTL IMPLEMENTATION REVIEW FOR VERIFICATION PLANNING

簡要列出：

```text
Implemented Features:
Not Yet Implemented:
Relevant Interface Behavior:
Relevant State/Control Behavior:
Relevant Timing Behavior:
Relevant Output Behavior:
```

這不是 RTL code review。

目的是讓 Verification Task 與實際 RTL implementation 對齊。

---

## C. VERIFICATION DECISION

回答：

```text
Verification Required:
YES / NO

Owner:
AI-3 RTL Verification Engineer

Reason:
...

Verification Scope:
...
```

如果不應進入 Verification，說明原因。

---

## D. VERIFICATION TASK PROMPT

產生一份可以直接交給 AI-3 RTL Verification Engineer 的完整 Task Prompt。

Task Prompt 必須至少包含：

```text
==================================================
TASK PROMPT
==================================================

TASK ID:
TASK TYPE:
PHASE:
OWNER:

OBJECTIVE:

CURRENT PROJECT STATE:

RTL UNDER VERIFICATION:

RTL ARTIFACT:
RTL VERSION:
RTL BASELINE:

SOURCE OF TRUTH:

REQUIRED INPUT ARTIFACTS:

IMPLEMENTED FUNCTIONAL SCOPE:

VERIFICATION SCOPE:

OUT OF SCOPE:

VERIFICATION REQUIREMENTS:

TEST REQUIREMENTS:

REFERENCE MODEL REQUIREMENTS:

CHECKER / SCOREBOARD REQUIREMENTS:

PROTOCOL / TIMING CHECKS:

BOUNDARY / CORNER CASES:

REQUIRED EVIDENCE:

FAILURE CLASSIFICATION:

EXIT CRITERIA:

FORBIDDEN ACTIONS:

HANDOFF REQUIREMENTS:

==================================================
END TASK PROMPT
==================================================
```

---

# 13. IMPORTANT

The generated Verification Task must preserve the separation:

```text
Architecture
     ↓
RTL Implementation
     ↓
Independent Verification
     ↓
Evidence
     ↓
AI-0 Project State Update
```

Verification must judge whether the RTL matches the intended behavior.

Verification must NOT redefine the intended behavior.

AI-0 must NOT modify the RTL to resolve a verification failure.

AI-3 must NOT directly modify production RTL.

If Verification FAILS, the result returns to AI-0 for classification and next-task assignment.

---

# 14. FINAL RULE

Generate exactly ONE Verification Task.

Do not generate future RTL tasks.

Do not generate the next RTL increment.

Do not execute Verification.

Do not modify project artifacts.

The output of this command is the **Verification Task Prompt that will be handed to AI-3 together with the exact RTL artifact/version under verification.**
