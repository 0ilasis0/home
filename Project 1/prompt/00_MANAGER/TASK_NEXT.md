# TASK PROGRESS → NEXT TASK

請依據目前 Project State、既有 Source of Truth、上一個 Task 的執行結果與 Evidence：

1. 更新目前 Project Progress 判斷。
2. 確認上一個 Task 是否真正完成。
3. 確認是否存在 FAIL、BLOCKER、CONFLICT 或 Human Decision Required。
4. 確認目前 Phase、Baseline 與各 AI 的責任分工是否仍然正確。
5. 判斷目前 Dependency 是否允許進入下一個工作。
6. 依照既有的 `NEXT TASK DISPATCH COMMAND` 規則，決定下一個唯一可執行 Task。
7. 生成下一個 Task Prompt。

要求：

* 不執行下一個 Task。
* 不修改 RTL。
* 不修改 Architecture。
* 不修改 Specification。
* 不自行解決 Architecture / Specification conflict。
* 不把未驗證內容視為完成。
* 不一次產生多個未來 Task。
* 若目前被 blocker 阻塞，下一個 Task 必須優先處理 blocker。
* 若需要 Human Decision，停止產生工程實作 Task，改為提出 Human Decision Required。
* 若上一個 Task 尚未滿足 Exit Criteria，不得進入下一個 Task。

輸出：

## CURRENT PROGRESS

目前 Phase、Baseline、完成進度、Verification Status、Blocker。

## TASK STATUS

上一個 Task 的實際狀態與 Evidence。

## NEXT TASK DECISION

說明為何下一步是這個 Task。

## NEXT TASK PROMPT

提供可直接交給對應 AI 的完整 Task Prompt。

## HUMAN DECISION / BLOCKER

若沒有，填 `NONE`。
