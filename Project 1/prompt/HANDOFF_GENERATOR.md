現在請停止目前階段的主要工作，不要繼續修改設計。

你的任務是：

根據目前這個 conversation 中「已確認的資訊、已完成的工作、實際產生的結果與尚未解決的問題」，產生一份可以交給下一個 AI / 下一個工程階段直接使用的 HANDOFF.md。

重要規則
1. 只記錄已確認的資訊

不要把推測、猜測、可能性寫成事實。

如果某項資訊沒有被確認，標記：

UNKNOWN

如果存在不同說法或衝突，標記：

CONFLICT

如果需要下一階段確認，標記：

NEEDS_CONFIRMATION

2. 不要重新設計

不要在交接文件中偷偷修改：

architecture
specification
interface
clock
reset
pipeline
protocol
latency
throughput
implementation strategy

交接文件的目的不是重新設計，而是保存目前狀態。

3. 必須區分三種資訊

所有重要內容必須盡量區分：

CONFIRMED

已經確認的設計決策。

PROPOSED

曾經提出但尚未正式確認的方案。

UNKNOWN

目前沒有足夠資料。

不得把 PROPOSED 寫成 CONFIRMED。

4. 必須記錄變更

整理目前階段中重要的設計決策與變更。

對每個重要變更說明：

原始狀態
新狀態
變更原因
是否已驗證
是否需要後續驗證
5. 必須記錄驗證證據

不要只寫：

Simulation PASS

必須說明：

使用什麼工具
測試什麼
測試結果
是否有 warning
是否有 failure
是否完整驗證

如果沒有實際證據：

NOT VERIFIED

6. 必須記錄問題

列出目前所有尚未解決的：

specification issue
architecture issue
RTL issue
verification issue
synthesis issue
timing issue
physical design issue
DRC/LVS issue

每個問題至少包含：

Problem

問題是什麼？

Evidence

有什麼證據？

Current Status

目前狀態。

Suspected Root Cause

如果尚未確認，必須明確標示為 suspected。

Required Action

下一階段需要做什麼。

7. 不要隱藏失敗

如果目前階段曾經：

simulation fail
assertion fail
lint warning
synthesis warning
timing violation
implementation failure

必須保留。

不能因為後來修好了就把歷史問題完全刪除。

只需要標記：

RESOLVED

並記錄解決方式。

8. 不要聲稱 Sign-off

除非有實際工具證據，禁止使用：

PASS
SIGNOFF
DRC CLEAN
LVS CLEAN
TIMING CLEAN
PRODUCTION READY

如果沒有證據，使用：

NOT VERIFIED

9. 下一個 AI 不應該需要閱讀整個 conversation

HANDOFF.md 必須讓一個完全不知道前情的新 AI，只閱讀：

GLOBAL_RULES
本 HANDOFF.md
本階段產生的相關檔案

就能理解目前專案狀態。