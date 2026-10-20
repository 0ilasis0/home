# COMMON

1. 先閱讀與記憶並不動作，直到我讓你開始行動

2. 理解完任務後可以開始行動了

3. 幫我依照該prompt基礎來修改，並非重新發明而是把目前已經形成prompt新增或刪除部分Prompt

---



# ARCHITTECT prompt

1. 現在正式建立 ARCHITECTURE.md；但不是「重新發明 Architecture」，而是把目前已經形成的 Architecture decisions 系統化、可追溯地固化成第 N 個正式 Architecture artifact

2. 接下來我會讓你和manager ai進行工作，因此接到任務後除了產出給manager prompt外，也需要一份精簡核心的報告給我

---



# Manager prompt

1. 接下來我會讓你和不同 endineer ai進行工作，因此給予任務後除了產出給其他 AI prompt外，也需要一份精簡核心的進度報告給我

## Architecture 已完成 Human Review。
請將目前已審核的 Architecture 設定為正式 Architecture Baseline。

請執行以下 Project Control 工作：
1. 將 Architecture 狀態更新為 FROZEN。
2. 記錄目前 Architecture Version。
3. 更新 PROJECT/00_PROJECT_STATE.md。
4. 記錄 Architecture Freeze 這項 Human Decision。
5. 更新 PROJECT/09_DECISION_LOG.md。
6. 建立或更新 Architecture → RTL Engineer 的 Handoff。
7. 明確列出 RTL Engineer 必須遵守的 frozen decisions。
8. 明確列出 RTL Engineer 不得自行修改的項目。
9. 列出目前仍存在的 Open Issues。
10. 列出 RTL Phase 開始前仍需要處理的 blocking issues。

不要重新設計 Architecture。
不要修改 Architecture 的技術內容。
不要產生 RTL。
如果目前沒有 blocking issue，請明確標示：
READY FOR RTL IMPLEMENTATION


---



# RTL VERIFICATION

## 設計 testbench的第一步

1. 先依找此要求設計testbench，下一步會給你RTL code在依照過去prompt限制與規範，利用先前設計的testbench來修改新的testbench

