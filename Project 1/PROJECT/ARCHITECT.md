# Architecture Document

## 1. Executive Summary

### 1.1 Project

**Triangle Rendering Engine**

本模組接收三角形三個頂點座標，依照指定的幾何規則，逐 cycle 輸出三角形內部與邊界上的所有整數座標點。

輸出順序必須符合：

> 先由左而右，再由下而上。

本 Architecture Document 的目的，是將目前已形成的 system-level architecture decisions 系統化、明確化並建立可追溯的 implementation baseline。

本文件不是重新發明 architecture，而是將目前已確認的：

* input protocol
* output protocol
* timing behavior
* coordinate domain
* triangle validity assumptions
* geometric algorithm
* Candidate C architecture
* FSM behavior
* datapath behavior
* verification implications

正式固化。

---

### 1.2 Current Architecture Status

**Architecture Status: `UNDER REVIEW`**

目前採用：

> **Candidate C — Bounded Coordinate Scan**

作為 RTL implementation 與 verification 的 working baseline。

Candidate B：

> **Incremental Boundary**

保留為後續 optimization candidate，但目前不作為 RTL baseline。

目前尚未有：

* RTL simulation result
* Gate-level simulation result
* Design Compiler area report
* Timing report
* Physical implementation result

因此所有 implementation / timing / area / power 結論均屬 architecture-level reasoning，尚未驗證。

---

### 1.3 Architecture Principle

本架構遵循以下優先順序：

1. Correctness
2. Traceability
3. Verifiability
4. Implementability
5. Physical Feasibility
6. Performance Optimization
7. Code Elegance

---

# 2. Requirements Interpretation

## 2.1 Explicit Requirements

以下為本 architecture 已確認之 explicit requirements。

### Interface

Top-level module 必須為：

```text
module triangle (clk, reset, nt, xi, yi, busy, po, xo, yo);
```

Interface：

| Signal  | Direction | Width | Description                     |
| ------- | --------- | ----: | ------------------------------- |
| `clk`   | input     |     1 | Positive-edge triggered clock   |
| `reset` | input     |     1 | Active-high asynchronous reset  |
| `nt`    | input     |     1 | New triangle indication         |
| `xi`    | input     |     3 | Input X coordinate              |
| `yi`    | input     |     3 | Input Y coordinate              |
| `busy`  | output    |     1 | Triangle engine busy indication |
| `po`    | output    |     1 | Output coordinate valid         |
| `xo`    | output    |     3 | Output X coordinate             |
| `yo`    | output    |     3 | Output Y coordinate             |

---

## 2.2 Coordinate Domain

Coordinate domain 正式定義為：

```text
0 ~ 7
```

因此：

```text
0 <= x <= 7
0 <= y <= 7
```

所有合法 triangle coordinates 均位於此 domain。

---

## 2.3 Triangle Geometry

Triangle vertices 為：

```text
P1 = (x1, y1)
P2 = (x2, y2)
P3 = (x3, y3)
```

且 testbench 保證 triangle 合法：

```text
x1 = x3
y1 < y2 < y3
```

因此：

* `P1 → P3` 為垂直邊
* `x1 = x3`
* `y1 < y2 < y3`

`x1 = x2 = x3` 為非法 triangle，不會出現在 testbench。

---

# 3. Behavioral Model

## 3.1 Input Transaction

一個 triangle transaction 由三組 consecutive input coordinates 組成。

當：

```text
nt = 1
```

表示新的 triangle transaction 開始。

Timing：

```text
Cycle N:
    nt = 1
    xi, yi = (x1, y1)

Cycle N+1:
    nt = 0
    xi, yi = (x2, y2)

Cycle N+2:
    nt = 0
    xi, yi = (x3, y3)
```

---

## 3.2 Busy Timing

`busy` 必須在輸入第三個 vertex 前已經為 High。

因此：

```text
Cycle N:
    busy = 0

Cycle N+1:
    busy = 1

Cycle N+2:
    busy = 1
```

換句話說，於 `N+1` clock edge 完成第二個 vertex capture 時，`busy` 被拉高。

因此在 `N+2` clock edge capture `x3/y3` 時，系統已經處於 busy 狀態。

---

## 3.3 Output Behavior

Triangle 完成 input capture 後，開始掃描 triangle 所在的 coordinate region。

輸出順序：

```text
y = y1 → y3
```

對每一個 y：

```text
x = min(x1,x2) → max(x1,x2)
```

依序進行 inside test。

若座標點位於 triangle 內部或邊界：

```text
po = 1
xo = x
yo = y
```

若座標點不在 triangle：

```text
po = 0
```

`po` 可以連續為 High。

例如：

```text
po = 1, xo = 1, yo = 2
po = 1, xo = 2, yo = 2
po = 1, xo = 3, yo = 2
```

不需要在兩個有效輸出點之間插入 bubble cycle。

---

## 3.4 Output Ordering

Output ordering 定義為 row-major order：

```text
y = y1
    x increasing

y = y1 + 1
    x increasing

...

y = y3
    x increasing
```

因此符合：

> 先由左而右，再由下而上。

此 ordering 由 sample output 與 specification behavior 一致推導。

---

# 4. Interface Architecture

## 4.1 Input Protocol

只有：

```text
busy = 0
```

時，`nt` 才有效。

Testbench 保證：

```text
busy = 1
```

期間不會送入新的 `nt`。

因此 architecture 不需要額外處理 busy 狀態下的新 transaction。

---

## 4.2 Vertex Capture

Input vertex registers：

```text
x1_reg, y1_reg
x2_reg, y2_reg
x3_reg, y3_reg
```

分別於三個 consecutive input cycles capture。

Protocol：

```text
N:
    capture P1

N+1:
    capture P2
    busy -> 1

N+2:
    capture P3
```

第三個 vertex capture 完成後，triangle parameters 已完整存在於 internal registers。

---

## 4.3 Output Interface

`po` 是 output coordinate validity indication。

當：

```text
po = 1
```

則：

```text
(xo, yo)
```

為本 cycle 有效輸出座標。

當：

```text
po = 0
```

`xo` 與 `yo` 為 Don't Care。

因此不需要額外加入：

```text
MUX → 0
```

來強制清除 `xo` / `yo`。

`xo` / `yo` 可以：

* 保持上一個值，或
* 保持 datapath 當下的值。

兩者皆不影響 protocol correctness。

---

# 5. Cycle-Level Behavior

## 5.1 Input Phase

```text
          N          N+1          N+2
          │           │             │
nt        1           0             0
          │           │             │
input    P1          P2            P3
          │           │             │
busy      0           1             1
```

其中 `busy` 於 `N+1` edge 後變為 `1`。

---

## 5.2 Processing Phase

Triangle parameters capture 完成後，進入 coordinate scan。

概念流程：

```text
y = y1

while y <= y3:

    x = min(x1,x2)

    while x <= max(x1,x2):

        evaluate Inside(x,y)

        if Inside:
            output (x,y)

        x = x + 1

    y = y + 1
```

實際 RTL 不需要使用 software-style loop；以上僅為 behavioral model。

硬體將使用 FSM 與 counters 實現。

---

## 5.3 Final Output and IDLE

目前採用簡化版本：

> 最後一個 output 完成後，直接回到 `IDLE`。

目前不額外建立 `RELEASE` state。

因此 architecture working behavior 為：

```text
OUTPUT
   │
   │ final output
   ▼
IDLE
```

若後續 verification 發現此寫法無法滿足實際 testbench 對 `busy` release timing 的要求，再增加 `RELEASE` state。

目前不預先增加此 state。

---

# 6. Control Architecture

## 6.1 FSM

目前 working FSM：

```text
IDLE
  │
  │ nt = 1
  ▼
CAPTURE_P2
  │
  │ capture P2
  │ busy = 1
  ▼
CAPTURE_P3
  │
  │ capture P3
  ▼
SCAN
  │
  │ x/y scan
  ▼
IDLE
```

---

## 6.2 IDLE

功能：

* 等待新的 `nt`
* `busy = 0`
* `po = 0`

當：

```text
nt = 1
```

於 clock edge capture：

```text
x1 = xi
y1 = yi
```

並進入下一個 input capture state。

---

## 6.3 CAPTURE_P2

此 state：

* capture `x2`
* capture `y2`
* 將 `busy` 拉高
* 準備接收第三個 vertex

因此此 clock edge 後：

```text
busy = 1
```

---

## 6.4 CAPTURE_P3

此 state：

* capture `x3`
* capture `y3`
* 完成 triangle input transaction

之後進入 coordinate scan phase。

---

## 6.5 SCAN

SCAN state 負責：

1. 產生目前 `(x,y)`
2. 計算 inside condition
3. 決定 `po`
4. 若有效，輸出 `(x,y)`
5. 更新 x counter
6. 完成一個 row 後更新 y counter
7. 完成最後一個 row 後回到 `IDLE`

---

# 7. Datapath Architecture

## 7.1 Required Registers

Architecture-level registers：

### Vertex registers

```text
x1
y1
x2
y2
x3
y3
```

### Scan registers

```text
x_counter
y_counter
```

---

## 7.2 Scan Range

由 triangle geometry：

```text
x1 = x3
y1 < y2 < y3
```

可知 triangle 的 horizontal extent 位於：

```text
min(x1,x2) ~ max(x1,x2)
```

因此目前 Candidate C 採用：

```text
x = min(x1,x2) → max(x1,x2)
```

而不是掃描完整：

```text
0 → 7
```

Y 方向則：

```text
y = y1 → y3
```

---

## 7.3 X Direction

由：

```text
x = min(x1,x2) → max(x1,x2)
```

可確保：

* triangle 朝右時，`x1 → x2`
* triangle 朝左時，`x2 → x1`

兩種 orientation 均可使用相同 scan architecture。

---

# 8. Algorithm / Mathematical Model

## 8.1 Edge Function

對任意 directed edge：

```text
PA = (xA,yA)
PB = (xB,yB)
```

定義：

$$
E_{A,B}(x,y)
=
(x-x_A)(y_B-y_A)
-
(x_B-x_A)(y-y_A)
$$

此式由 specification 原始 ratio relation 交叉相乘而得，因此不需要 hardware division。

---

## 8.2 Edge 12

$$
E_{12}
=
(x-x_1)(y_2-y_1)
-
(x_2-x_1)(y-y_1)
$$

---

## 8.3 Edge 23

$$
E_{23}
=
(x-x_2)(y_3-y_2)
-
(x_3-x_2)(y-y_2)
$$

由：

$$
x_3=x_1
$$

可進一步利用：

$$
x_3-x_2=x_1-x_2
$$

---

## 8.4 Edge 31

General form：

$$
E_{31}
=
(x-x_3)(y_1-y_3)
-
(x_1-x_3)(y-y_3)
$$

由：

$$
x_1=x_3
$$

得到：

$$
E_{31}
=
(x-x_1)(y_1-y_3)
$$

---

## 8.5 E31 Implementation Decision

雖然 `E31` 已建立於 mathematical model，但目前：

> **E31 不實作於 Candidate C datapath。**

原因是：

1. scan range 已經限制在 `min(x1,x2) ~ max(x1,x2)`
2. 因此 vertical edge 的 horizontal constraint 已由 scan range 隱含處理
3. 不需要額外建立 E31 multiplier
4. 可降低 datapath complexity

因此 Candidate C 的 inside test 主要由：

```text
E12
E23
```

以及既有 x scan boundary 完成。

---

# 9. Inside Condition

## 9.1 Triangle Facing Right

當：

$$
x_1 < x_2
$$

triangle 位於 vertical edge：

$$
x=x_1
$$

的右側。

因此：

$$
x \ge x_1
$$

且：

$$
E_{12}\le0
$$

$$
E_{23}\le0
$$

所以：

$$
Inside =
(E_{12}\le0)
\land
(E_{23}\le0)
$$

其中 x scan range 已保證：

```text
x1 <= x <= x2
```

因此不需要額外實作 E31。

---

## 9.2 Triangle Facing Left

當：

$$
x_1 > x_2
$$

triangle 位於：

$$
x=x_1
$$

的左側。

因此：

$$
x \le x_1
$$

且：

$$
E_{12}\ge0
$$

$$
E_{23}\ge0
$$

所以：

$$
Inside =
(E_{12}\ge0)
\land
(E_{23}\ge0)
$$

x scan range 此時為：

```text
x2 <= x <= x1
```

因此同樣不需要 E31。

---

## 9.3 Boundary Inclusion

所有：

```text
E12 = 0
```

或：

```text
E23 = 0
```

的 boundary points 都必須被包含。

因此 inside comparison 必須使用：

```text
<=
```

或：

```text
>=
```

而不能使用 strict inequality。

三角形頂點也因此自然包含於 output。

---

# 10. Bit-Width and Signedness Analysis

## 10.1 Coordinate Width

Coordinate：

```text
x1,x2,x3,y1,y2,y3
```

均為 3-bit unsigned：

```text
0 ~ 7
```

---

## 10.2 Coordinate Difference

任兩個 coordinate difference：

$$
a-b
$$

範圍：

$$
-7 \sim +7
$$

因此 arithmetic implementation 必須視為 signed quantity。

---

## 10.3 Multiplication Range

例如：

$$
(x-x_A)(y_B-y_A)
$$

兩個 operand 的 magnitude 最大為：

$$
7
$$

因此最大 magnitude：

$$
7\times7=49
$$

所以 edge-function product 必須能表示：

$$
-49\sim+49
$$

---

## 10.4 Signed Representation

Architecture implementation 必須避免 Verilog expression 中：

* unsigned/signed 混用
* implicit width extension
* implicit truncation
* comparison signedness mismatch

因此 E12/E23 datapath 的 signedness 必須在 RTL 中明確指定。

此項屬於 RTL implementation constraint，不得由 RTL engineer 任意改成不同 arithmetic interpretation。

---

# 11. Candidate Architectures

## 11.1 Candidate B — Incremental Boundary

Candidate B 使用 triangle boundary 的 incremental calculation。

概念上：

```text
row
 ↓
calculate right boundary
 ↓
output x range
 ↓
update boundary
 ↓
next row
```

優點：

* 每個 row 不需要逐一測試所有 x
* cycle count 理論上較低
* 可避免完整 coordinate scan
* 有後續 performance optimization 空間

缺點：

* boundary calculation 較複雜
* quotient / remainder handling 需要額外 architecture
* rounding / floor behavior 必須精確控制
* variable `dy` 增加 datapath complexity
* verification complexity 高於 Candidate C

Candidate B 目前不作為 RTL baseline。

---

## 11.2 Candidate C — Bounded Coordinate Scan

Candidate C 掃描：

```text
y1 → y3
```

以及：

```text
min(x1,x2) → max(x1,x2)
```

每個 candidate coordinate 執行：

```text
E12
E23
Inside
```

若 inside：

```text
po = 1
xo = x
yo = y
```

優點：

* architecture straightforward
* cycle-level behavior 容易定義
* mathematical model 與 RTL datapath 直接對應
* 不需要 division
* 不需要 E31
* verification reference model 簡單
* coordinate domain 僅 0~7，因此 scan space 有明確上限

缺點：

* cycle count 高於 boundary-based architecture
* 每個 candidate point 都需要 arithmetic evaluation
* Time Complexity 可能成為主要 performance cost

---

# 12. Architecture Trade-Off

目前尚無實際：

* `ncverilog.log`
* Design Compiler area report
* synthesis timing report

因此不能宣稱 Candidate B 或 Candidate C 在實際 project metric 下較佳。

目前比較：

| 項目                      | Candidate B | Candidate C |
| ----------------------- | ----------- | ----------- |
| Control complexity      | 較高          | 較低          |
| Arithmetic complexity   | 較高          | 中等          |
| Division dependency     | 可能需要處理      | 不需要         |
| Cycle efficiency        | 理論上較高       | 理論上較低       |
| Verification complexity | 較高          | 較低          |
| Traceability            | 中等          | 高           |
| RTL implementation risk | 較高          | 較低          |
| Optimization potential  | 高           | 中等          |
| Current baseline        | No          | **Yes**     |

目前 Candidate C 被選為 working baseline，不代表已證明其 `Time Complexity × Area Complexity` 最佳。

---

# 13. Recommended / Working Architecture

## 13.1 Working Baseline

目前採用：

> **Candidate C — Bounded Coordinate Scan**

作為第一版 RTL architecture。

---

## 13.2 Scan Algorithm

```text
initialize:

x_counter = min(x1,x2)
y_counter = y1

while y_counter <= y3:

    while x_counter <= max(x1,x2):

        calculate E12
        calculate E23

        determine Inside

        if Inside:
            po = 1
            xo = x_counter
            yo = y_counter

        advance x_counter

    x_counter = min(x1,x2)
    advance y_counter
```

上述為 behavioral representation。

實際 RTL 必須使用 synthesizable sequential/combinational logic 實現。

---

## 13.3 No E31

Candidate C 不實作 E31。

Vertical edge constraint 由：

```text
x = min(x1,x2) → max(x1,x2)
```

的 scan range 隱含處理。

---

# 14. Timing Considerations

## 14.1 Clock

Clock 為：

```text
clk
```

positive-edge triggered。

---

## 14.2 Reset

Reset：

```text
reset = 1
```

時為 asynchronous reset。

---

## 14.3 Reset Output Requirements

Reset 期間以及 reset release 後的 idle condition：

```text
busy = 0
po   = 0
```

因此 system 可以準備接收下一個 `nt`。

---

## 14.4 xo / yo During Reset

`xo` 與 `yo` 在 reset 期間以及第一個有效 output 之前為：

> **Don't Care**

因此不需要額外 reset mux 或 zero forcing logic。

---

## 14.5 Timing Constraint Status

目前 specification 僅確認：

```text
Setup/Hold > 0.5 ns
```

實際：

* T_cycle
* technology node
* standard-cell library
* clock constraint
* SDC
* operating corner

目前未提供。

因此：

> Timing feasibility = **NOT VERIFIED**

不能在 architecture 階段宣稱 timing closure。

---

# 15. Area Considerations

Candidate C 的主要 datapath resource 為：

* coordinate registers
* x/y counters
* E12 arithmetic
* E23 arithmetic
* comparators
* control FSM

因 coordinate domain 僅 0~7，arithmetic width 有限。

但實際 area 必須由 Design Compiler synthesis report 驗證。

因此：

> Area Complexity = **NOT VERIFIED**

Candidate B 是否能以較少 area / cycle 達到更好的 project metric，目前不能由 architecture reasoning 單獨確認。

---

# 16. Power Considerations

Candidate C 在 scan phase 會持續進行 arithmetic evaluation，即使某些 candidate points 最終不 output。

因此可能存在：

* arithmetic switching
* counter switching
* comparator switching

等 dynamic power activity。

目前 project specification 沒有提供：

* power target
* switching activity
* clock gating requirement
* power budget

因此 power impact 目前僅屬 architecture consideration。

> Power result = **NOT VERIFIED**

目前不增加 clock gating 或 power optimization architecture。

---

# 17. Physical Implementation Considerations

Candidate C datapath 的主要 physical considerations：

* arithmetic combinational depth
* comparator fanout
* counter-to-datapath routing
* control-to-output routing
* clock/reset distribution
* register placement

由於 coordinate arithmetic width 很小，目前沒有證據顯示需要額外 pipeline。

但是：

> 是否存在 timing / congestion / area problem，必須透過 synthesis / P&R evidence 判斷。

不得僅依 RTL code 外觀宣稱 physical feasibility。

---

# 18. Verification Implications

## 18.1 Golden Behavioral Model

Reference model 可使用：

```text
for y = y1 to y3:
    for x = min(x1,x2) to max(x1,x2):
        calculate geometric inside condition
        if inside:
            expected output = (x,y)
```

---

## 18.2 Output Ordering Check

Verification 必須確認：

1. `y` 不會下降
2. 同一 row 中 `x` 僅會遞增
3. row transition 後才進入下一個 y
4. 不會漏掉合法 point
5. 不會輸出 triangle 外的 point
6. boundary points 必須包含
7. 三個 vertices 必須包含

---

## 18.3 Protocol Checks

需要確認：

### Input

```text
nt = 1
```

只會出現在：

```text
busy = 0
```

### Busy

`busy` 必須在 N+1 edge 後為 High。

### Output

當：

```text
po = 1
```

則：

```text
xo,yo
```

必須為有效 triangle coordinate。

### Reset

Reset 後：

```text
busy = 0
po = 0
```

---

## 18.4 Continuous PO

Verification 不應假設：

```text
po = 1
```

之間一定存在：

```text
po = 0
```

因為 architecture 明確允許：

```text
po = 1
```

連續多 cycle。

---

# 19. Corner Cases

## 19.1 Triangle Facing Right

```text
x1 < x2
```

Inside：

```text
E12 <= 0
E23 <= 0
```

---

## 19.2 Triangle Facing Left

```text
x1 > x2
```

Inside：

```text
E12 >= 0
E23 >= 0
```

---

## 19.3 Vertical Edge

```text
x1 = x3
```

此為 specification guarantee。

---

## 19.4 Lowest Row

```text
y = y1
```

必須包含：

```text
(x1,y1)
```

以及符合 geometry 的 boundary points。

---

## 19.5 Middle Vertex Row

```text
y = y2
```

兩條斜邊於：

```text
(x2,y2)
```

相交。

此 row 必須正確包含該 vertex，且不能因 lower/upper edge transition 重複輸出。

---

## 19.6 Highest Row

```text
y = y3
```

必須包含：

```text
(x3,y3)
```

且：

```text
x3 = x1
```

---

## 19.7 Coordinate Boundary

合法 domain：

```text
0 ~ 7
```

因此必須能正確處理：

```text
x = 0
x = 7
y = 0
y = 7
```

---

## 19.8 Invalid Triangle

以下情況由 testbench 排除：

```text
x1 = x2 = x3
```

以及不符合：

```text
x1 = x3
y1 < y2 < y3
```

的 triangle。

因此 architecture 不加入 invalid-input error handling。

---

# 20. Open Issues / Deferred Decisions

目前原先提出的 Open Issues 已大部分完成決策，不再視為 blocking architecture issues。

## 20.1 Candidate B Evaluation

Candidate B 保留作為 optimization candidate。

是否需要進一步實作 Candidate B，待 Candidate C 完成：

* RTL simulation
* synthesis
* timing analysis
* area analysis

後再依實際 project metric 判斷。

---

## 20.2 Actual Timing Closure

仍需要實際：

* technology
* standard-cell library
* clock constraint
* SDC
* synthesis
* STA

才能確認 timing。

目前：

```text
Timing = NOT VERIFIED
```

---

## 20.3 Actual Area Complexity

需要 Design Compiler report。

目前：

```text
Area = NOT VERIFIED
```

---

## 20.4 Final Architecture Freeze

目前：

```text
UNDER REVIEW
```

尚未 FROZEN。

只有經 human/project owner 明確 review / approval 後，才能將 architecture status 改為：

```text
FROZEN
```

---

# 21. Architecture Decisions Summary

目前正式採用的 architecture decisions：

1. Coordinate domain 採 `0~7`。
2. Triangle input 保證合法。
3. `x1 = x3`。
4. `y1 < y2 < y3`。
5. `x1 = x2 = x3` 不會出現在 test。
6. `busy` 於 N+1 clock edge 後拉高。
7. `po` 可以連續 High。
8. `po=0` 時 `xo/yo` 為 Don't Care。
9. Reset 時 `busy=0`。
10. Reset 時 `po=0`。
11. Candidate C 作為目前 working baseline。
12. Candidate B 保留作為 optimization candidate。
13. Candidate C 採 bounded coordinate scan。
14. Y scan 為 `y1 → y3`。
15. X scan 為 `min(x1,x2) → max(x1,x2)`。
16. Inside 判斷使用 cross-product edge function。
17. 不使用 division。
18. Candidate C 不實作 E31。
19. E12 / E23 為主要 geometric inside calculation。
20. Boundary points 使用 inclusive comparison。
21. `po` 為有效輸出 indicator。
22. 最後一個 output 後目前直接回 `IDLE`。
23. 目前不加入 RELEASE state。
24. 若 verification 顯示 busy release timing 不符合 testbench，再重新評估 RELEASE state。

---

# 22. Traceability

Architecture decisions 可追溯至以下來源：

| Architecture Item       | Source                                                  |
| ----------------------- | ------------------------------------------------------- |
| Top-level interface     | Project Specification                                   |
| Input timing            | Project Specification                                   |
| Busy timing             | Project Specification + confirmed architecture decision |
| Output ordering         | Project Specification + sample behavior                 |
| Coordinate domain `0~7` | Confirmed architecture decision                         |
| Triangle validity       | Testbench assumption / confirmed decision               |
| Edge Function           | Derived from Project Specification                      |
| E31 elimination         | Geometry constraint `x1=x3` + architecture decision     |
| Candidate C             | Architecture decision                                   |
| X scan range            | Architecture decision                                   |
| Continuous `po`         | Architecture decision                                   |
| `xo/yo` Don't Care      | Interface/reset timing specification interpretation     |
| Direct return to IDLE   | Current working architecture decision                   |
| Candidate B retention   | Architecture trade-off decision                         |

---

# 23. Handoff to RTL

RTL implementation 必須遵守本文件目前已確認的 architecture decisions。

RTL engineer 不應自行修改：

* input protocol
* busy timing
* output ordering
* coordinate domain
* scan range
* inside definition
* E31 implementation decision
* reset behavior
* `po` semantics
* final output to IDLE behavior

若 RTL implementation 發現 architecture 無法直接實現，必須先回報：

1. Root Cause
2. Affected Architecture Decision
3. Proposed Change
4. Functional Impact
5. Timing Impact
6. Area Impact
7. Verification Impact

未經 architecture review / human approval，不得自行改變 architecture contract。

---

# 24. Verification Status

目前：

```text
RTL Simulation       : NOT VERIFIED
Gate-Level Simulation: NOT VERIFIED
Synthesis            : NOT VERIFIED
STA                  : NOT VERIFIED
P&R                  : NOT VERIFIED
DRC                  : NOT VERIFIED
LVS                  : NOT VERIFIED
```

本 Architecture Document 僅代表目前 architecture definition，並不代表 implementation correctness 或 physical closure。

---

# 25. Architecture Status

```text
Architecture Status: UNDER REVIEW
Working Baseline   : Candidate C
Optimization Path  : Candidate B
```

本文件目前作為第一個正式 Architecture artifact。

在 human/project owner review 並明確批准前：

> **不得標記為 FROZEN。**

Architecture freeze 後，任何對：

* interface
* protocol
* cycle behavior
* datapath
* algorithm
* width
* signedness
* reset
* latency
* throughput

的修改，都必須依 change-control 流程處理。

---

# 26. Final Architecture Summary

目前 Triangle Rendering Engine 的 architecture 可濃縮為：

```text
                  ┌─────────────────────┐
nt, xi, yi ──────►   Input Capture      │
                  │ P1 → P2 → P3        │
                  └──────────┬──────────┘
                             │
                             │
                         busy = 1
                             │
                             ▼
                  ┌─────────────────────┐
                  │  Coordinate Scanner │
                  │                     │
                  │ y : y1 → y3         │
                  │ x : min → max       │
                  └──────────┬──────────┘
                             │
                             ▼
                  ┌─────────────────────┐
                  │   E12 / E23         │
                  │   Inside Test       │
                  └──────────┬──────────┘
                             │
                     ┌───────┴───────┐
                     │               │
                   Inside          Outside
                     │               │
                     ▼               │
                po = 1               │
                xo = x               │
                yo = y               │
                     │               │
                     └───────┬───────┘
                             │
                             ▼
                      Next x / Next y
                             │
                             ▼
                           IDLE
```

因此，目前正式 Architecture baseline 為：

> **以 Candidate C 為基礎，使用 bounded coordinate scan，對 `y1→y3` 與 `min(x1,x2)→max(x1,x2)` 的候選座標進行 E12/E23 cross-product inside test，輸出所有包含於 triangle 內或邊界上的整數座標。**

目前 architecture **UNDER REVIEW，尚未 FROZEN，亦尚未經 EDA verification。**
