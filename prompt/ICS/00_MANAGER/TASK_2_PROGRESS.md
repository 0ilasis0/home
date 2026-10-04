# RTL COMPLETION → VERIFICATION TASK

RTL Engineer 已完成目前 RTL Task。

請依照你已載入的 Project Manager Rules、Task Dispatch Rules 與 RTL → Verification Rules：

1. 讀取目前 Project State。

2. 讀取 RTL Engineer 的 Task Result。

3. 讀取並檢查實際 RTL artifact。

4. 確認 RTL artifact 的 version / baseline。

5. 確認目前 RTL increment 實際已實作的功能。

6. 確認該 increment 尚未完成驗證的項目。

7. 對照 Specification、Frozen Architecture、Interface、Clock/Reset、Verification Plan。

8. 判斷目前是否已滿足進入 Verification 的條件。

9. 若可以進入 Verification，指定 AI-3 RTL Verification Engineer。

10. 生成一份可直接交給 AI-3 的 Verification Task Prompt。

11. Verification Task 必須明確指定：

    * RTL artifact

    * RTL version / baseline

    * verification scope

    * out-of-scope

    * required source-of-truth

    * required evidence

    * exit criteria

    * failure handling

12. 不執行 Verification。

13. 不修改 RTL。

14. 不修改 Architecture。

15. 不修改 Specification。

16. 不自行宣布 Verification PASS。

若發現：

* compile failure

* RTL artifact 不完整

* RTL version 不明

* Architecture / Specification conflict

* prerequisite 未完成

* 無法確認 verification baseline

則不要產生一般 Verification Task。

請依情況將狀態標記為：

BLOCKED

或

HUMAN DECISION REQUIRED

並說明原因。

如果可以進入 Verification，輸出：

## CURRENT PROGRESS

## RTL VERIFICATION READINESS

## VERIFICATION TASK PROMPT

## BLOCKER / HUMAN DECISION

若沒有 blocker：

NONE