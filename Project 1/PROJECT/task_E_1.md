# RTL ENGINEER — TASK 1

## Triangle Rendering Engine — Frozen Architecture RTL Implementation

### 1. Role

你現在是本專案的 **RTL Engineer**。

你的唯一任務是：

> 依照已 Freeze 的 **Triangle Rendering Engine Architecture Specification — Revision A1**，實作最終 RTL module `triangle.v`。

你不是 Architecture Designer，也不是 Project Manager。

因此：

* 不得自行修改 Architecture。
* 不得重新設計 datapath。
* 不得改變 FSM。
* 不得改變 interface。
* 不得改變 clock/reset behavior。
* 不得改變 output protocol。
* 不得改變 busy / po timing。
* 不得改變 arithmetic definition。
* 不得改變 bit width / signedness。
* 不得加入 Architecture 未批准的 optimization。

若你認為 Architecture 有矛盾或 RTL 無法忠實實作，**停止該部分實作並回報 Issue**，不得自行修正 Architecture。

---

# 2. Frozen Baseline

以下文件全部視為 frozen requirements：

1. Project Specification
2. Architecture Specification — Revision A1
3. Final Verification Testbench
4. 已批准的 Architecture Decisions

Architecture Freeze 後，RTL 必須以 Architecture 為唯一 implementation baseline。

---

# 3. Mandatory Top Module

RTL 必須提供：

```verilog
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
```

不得：

* 更改 port 名稱
* 更改 port 順序
* 增加 external port
* 移除 port
* 改變 input/output direction
* 改變 interface protocol

Coordinate：

* `xi`：3-bit unsigned
* `yi`：3-bit unsigned
* `xo`：3-bit output
* `yo`：3-bit output

---

# 4. Clock / Reset

Clock：

* positive-edge triggered
* synchronous datapath/control behavior

Reset：

* active-high
* asynchronous

RTL 必須使用與 Architecture 一致的 asynchronous reset implementation。

Reset 後至少必須保證：

```text
FSM      = IDLE
busy     = 0
po       = 0
xo       = reset value
yo       = reset value
```

所有 state/control/datapath registers 都必須具有明確 reset behavior。

---

# 5. Input Protocol

每個 triangle 由三個 consecutive cycles 輸入：

```text
Cycle N     nt=1   P1
Cycle N+1   nt=0   P2
Cycle N+2   nt=0   P3
```

P1：

```text
(x1, y1)
```

P2：

```text
(x2, y2)
```

P3：

```text
(x3, y3)
```

Geometry constraint：

```text
x1 = x3
y1 < y2 < y3
x2 != x1
```

Coordinates：

```text
0 <= x <= 7
0 <= y <= 7
```

`nt` 只有在 `busy=0` 時有效。

---

# 6. Busy Protocol

Architecture-defined behavior：

```text
P1 input
    ↓
P2 input
    ↓
P3 input
    ↓
processing
    ↓
output
    ↓
RELEASE
    ↓
IDLE
```

重要要求：

> 在 P3 input 所屬 cycle，`busy` 必須已經為 `1`。

Output 最後一個 valid point：

```text
po   = 1
busy = 1
```

下一 cycle：

```text
po   = 0
busy = 1
```

RELEASE 完成後下一 cycle：

```text
po   = 0
busy = 0
```

不得自行縮短或修改此 protocol。

---

# 7. Mandatory FSM

必須依 Architecture 使用以下 FSM：

```text
IDLE
CAPTURE_P2
CAPTURE_P3
INIT_COLUMN

LOWER_INIT
LOWER_TRACE
LOWER_CLAMP

UPPER_INIT
UPPER_TRACE
UPPER_CLAMP

OUTPUT_SCAN
RELEASE
```

不得：

* 移除 Architecture-required state
* 合併 state
* 新增 pipeline state
* 改變 state semantic

除非你發現 Architecture 無法實作，否則不得自行修改 FSM。

---

# 8. Column Storage

必須使用：

```text
ylow[0:7]
yup [0:7]
```

共：

```text
8 x ylow
8 x yup
```

不得加入 valid bit。

不得另外建立 endpoint-only storage 取代 common storage。

Endpoint initialization：

```text
ylow[x1] = y1
yup [x1] = y3

ylow[x2] = y2
yup [x2] = y2
```

Intermediate columns 必須透過 tracer 建立。

---

# 9. Geometry Parameters

必須明確處理：

```text
dx   = x2 - x1
dy12 = y2 - y1
dy32 = y2 - y3
s    = sign(x2 - x1)
```

Required widths：

```text
coordinate : unsigned 3-bit
dx         : signed 4-bit
dy12       : signed 4-bit
dy32       : signed 4-bit
E_trace    : signed 9-bit
```

RTL 必須：

* 明確 signed declaration
* 明確 sign extension
* 明確 expression width
* 明確 signed comparison

不得依賴 Verilog implicit sizing / implicit signedness。

---

# 10. Edge Equations

## LOWER

必須使用：

```text
E_L = dx(y-y1) - (x-x1)dy12
```

inside condition：

```text
sE_L >= 0
```

其中：

```text
sE_L = s * E_L
```

Step：

```text
x += s
    => E -= s*dy12

y += 1
    => E += dx
```

LOWER_INIT：

```text
candidate x = x1 + s
candidate y = y1
E = -s*dy12
```

LOWER_INIT **不得 evaluate candidate**。

---

# 11. LOWER_TRACE

若 candidate inside：

```text
ylow[x] = y
```

若下一個 x 已到 x2：

```text
LOWER 完成
```

否則：

```text
x += s
E -= s*dy12
```

若 candidate outside：

```text
y += 1
E += dx
```

若 y 已達 y2：

```text
ylow[x] = y2
```

然後進入：

```text
LOWER_CLAMP
```

---

# 12. LOWER_CLAMP

對剩餘 intermediate columns：

```text
ylow[x] = y2
```

直到 x2。

不得修改 x2 endpoint column。

---

# 13. UPPER

必須使用：

```text
E_U = (x-x3)dy32 - dx(y-y3)
```

inside condition：

```text
sE_U >= 0
```

Step：

```text
x += s
    => E += s*dy32

y -= 1
    => E += dx
```

UPPER_INIT：

```text
candidate x = x1 + s
candidate y = y3
E = s*dy32
```

UPPER_INIT **不得 evaluate candidate**。

---

# 14. UPPER_TRACE

若 candidate inside：

```text
yup[x] = y
```

若下一個 x 已到 x2：

```text
UPPER 完成
```

否則：

```text
x += s
E += s*dy32
```

若 candidate outside：

```text
y -= 1
E += dx
```

若 y 已達 y2：

```text
yup[x] = y2
```

然後進入：

```text
UPPER_CLAMP
```

---

# 15. UPPER_CLAMP

對剩餘 intermediate columns：

```text
yup[x] = y2
```

直到 x2。

不得修改 x2 endpoint column。

---

# 16. |dx| = 1 Shortcut

這是 Frozen Architecture 的明確 decision。

當：

```text
|x2 - x1| = 1
```

代表不存在 intermediate column。

因此：

```text
INIT_COLUMN
    ↓
OUTPUT_SCAN
```

不得執行：

```text
LOWER_INIT
LOWER_TRACE
LOWER_CLAMP
UPPER_INIT
UPPER_TRACE
UPPER_CLAMP
```

Endpoint columns 已經由 INIT_COLUMN 完成 initialization。

---

# 17. Shared Tracer

LOWER / UPPER 必須共用 tracer datapath：

```text
x_trace
y_trace
E_trace
trace_mode
```

不得實作兩套平行 tracer。

`trace_mode` 必須能明確區分：

```text
LOWER
UPPER
```

---

# 18. Output Scanner

Output 必須：

```text
y ascending
x ascending
```

也就是：

```text
for y = y1 ... y3
    for x = x_left ... x_right
```

其中：

```text
x_left  = min(x1,x2)
x_right = max(x1,x2)
```

candidate：

```text
inside =
    (ylow[x] <= y) &&
    (y <= yup[x])
```

若 inside：

```text
po = 1
xo = x
yo = y
```

若 outside：

```text
po = 0
```

但 scanner 必須繼續。

---

# 19. Registered Output Requirement

`po/xo/yo` 必須為 registered outputs。

Architecture-defined timing：

> Enter `OUTPUT_SCAN` 時初始化 scanner candidate；下一個 rising edge 才產生第一個 registered valid output。

不得改成 combinational output。

不得提前一 cycle 輸出。

不得改變 output latency。

---

# 20. Expected Ordering Example

對：

```text
P1 = (1,1)
P2 = (6,3)
P3 = (1,6)
```

輸出必須：

```text
(1,1)

(1,2)
(2,2)
(3,2)

(1,3)
(2,3)
(3,3)
(4,3)
(5,3)
(6,3)

(1,4)
(2,4)
(3,4)
(4,4)

(1,5)
(2,5)

(1,6)
```

不得改變 ordering。

---

# 21. RTL Coding Requirements

請優先使用清楚、可 synthesis 的 RTL。

要求：

* FSM state 使用明確 encoding / enum-style structure
* sequential logic 與 combinational logic 清楚分離
* 所有 registers 有明確 reset behavior
* 避免 latch
* 避免 implicit signed conversion
* 避免 unsized arithmetic constants 導致 width ambiguity
* arithmetic intermediate 必須使用明確 width
* comparison 必須明確 signed / unsigned
* 不使用不可 synthesis construct
* 不使用 real number
* 不使用 delay-based functional logic
* 不使用 testbench-only construct
* 不加入 assertion 取代 functional RTL
* 不加入 Architecture 未定義的 memory macro
* 不加入 pipeline

RTL 必須可以供：

```text
RTL simulation
Synthesis
Gate-level simulation
SDF simulation
```

使用。

---

# 22. Final Testbench Compatibility

RTL 必須能直接接上 Project Final Verification Testbench：

```text
triangle top(
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
```

不得要求 testbench 修改 interface。

不得要求 testbench 改變 input protocol。

不得要求 testbench 改變 expected output ordering。

---

# 23. Implementation Deliverable

你的主要 deliverable：

```text
triangle.v
```

並在交付時附上簡短 implementation report，內容：

### A. Implemented

列出實作完成的 Architecture sections。

### B. State Mapping

列出每一個 FSM state 的 RTL implementation 對應。

### C. Arithmetic Width

列出：

```text
dx
dy12
dy32
E_trace
```

實際 RTL declaration / extension / comparison 的處理方式。

### D. Protocol Timing

說明：

```text
P1
P2
P3
first output
last output
RELEASE
IDLE
```

的 cycle relationship。

### E. Architecture Compliance

逐項確認是否遵守：

```text
FSM
column storage
shared tracer
edge equations
clamp
x2 termination
|dx|=1 shortcut
registered output
busy protocol
reset
```

---

# 24. 禁止自行宣稱 Verification PASS

你是 RTL Engineer。

因此完成 `triangle.v` **不代表 verification PASS**。

除非真的有 RTL simulation evidence，禁止使用：

```text
PASS
Verified
Correct
Signoff-ready
Timing-clean
Synthesis-clean
```

作為未驗證結果。

若尚未執行 simulation，請標示：

```text
NOT VERIFIED
```

---

# 25. Issue Escalation

若 implementation 過程發現：

* Architecture contradiction
* undefined timing
* impossible state transition
* arithmetic definition ambiguity
* protocol contradiction
* interface contradiction

不得自行修改 Architecture。

請以以下格式回報：

```text
ISSUE ID:
TITLE:

SYMPTOM:

FAILING STAGE:

ROOT CAUSE:

AFFECTED ARCHITECTURE SECTION:

RTL IMPACT:

PROPOSED OPTIONS:

SIDE EFFECTS:

RECOMMENDATION:

STATUS:
```

在 Project Manager / Architecture Owner 決定前，不得以 workaround 形式偷偷修改 frozen behavior。

---

# 26. Task Completion Condition

Task 1 完成條件：

1. `triangle.v` 已完整產生。
2. Top module/interface 完全符合 specification。
3. Frozen FSM 已實作。
4. Frozen datapath 已實作。
5. Frozen arithmetic 已實作。
6. Frozen column storage 已實作。
7. `|dx|=1` shortcut 已實作。
8. LOWER / UPPER tracer 已實作。
9. Y2 clamp 已實作。
10. x2 termination 已實作。
11. registered output 已實作。
12. busy / RELEASE protocol 已實作。
13. asynchronous active-high reset 已實作。
14. RTL 尚未經 simulation 時，明確標示 `NOT VERIFIED`。
15. 任何 Architecture conflict 必須先回報，而不是自行修改。

---

## 最終指令

**請現在直接開始實作 `triangle.v`。**

不要重新設計 Architecture。

不要提出新的 Architecture。

不要自行修改 Frozen Decision。

先完成 RTL implementation，再輸出：

1. `triangle.v`
2. Implementation Report
3. Architecture Compliance Checklist
4. 若有問題，依 Issue Escalation 格式回報

這份 RTL 將在下一階段交由 **RTL Verification Engineer** 進行獨立驗證。
