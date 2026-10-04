# RTL Modification Instruction

## ARCH-CHANGE-OUTPUT-SCAN-EARLY-EXIT

**Project:** Triangle Rendering Engine
**RTL Baseline:** RTL Version 1.0.2
**Baseline ID:** `RTL-BASE-A1-TASK1-BUGFIX-2`
**Architecture Status:** A1 PENDING — Change Under Review
**Change Status:** **IMPLEMENTATION LIMITED / NOT ARCHITECTURE FREEZE**

---

## 1. Modification Objective

本次只修改既有 RTL 的 `OUTPUT_SCAN` 行為。

目的：

> 在不改變 triangle geometry、valid point set、output ordering、interface、protocol、reset、column storage、tracer architecture 的前提下，對安全的 `x2 > x1` 情況加入 row-level early exit，以減少無效 scan candidate cycles。

本次修改不是重新設計 RTL。

---

# 2. ALLOWED CHANGE

只允許修改：

```text
OUTPUT_SCAN
```

相關的：

* row scanning control
* `x_scan` update
* `y_scan` update
* row termination condition
* scan-to-next-row transition

必要時可以增加**只服務於 OUTPUT_SCAN 的 control logic/register**。

---

# 3. REQUIRED NEW BEHAVIOR

## Case A — `x2 > x1`

此時：

```text
x_left  = x1
x_right = x2
```

`x_left` 對所有：

```text
y1 <= y <= y3
```

皆為 valid column。

因此：

```text
OUTPUT_SCAN

candidate = (x_scan, y_scan)

if inside:
    po = 1
    x_scan = x_scan + 1

else:
    po = 0
    current row terminates
    y_scan = y_scan + 1
    x_scan = x_left
```

也就是：

```text
valid → valid → valid → 0
                         |
                         +--> next y
```

**第一次 trailing `po=0` 即可結束目前 row。**

---

# 4. Case B — `x2 < x1`

本次修改：

> **不得套用上述 early-exit rule。**

保持原本 A1 行為：

```text
x_scan = x_left → x_right
```

即使：

```text
po = 0
```

也必須繼續：

```text
x_scan++
```

直到：

```text
x_scan == x_right
```

才進入下一個 y。

原因：

```text
x_left = x2
```

而：

```text
ylow[x2] = yup[x2] = y2
```

所以在：

```text
y != y2
```

時，第一個 candidate 可能就是：

```text
po = 0
```

但後方仍可能存在 valid points。

**禁止因第一個 `po=0` 而提前結束 row。**

---

# 5. Direction Decision

Engineer 可以使用既有：

```text
dx
```

或等價的 direction information。

判斷：

```text
x2 > x1
```

時：

```text
early_exit_enable = 1
```

判斷：

```text
x2 < x1
```

時：

```text
early_exit_enable = 0
```

不得重新建立 geometry calculation。

---

# 6. `|dx| = 1`

`|dx| = 1` 的既有 shortcut 必須保留。

不得因本次 optimization：

* 移除 shortcut
* 修改 shortcut
* 將 shortcut 改成新的 algorithm
* 改變其 output behavior

本次只是在既有 `OUTPUT_SCAN` 上使用 direction-aware early exit。

---

# 7. `ylow / yup` 禁止修改

本次 RTL change：

**不得修改：**

```text
ylow[0:7]
yup[0:7]
```

的：

* storage structure
* write behavior
* initialization
* semantics
* address mapping

`OUTPUT_SCAN` 只能讀取既有 column information。

---

# 8. Geometry / Edge Algorithm 禁止修改

不得修改：

```text
LOWER edge
UPPER edge
E_L
E_U
sE >= 0
Y2 Clamp
```

也不得修改：

```text
x_trace
y_trace
E_trace
trace_mode
```

本次 change 與 tracer algorithm 無關。

---

# 9. FSM Restriction

禁止新增：

```text
LOWER
UPPER
TRACE
CLAMP
```

相關 state。

如有需要，只能修改：

```text
OUTPUT_SCAN
```

內部 control transition。

不得重新設計整個 FSM。

---

# 10. `row_started` Restriction

**不得為了本次 change 自動加入 `row_started`。**

原因：

本次採用 direction-aware rule：

```text
x2 > x1:
    first po=0 → row end

x2 < x1:
    first po=0 → continue scan
```

因此不需要透過：

```text
row_started
```

來區分 leading/trailing zero。

除非 Engineer 發現現有 RTL 結構確實需要等價 control signal 才能實現上述明確行為；若需要，必須先回報，不得自行改變 architecture intent。

---

# 11. Output Register Timing

必須維持既有：

```text
po
xo
yo
```

全部為 registered outputs。

禁止：

* combinational `po`
* combinational output bypass
* output timing shift
* output pulse shortening

Early-exit 發生的 cycle：

```text
po = 0
```

該 cycle 仍為正常 registered output cycle。

下一個 y 的第一個 candidate：

> 必須在下一個正常 scan cycle 被檢查。

不得在同一 clock edge 同時：

```text
finish current row
+
produce next-row output
```

---

# 12. Output Ordering

必須保持：

```text
y ascending
```

以及同一 y：

```text
x ascending
```

不得 reverse scan direction。

不得修改：

```text
x_left
x_right
```

的定義。

---

# 13. RELEASE

**不得修改 RELEASE。**

既有：

```text
OUTPUT_SCAN
    ↓
RELEASE
    ↓
IDLE
```

必須保持。

禁止：

```text
delete RELEASE
```

或：

```text
OUTPUT_SCAN → IDLE
```

除非另外建立獨立 Architecture Change：

```text
ARCH-CHANGE-RELEASE-REMOVAL
```

並取得 approval。

---

# 14. Interface Restriction

以下 interface 完全禁止修改：

```text
clk
reset
nt
xi
yi
busy
po
xo
yo
```

不得：

* 增加 port
* 刪除 port
* 修改 port width
* 修改 active level
* 修改 protocol

---

# 15. Reset Restriction

Reset architecture 不得修改。

維持：

```text
active-high asynchronous reset
```

新增的任何 OUTPUT_SCAN control register，如果真的需要，必須具有與既有 architecture 一致的 reset behavior。

---

# 16. Latency Objective

本次 optimization 的 expected benefit：

### A1

```text
N_scan =
(|x2-x1| + 1) × (y3-y1+1)
```

### New behavior

僅：

```text
x2 > x1
```

允許 trailing-zero early exit。

已知完整合法 domain analysis：

```text
A1:
68,992 candidates

Direction-aware early-exit:
59,306 candidates

Reduction:
9,686 candidates
≈14.04%
```

這是 candidate-level scan reduction。

不得聲稱 Big-O complexity 改善。

---

# 17. Functional Correctness Requirement

以下 properties 必須保持：

```text
P1  No valid point may be lost.

P2  No invalid point may become po=1.

P3  Output ordering remains y ascending.

P4  Same-y ordering remains x ascending.

P5  Boundary points remain included.

P6  P1/P2/P3 remain included.

P7  Horizontal y2 row remains unchanged.

P8  x2>x1 and x2<x1 both remain correct.

P9  |dx|=1 shortcut remains correct.

P10 Busy/input protocol remains unchanged.
```

---

# 18. Mandatory Verification

修改完成後不得只測單一 testcase。

至少執行：

### Directed

```text
(1,1),(6,3),(1,6)

reverse-x direction

|dx|=1

x=0 boundary
x=7 boundary

y=0 boundary
y=7 boundary

minimum vertical extent
maximum vertical extent
```

---

# 19. Mandatory Exhaustive Regression

完整合法 domain：

```text
x1 = 0..7
y1 = 0..7
y2 = y1+1..7
y3 = y2+1..7
x2 = 0..7
x2 != x1
x3 = x1
```

共：

```text
3136 triangles
```

Required:

```text
3136 / 3136 PASS
```

---

# 20. Required Regression Against Baseline

修改後必須重新確認：

```text
Transactions:
3146 / 3146 PASS

Points:
35641 / 35641 PASS

Busy errors:
0

Output errors:
0

Protocol errors:
0

Timing errors:
0

Reset errors:
0
```

---

# 21. Final Testbench

Final Testbench 不得修改。

必須使用原本 frozen Final TB。

需要比較：

```text
Baseline RTL 1.0.2:
146000 ns

New RTL:
<measured result>
```

並報告：

```text
latency reduction
```

但：

> Final TB 的 expected behavior 不得因 RTL 修改而被改寫。

---

# 22. Reference Model Restriction

Expected output 必須繼續來自獨立 geometric reference model。

不得：

```text
copy RTL scanner logic
```

來產生 expected result。

不得修改 reference model 使其配合 RTL。

---

# 23. Forbidden Changes

本次 Engineer **禁止修改**：

```text
[ ] Interface
[ ] Input protocol
[ ] Output protocol
[ ] Clock
[ ] Reset
[ ] Column storage
[ ] ylow/yup semantics
[ ] LOWER algorithm
[ ] UPPER algorithm
[ ] Edge equations
[ ] E width
[ ] Signedness
[ ] Y2 Clamp
[ ] Shared tracer
[ ] |dx|=1 shortcut
[ ] RELEASE
[ ] Final Testbench
[ ] Expected output data
```

---

# 24. Implementation Boundary

本次修改的最大允許範圍：

```text
                ┌──────────────────┐
                │   OUTPUT_SCAN    │
                │                  │
                │ row control      │
                │ x_scan update    │
                │ y_scan update    │
                │ row termination  │
                └──────────────────┘
                         │
                         │ read only
                         ▼
                  ┌──────────────┐
                  │ ylow / yup   │
                  └──────────────┘
```

不得向 upstream tracer architecture 擴散。

---

# 25. Required Engineer Report

Engineer 完成 RTL 修改後，必須提供：

```text
1. Modified RTL version
2. Exact modified state/control logic
3. Files modified
4. Lines/functions modified
5. 3136-triangle regression result
6. Final TB result
7. Baseline latency
8. New latency
9. Latency reduction
10. Any unexpected behavior
```

如果發現需要修改上述 Forbidden Changes 中任何項目：

> **STOP implementation and report back to Architect.**

不得自行擴大 scope。

---

# 26. Registered-output timing clarification:

po is a registered output and therefore represents the validity of the previously registered candidate. Engineer must determine whether the current (x_scan, y_scan) candidate is inside the triangle using the existing ylow/yup information, and use that current-candidate result for row termination.
The implementation must not interpret the previous-cycle registered po as the current candidate's validity.
po/xo/yo timing and the one-cycle separation between row termination and the next-row candidate must remain unchanged.

---

# 27. Change-Control Status

目前：

```text
Architecture A1:
    PENDING

A2:
    NOT FROZEN

RTL 1.0.2:
    VERIFIED BASELINE

Current task:
    LIMITED RTL IMPLEMENTATION STUDY
```

本 instruction **不代表 Architecture A2 已 Freeze**。

Engineer 的工作只是：

> 在上述明確限制內實作並驗證 OUTPUT_SCAN direction-aware early exit。

若 implementation 與上述規則衝突：

**STOP — DO NOT MODIFY ARCHITECTURE.**
