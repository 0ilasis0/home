請嚴格按照以下結構輸出：

HANDOFF — [CURRENT PHASE]
1. Project Identity
Project

[project name]

Current Phase

[current phase]

Previous Phase

[previous phase]

Next Phase

[next phase]

Handoff Date

[date if known]

2. Phase Objective

說明本階段原本要完成什麼。

3. Current Status

使用：

COMPLETE
PARTIAL
BLOCKED
NOT VERIFIED

並簡述原因。

4. Confirmed Decisions

列出目前已正式確認的設計決策。

格式：

Item	Decision	Status	Evidence
Clock	...	CONFIRMED	...
Reset	...	CONFIRMED	...
Pipeline	...	CONFIRMED	...
Interface	...	CONFIRMED	...
5. Proposed / Unconfirmed Decisions

列出曾經提出但尚未正式確認的內容。

格式：

Item	Proposal	Status	Reason
...	...	PROPOSED	...
6. Unknown / Missing Information

列出下一階段仍然缺少的資訊。

例如：

technology node
target frequency
library
reset requirement
protocol detail
timing constraint

不得自行補值。

7. Architecture Summary

簡潔描述目前 architecture。

包括：

major blocks
data path
control path
pipeline
memory
clock domains
reset domains
CDC
important dependencies

不要重新設計。

8. Interface Summary

列出：

module
input
output
width
direction
clock
reset
protocol
timing assumptions

如果不知道：

UNKNOWN

9. RTL Status

列出：

RTL files
modules
completed modules
incomplete modules
known RTL issues
known synthesis concerns
10. Verification Status

列出：

testbench
test cases
assertions
coverage information if available
simulation tools
simulation results
unresolved failures
11. Tool / EDA Evidence

如果有實際結果，列出：

Lint

Tool:
Result:
Warnings:
Errors:

Simulation

Tool:
Result:
Tests:
Failures:

Synthesis

Tool:
Result:
Area:
Timing:
Warnings:

STA

Tool:
Result:
Worst Slack:
Critical Paths:

P&R

Tool:
Result:
Utilization:
Congestion:
Timing:

DRC

Tool:
Result:
Violations:

LVS

Tool:
Result:
Mismatch:

沒有資料就寫：

NOT RUN

12. Known Problems

使用以下格式：

Problem [ID]
Description

...

Evidence

...

Root Cause

CONFIRMED / SUSPECTED / UNKNOWN

Current Status

OPEN / RESOLVED / BLOCKED

Required Action

...

Regression Required

...

13. Important Design Decisions History

記錄會影響後續工程的重大決策。

格式：

ID	Decision	Reason	Status
DEC-001	...	...	CONFIRMED
14. Files / Artifacts

列出目前階段實際存在或產生的檔案。

例如：

rtl/top.v
rtl/fifo.v
tb/tb_top.v
reports/synthesis.rpt
reports/sta.rpt

不要虛構檔案。

15. Next Phase Requirements

明確告訴下一個 AI：

Must Preserve

哪些東西絕對不能改。

Must Verify

哪些東西下一階段一定要驗證。

Must Resolve

哪些問題必須處理。

Allowed Changes

下一階段可以修改什麼。

Forbidden Changes

下一階段禁止自行修改什麼。

16. Recommended Next Actions

按照優先順序列出下一階段應該做的事情。

格式：

...
...
...

不要自行執行這些工作。

17. Handoff Constraints

最後明確寫出：

下一個 AI 的角色
下一個 AI 可以做什麼
下一個 AI 不可以做什麼
哪些問題必須先詢問人類
哪些項目需要實際 EDA evidence
18. Critical Warnings

列出下一個 AI 最容易犯錯的地方。

只列真正重要的事項。

FINAL CONSISTENCY CHECK

在輸出 HANDOFF.md 前，執行以下檢查：

是否把推測寫成事實？
是否遺漏未解決問題？
是否遺漏失敗的驗證？
是否有未驗證的 PASS？
是否把 PROPOSED 寫成 CONFIRMED？
是否存在 specification conflict？
是否存在 architecture conflict？
是否存在 clock/reset ambiguity？
是否存在 interface ambiguity？
下一個 AI 是否能只依靠這份文件理解目前狀態？

如果有任何問題，先修正 HANDOFF.md。

OUTPUT REQUIREMENT

只輸出完整的 HANDOFF.md 內容。

不要額外解釋。

不要繼續修改 RTL。

不要提出新的 architecture。

不要執行下一階段工作。