# VERIFICATION COMPLETION → PROGRESS GATE

AI-0 Project Manager：

Verification Engineer 已完成目前 RTL increment 的 Verification Task。

請依照你已載入的 Project Manager Rules、Task Dispatch Rules、RTL → Verification Rules 與既有 Project State，判斷目前 increment 是否具備推進條件。

本 Gate 的目的不是重新執行 Verification，而是確認：

> **目前 Verification evidence 是否足以證明本 RTL increment 已達到其既定 Verification / Exit Criteria，並允許 Project 進入下一個 Task。**

---

## 0. GATE PRINCIPLES

### 0.1 Verification Baseline 必須在 Verification 開始前已被識別

RTL artifact、RTL version / increment identity、Verification baseline 應在 RTL → Verification handoff 時已經明確。

本 Gate 可以檢查其 traceability，但：

* 不應在 Verification 完成後才首次建立 RTL identity。
* 不得自行猜測或補造 RTL version。
* 不得因為 Testbench version 可以識別，就推定 RTL version。
* 若無法證明 Verification 對應到正確 RTL artifact，則不得將 Verification result 升級為 Verified Baseline。

---

### 0.2 Evidence Requirement 必須來自既有要求

本 Gate 不得自行增加 Verification requirement。

「Required Evidence」只能來自：

1. Verification Task
2. Verification Plan
3. 已確認的 Increment Exit Criteria
4. Specification / Frozen Architecture 中明確要求的 verification evidence

例如：

* functional coverage
* code coverage
* formal evidence
* regression report
* detailed verification report

只有在上述 source 明確要求時，才可作為 Progress Gate 的必要條件。

如果某項 evidence 未被要求，則不得僅因其不存在而阻止 progress。

---

### 0.3 Functional PASS 與 Evidence Completeness 必須分開判斷

必須區分：

* Functional Verification Result
* Verification Evidence Completeness
* Baseline Traceability
* Overall Progress Decision

例如：

```text
Functional Result:
PASS

Evidence Completeness:
COMPLETE

Baseline Traceability:
COMPLETE

Overall:
VERIFIED — READY TO PROGRESS
```

或：

```text
Functional Result:
PASS

Evidence Completeness:
INCOMPLETE

Baseline Traceability:
INCOMPLETE

Overall:
VERIFICATION INCOMPLETE
```

不要把「Verification functional test PASS」與「Project verification gate PASS」混為同一件事。

---

# 1. READ CURRENT STATE

讀取並確認：

1. Current Project State
2. Current RTL artifact
3. RTL version / baseline / increment identity
4. Verification Task
5. Verification Testbench
6. Verification Result / Report
7. Simulation logs
8. Waveform（若存在或被要求）
9. Coverage evidence（若存在或被要求）
10. Specification
11. Frozen Architecture
12. Interface
13. Clock / Reset
14. Verification Plan
15. Increment Exit Criteria
16. Previous handoff / decision records（若存在）

---

# 2. VERIFY THE VERIFICATION BASELINE

確認 Verification 實際驗證的是：

* 哪一個 RTL artifact
* 哪一個 RTL version / baseline
* 哪一個 increment
* 哪一組 testbench / verification environment

確認：

```text
RTL Artifact
    ↕
RTL Version / Increment
    ↕
Verification Baseline
    ↕
Testbench
    ↕
Simulation Result
```

必須可以建立明確 traceability。

不得將：

* 不同 RTL version
* 不同 RTL artifact
* 不同 testbench version
* 不同 simulation result

混合判斷。

如果無法建立 exact-baseline traceability：

`Baseline Traceability = INCOMPLETE`

不得將該 increment 標記為：

`VERIFIED — READY TO PROGRESS`

但不要因此宣稱 Functional Verification FAIL。

---

# 3. INSPECT ACTUAL VERIFICATION EVIDENCE

不要只依賴 Verification Engineer 的文字結論。

實際檢查可取得的：

* Testbench
* Test cases
* Reference model
* Checker / scoreboard
* Simulation result
* Regression result
* Failure logs
* Coverage evidence（若有）
* Waveform evidence（若有）
* Verification report（若有）

確認：

1. Verification 是否真的執行。
2. 執行的 test 是否屬於本 increment。
3. Testbench 是否以 Specification / Frozen Architecture 為 expected behavior source。
4. Reference model / checker 是否具有足夠獨立性。
5. Test result 是否有實際 evidence。
6. Required verification 是否已完成。

---

# 4. CHECK AGAINST SOURCE OF TRUTH

以：

1. Specification
2. Frozen Architecture
3. Interface
4. Clock / Reset
5. Verification Plan
6. Increment Exit Criteria

作為 intended behavior 與 verification requirement 的主要依據。

RTL 與 Testbench 都不是 specification。

確認：

* Required behavior 是否已驗證
* Interface behavior 是否已驗證
* Cycle / timing behavior 是否已驗證
* Boundary / corner cases 是否已驗證
* 本 increment 的 required functionality 是否已驗證
* 是否存在未驗證的 required behavior
* Required verification evidence 是否已取得

---

# 5. DETERMINE EVIDENCE REQUIREMENTS

建立兩個清單：

## REQUIRED EVIDENCE

只列出由 Verification Task、Verification Plan 或 Increment Exit Criteria 明確要求的 evidence。

例如：

* required directed tests
* required regression
* required coverage
* required waveform
* required report
* required checker result

## OPTIONAL / SUPPORTING EVIDENCE

例如：

* additional waveform
* additional coverage
* debug traces
* supplementary logs

Optional / Supporting Evidence 不得單獨成為 Progress Blocker。

---

# 6. CLASSIFY CURRENT STATE

將目前狀態分類為以下其中之一。

## A. VERIFIED — READY TO PROGRESS

只有在：

* RTL artifact 明確
* RTL version / baseline 明確
* Verification 對應正確 RTL artifact / version
* Required verification 已執行
* Required evidence 已取得
* Required tests pass
* 無未解決 verification failure
* 無 architecture / specification conflict
* 無 blocker
* 本 increment 達到既定 Exit Criteria

時，才能判定：

`VERIFIED — READY TO PROGRESS`

---

## B. VERIFICATION FAIL

若存在：

* failing test
* checker failure
* scoreboard mismatch
* protocol violation
* timing / cycle mismatch
* unexpected output
* reference-model mismatch
* required regression failure

則判定：

`VERIFICATION FAIL`

不要因為部分 tests PASS 就忽略 failure。

產生 RTL Fix Task 所需的 failure evidence。

---

## C. VERIFICATION INCOMPLETE

若：

* required test 尚未執行
* required scenario 尚未完成
* required evidence 尚未取得
* regression requirement 尚未完成
* verification scope 未覆蓋完整 increment
* baseline traceability 尚未完成
* Increment Exit Criteria 尚未滿足

則判定：

`VERIFICATION INCOMPLETE`

注意：

> Functional tests PASS 並不等於 Progress Gate PASS。

但：

> Verification Incomplete 也不等於 Functional Verification FAIL。

---

## D. BLOCKED

若：

* simulation environment failure
* missing artifact
* missing tool output
* missing required input
* testbench infrastructure issue
* 無法取得必要 evidence

導致無法判斷 correctness，則：

`BLOCKED`

不得推進 RTL increment。

---

## E. ARCHITECTURE / SPECIFICATION CONFLICT

若發現：

* RTL 與 Frozen Architecture 不一致
* Verification expectation 與 Frozen Architecture 衝突
* Specification 與 Architecture 衝突
* requirement ambiguity

不要自行選擇其中一個。

標記：

`HUMAN / ARCHITECT DECISION REQUIRED`

---

# 7. IMPORTANT INDEPENDENCE RULE

不要因為：

* RTL 已經產生某種 behavior
* Testbench 已經針對 RTL 寫好
* Testbench PASS

就自動認定 behavior 正確。

判斷順序必須是：

```text
Specification / Frozen Architecture
            ↓
Intended Behavior
            ↓
Independent Verification
            ↓
Evidence
            ↓
RTL Correctness
```

而不是：

```text
RTL
 ↓
Testbench
 ↓
PASS
 ↓
Assumed Correct
```

如果 Verification expectation 與 RTL implementation 不一致：

* 不修改 expected result 來配合 RTL。
* 不修改 specification。
* 不自行選擇 Architecture interpretation。
* 按 failure / conflict 流程處理。

---

# 8. PROGRESS DECISION

如果狀態為：

`VERIFIED — READY TO PROGRESS`

則：

1. 將目前 RTL increment 標記為 Verified Baseline。
2. 更新 Project State。
3. 記錄 Verification evidence。
4. 確認目前 baseline / version。
5. 記錄 Verification result。
6. 觸發既有：

`TASK PROGRESS → NEXT TASK`

7. 由 AI-0 根據 Project State 決定下一個 Task。
8. 下一個 Task 若為 RTL implementation，交給 AI-2 RTL Engineer。

---

如果不是：

### VERIFICATION FAIL

產生 RTL Fix Task 或依 failure classification 處理。

### VERIFICATION INCOMPLETE

指定缺少的 verification activity / evidence。

### BLOCKED

指定 blocker 與解除條件。

### HUMAN / ARCHITECT DECISION REQUIRED

列出 conflict 與需要 Human / Architect 決策的事項。

---

# 9. DO NOT

AI-0 不得：

* 修改 RTL
* 修改 Testbench
* 修改 expected result
* 修改 Specification
* 修改 Frozen Architecture
* 為了讓 regression PASS 而降低 verification requirement
* 將未驗證項目標記為 VERIFIED
* 將部分 PASS 標記為完整 PASS
* 自行修正 architecture conflict
* 自行宣布 ASIC signoff
* 因為「看起來合理」而跳過 required evidence
* 自行新增 Verification Plan 未要求的 evidence requirement
* 因為沒有 optional waveform / coverage 就阻止 progress
* 猜測不存在的 RTL version / baseline
* 將 Functional PASS 自動等同於 Progress Gate PASS

---

# 10. OUTPUT

輸出：

# VERIFICATION PROGRESS GATE

## CURRENT RTL BASELINE

* RTL Artifact:
* RTL Version:
* Increment:
* Verification Baseline:
* Baseline Traceability:

## VERIFICATION STATUS

* Required Verification:
* Executed:
* Passed:
* Failed:
* Incomplete:
* Blocked:

### Functional Verification Result

* PASS / FAIL / NOT VERIFIED

### Required Evidence

* COMPLETE / INCOMPLETE

### Optional / Supporting Evidence

* Available / Not Available / Not Required

### Verification Evidence

* Simulation:
* Regression:
* Coverage:
* Waveform:
* Verification Report:
* Other Evidence:

## REQUIREMENT / ARCHITECTURE CHECK

* Specification Compliance:
* Architecture Compliance:
* Interface Compliance:
* Clock / Reset Compliance:
* Verification Plan Coverage:
* Increment Exit Criteria:

## PROGRESS DECISION

選擇一項：

`VERIFIED — READY TO PROGRESS`

或

`VERIFICATION FAIL`

或

`VERIFICATION INCOMPLETE`

或

`BLOCKED`

或

`HUMAN / ARCHITECT DECISION REQUIRED`

## NEXT ACTION

若 VERIFIED：

`TRIGGER: TASK PROGRESS → NEXT TASK`

若 FAIL：

指定需要 RTL Fix 的 failure 與 evidence。

若 INCOMPLETE：

指定尚未完成的 Verification activity / required evidence。

若 BLOCKED：

指定 blocker 與解除條件。

若 Architecture / Specification conflict：

列出 conflict 與需要 Human / Architect 決策的事項。

## HUMAN DECISION REQUIRED

若無：

`NONE`
