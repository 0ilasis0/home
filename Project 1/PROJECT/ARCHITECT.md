# Architecture Document

## 1. Executive Summary

### 1.1 Project

**Triangle Rendering Engine**

本模組接收三角形三個頂點座標，依照 Project Specification 定義的幾何規則，逐 cycle 輸出三角形內部與邊界上的所有整數座標點。

輸出順序為：

> 先由左而右，再由下而上。

本文件是本專案第 2 個正式 Architecture artifact。

本文件的目的不是重新發明 Architecture，而是將前一版已形成並經 review 修正後的 architecture decisions：

* input protocol
* `busy` timing
* output protocol
* coordinate domain
* triangle validity
* bounded coordinate scan
* E12/E23 edge-function algorithm
* E31 elimination
* register requirements
* signed arithmetic requirements
* cycle-level behavior
* Candidate B / Candidate C trade-off

正式、可追溯地固化。

---

### 1.2 Architecture Status

```text
Architecture Status : UNDER REVIEW
Working Baseline    : Candidate C
Optimization Candidate: Candidate B
```

目前尚未完成：

* RTL simulation
* Gate-level simulation
* Synthesis
* STA
* P&R
* DRC
* LVS

因此本文件中的 implementation feasibility、area、timing、power 結論均尚未經 EDA tool 驗證。

---

# 2. Source of Truth and Decision Classification

本 Architecture 依以下優先順序解讀：

1. Project Specification
2. 已確認的 Interface / Timing decisions
3. 本 Architecture Document
4. Verification requirements
5. RTL implementation

Architecture 中的內容分為：

### Explicit Requirement

直接來自 Project Specification 或已確認的 project requirement。

### Derived Requirement

由 geometry、protocol 或 mathematical model 推導而來。

### Architecture Decision

目前已明確選定，RTL 必須遵守。

### RTL Implementation Constraint

Architecture 對 RTL arithmetic / coding behavior 所設定的硬性限制。

### Deferred Decision

目前刻意不提前決定，待 verification / synthesis evidence 後再處理。

---

# 3. Requirements Interpretation

## 3.1 Top-Level Interface

Top-level module 必須精確使用：

```text
module triangle (clk, reset, nt, xi, yi, busy, po, xo, yo);
```

Interface：

| Signal  | Direction | Width | Meaning                        |
| ------- | --------- | ----: | ------------------------------ |
| `clk`   | input     |     1 | Positive-edge triggered clock  |
| `reset` | input     |     1 | Active-high asynchronous reset |
| `nt`    | input     |     1 | New triangle transaction       |
| `xi`    | input     |     3 | Input X coordinate             |
| `yi`    | input     |     3 | Input Y coordinate             |
| `busy`  | output    |     1 | Engine busy indication         |
| `po`    | output    |     1 | Output coordinate valid        |
| `xo`    | output    |     3 | Output X coordinate            |
| `yo`    | output    |     3 | Output Y coordinate            |

---

## 3.2 Coordinate Domain

Coordinate domain 正式定義為：

$$
0\le x\le7
$$

$$
0\le y\le7
$$

即：

> **Coordinate Domain = 0~7**

這是目前已確認的 architecture decision。

---

## 3.3 Triangle Validity

Testbench 保證所有 input triangle 合法。

Triangle：

$$
P_1=(x_1,y_1)
$$

$$
P_2=(x_2,y_2)
$$

$$
P_3=(x_3,y_3)
$$

滿足：

$$
x_1=x_3
$$

$$
y_1<y_2<y_3
$$

其中：

* `P1 → P3` 為垂直邊
* `x1 = x3`
* `y1 < y2 < y3`

---

## 3.4 Invalid Triangle

以下情況不會出現在 testbench：

$$
x_1=x_2=x_3
$$

因此 architecture 不需要建立 invalid-triangle error handling。

同樣地，不需要為不符合：

$$
x_1=x_3,\quad y_1<y_2<y_3
$$

的輸入增加額外 recovery logic。

---

# 4. Input Protocol Architecture

## 4.1 Transaction Timing

一個 triangle transaction 使用三個 consecutive cycles：

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

## 4.2 Precise Definition of N+1 `busy`

**N+1 cycle 的定義必須以 clock edge 為界。**

N+1 cycle 指：

> **N+1 rising edge 之後，到 N+2 rising edge 之前的 clock interval。**

在 **N+1 rising edge**：

1. capture `(x2,y2)`
2. `busy` 更新為 `1`

因此：

```text
N+1 rising edge:
    x2/y2 captured
    busy <= 1

N+1 cycle:
    busy = 1

N+2 rising edge:
    x3/y3 captured
```

Timing：

```text
                N edge          N+1 edge          N+2 edge
                  │                 │                 │
                  │                 │                 │
nt                1                 0                 0
                  │                 │                 │
input             P1                P2                P3
                  │                 │                 │
busy              0 ────────────────┘                 │
                                    │                  │
                                    └──── busy = 1 ────┘
```

因此：

> **`busy` 必須在 capture P3 的 N+2 rising edge 之前就已經為 `1`。**

這是正式 architecture timing requirement。

---

## 4.3 Busy Protocol

當：

```text
busy = 0
```

系統可接受：

```text
nt = 1
```

Testbench 保證：

```text
busy = 1
```

期間不會送入新的 `nt`。

因此 architecture 不需要處理 busy 狀態下的新 triangle transaction。

---

# 5. Output Protocol Architecture

## 5.1 Output Validity

當：

```text
po = 1
```

則：

```text
xo, yo
```

代表本 cycle 的有效 triangle coordinate。

當：

```text
po = 0
```

則：

```text
xo, yo
```

為：

> **Don't Care**

不要求：

```text
xo = 0
yo = 0
```

---

## 5.2 No Forced Zero Logic

RTL 不需要為 `po=0` 建立額外 MUX 或 zero-forcing logic。

`xo/yo` 可以：

* 保持上一個有效值
* 保持 datapath 當下值

兩者均符合 architecture contract。

---

## 5.3 Continuous `po`

`po` 可以連續多 cycle 為 High。

例如：

```text
Cycle C:
    po = 1
    xo = 1
    yo = 2

Cycle C+1:
    po = 1
    xo = 2
    yo = 2

Cycle C+2:
    po = 1
    xo = 3
    yo = 2
```

不需要在兩個有效 output 之間插入 `po=0` bubble。

---

# 6. Reset Architecture

## 6.1 Reset Type

`reset` 為：

> Active-high asynchronous reset

---

## 6.2 Reset State

Reset 期間：

```text
busy = 0
po   = 0
```

Reset release 後，system 保持 idle-ready condition：

```text
busy = 0
po   = 0
```

直到下一個合法 `nt` transaction。

---

## 6.3 xo / yo Reset Behavior

`xo` / `yo` 在：

* reset 期間
* reset release 後
* 第一個 `po=1` 前

均為：

> **Don't Care**

因此不要求額外 reset value。

---

# 7. Behavioral Model

## 7.1 Triangle Scan

Candidate C 使用 bounded coordinate scan。

Y：

$$
y=y_1\rightarrow y_3
$$

X：

$$
x=\min(x_1,x_2)\rightarrow\max(x_1,x_2)
$$

對每一個 candidate coordinate `(x,y)`：

1. 計算 `E12`
2. 計算 `E23`
3. 判斷 Inside
4. 若 Inside，輸出 `(x,y)`
5. advance x
6. row 完成後 advance y

---

## 7.2 Behavioral Pseudocode

以下僅為 behavioral model，不代表 RTL 必須使用 software loop：

```text
y = y1

while y <= y3:

    x = min(x1, x2)

    while x <= max(x1, x2):

        calculate E12
        calculate E23

        if Inside(x,y):
            po = 1
            xo = x
            yo = y

        x = x + 1

    y = y + 1
```

---

# 8. Output Ordering

Output ordering 為：

> **由下而上掃描 y；每一個 y 內由左而右掃描 x。**

即：

```text
y = y1:
    x increasing

y = y1+1:
    x increasing

...

y = y3:
    x increasing
```

因此 output sequence 必須同時滿足：

1. Y 不下降
2. 同一個 Y 中 X 嚴格遞增
3. 完成目前 row 後才進入下一 row
4. 不輸出 triangle 外部 point
5. 不漏輸出 triangle 內部 / boundary point

---

# 9. Control Architecture

## 9.1 FSM

Current working FSM：

```text
IDLE
  │
  │ nt = 1
  ▼
CAPTURE_P2
  │
  │ capture P2
  │ busy -> 1
  ▼
CAPTURE_P3
  │
  │ capture P3
  ▼
SCAN
  │
  │ final scan/output
  ▼
IDLE
```

目前**不建立 RELEASE state**。

---

## 9.2 IDLE

功能：

* `busy = 0`
* `po = 0`
* 等待 `nt=1`

當 `nt=1`：

```text
x1 <= xi
y1 <= yi
```

並進入 `CAPTURE_P2`。

---

## 9.3 CAPTURE_P2

於 N+1 rising edge：

```text
x2 <= xi
y2 <= yi
busy <= 1
```

並進入 `CAPTURE_P3`。

這保證：

> `busy=1` 發生在 P3 capture 前。

---

## 9.4 CAPTURE_P3

於 N+2 rising edge：

```text
y3 <= yi
```

`x3` 不需要建立 dedicated register，原因見 Section 10。

完成後進入 `SCAN`。

---

## 9.5 SCAN

SCAN 負責：

* x/y counter
* E12/E23 arithmetic
* inside comparison
* `po`
* `xo`
* `yo`
* x advance
* y advance

最後一個 candidate/output 完成後直接回到 `IDLE`。

---

# 10. Register Architecture

## 10.1 Required Vertex Registers

目前不需要保存完整：

```text
x1, y1
x2, y2
x3, y3
```

原因：

$$
x_1=x_3
$$

為已確認的合法-input constraint。

因此 internal vertex registers 最小集合為：

```text
x1
y1
x2
y2
y3
```

共：

$$
5\times3=15
$$

個 coordinate storage bits。

---

## 10.2 Third Vertex Capture

雖然不需要 `x3` register，仍必須遵守 input protocol：

```text
N+2:
    xi,yi = x3,y3
```

其中：

```text
x3
```

不另存，而直接由：

```text
x1
```

代表。

`y3` 必須保存，因為：

$$
y_3
$$

是 scan upper boundary。

---

## 10.3 Scan Registers

需要：

```text
x_counter
y_counter
```

以及 arithmetic/control 所需的 internal state。

---

# 11. Candidate C Architecture

## 11.1 Candidate Definition

Candidate C：

> **Bounded Coordinate Scan**

掃描範圍：

$$
y_1\rightarrow y_3
$$

$$
\min(x_1,x_2)\rightarrow\max(x_1,x_2)
$$

每一個 candidate point 使用 geometric edge function 判斷。

---

## 11.2 Why Bounded Scan

不是掃描完整 coordinate domain：

$$
x=0\rightarrow7
$$

而是利用 triangle geometry：

$$
x_1=x_3
$$

將 X scan 限制為：

$$
\min(x_1,x_2)\rightarrow\max(x_1,x_2)
$$

因此：

* 減少 unnecessary scan cycles
* 保留 architecture simplicity
* 不需要增加 boundary divider
* 不改變 output ordering

---

# 12. Mathematical Model

## 12.1 General Edge Function

對：

$$
P_A=(x_A,y_A)
$$

$$
P_B=(x_B,y_B)
$$

定義：

$$
E_{A,B}(x,y)
=
(x-x_A)(y_B-y_A)
-
(x_B-x_A)(y-y_A)
$$

此式由 specification ratio relation cross multiplication 得到。

因此不需要 division。

---

## 12.2 E12

$$
E_{12}
=
(x-x_1)(y_2-y_1)
-
(x_2-x_1)(y-y_1)
$$

---

## 12.3 E23

$$
E_{23}
=
(x-x_2)(y_3-y_2)
-
(x_3-x_2)(y-y_2)
$$

因為：

$$
x_3=x_1
$$

因此：

$$
E_{23}
=
(x-x_2)(y_3-y_2)
-
(x_1-x_2)(y-y_2)
$$

---

## 12.4 E31

General form：

$$
E_{31}
=
(x-x_3)(y_1-y_3)
-
(x_1-x_3)(y-y_3)
$$

因：

$$
x_1=x_3
$$

所以：

$$
E_{31}
=
(x-x_1)(y_1-y_3)
$$

---

# 13. E31 Implementation Decision

雖然 E31 屬於完整 mathematical model，但 Candidate C **不實作 E31 datapath**。

原因：

1. triangle 的 X extent 已由：

   $$
   \min(x_1,x_2)\rightarrow\max(x_1,x_2)
   $$

   限制。
2. 此 X range 已經保證 candidate point 不會超出 vertical edge 的 horizontal extent。
3. 因此不需要額外 E31 multiplier。
4. 可降低 datapath area 與 arithmetic complexity。

所以 Candidate C 的 inside test 使用：

```text
E12
E23
X scan boundary
```

完成。

---

# 14. Inside Condition

## 14.1 Right-Facing Triangle

若：

$$
x_1<x_2
$$

則 triangle 位於 vertical edge `x=x1` 的右側。

使用：

$$
E_{12}\le0
$$

$$
E_{23}\le0
$$

因此：

$$
Inside=
(E_{12}\le0)
\land
(E_{23}\le0)
$$

X scan range 已保證：

$$
x_1\le x\le x_2
$$

所以不需要 E31。

---

## 14.2 Left-Facing Triangle

若：

$$
x_1>x_2
$$

則 triangle 位於 vertical edge `x=x1` 的左側。

使用：

$$
E_{12}\ge0
$$

$$
E_{23}\ge0
$$

因此：

$$
Inside=
(E_{12}\ge0)
\land
(E_{23}\ge0)
$$

X scan range 已保證：

$$
x_2\le x\le x_1
$$

所以同樣不需要 E31。

---

## 14.3 Boundary Inclusion

Boundary points 必須包含。

因此：

```text
right-facing:
    E12 <= 0
    E23 <= 0

left-facing:
    E12 >= 0
    E23 >= 0
```

不得改為 strict comparison。

---

# 15. Arithmetic Width and Signedness

## 15.1 MAJOR RTL Implementation Constraint

**E12 / E23 arithmetic 不得依賴 Verilog implicit signedness 或 implicit expression width propagation。**

RTL 必須明確建立 signed intermediate operands。

禁止將以下形式直接視為 architecture-approved arithmetic：

```text
(x - x1) * (y2 - y1)
```

原因包括：

* 原始 coordinates 是 3-bit unsigned
* subtraction 可能以 unsigned interpretation 執行
* intermediate width 可能不足
* multiplication result width 可能與預期不同
* final comparison 可能產生 signed / unsigned mismatch
* truncation 可能發生在 subtraction 或 multiplication 的中間階段

---

## 15.2 Coordinate Representation

Interface input：

```text
xi, yi : unsigned [2:0]
```

coordinate arithmetic 在進入 signed calculation 前，必須明確轉換為 signed representation。

建議 arithmetic representation：

```text
signed [3:0]
```

因 coordinate difference 範圍為：

$$
-7\le d\le7
$$

---

## 15.3 Coordinate Difference

所有 difference：

$$
x-x_A
$$

$$
y_B-y_A
$$

$$
x_B-x_A
$$

$$
y-y_A
$$

必須使用明確 signed operands。

差值範圍：

$$
-7\sim+7
$$

因此：

> **Difference width = at least 4-bit signed**

---

## 15.4 Product Width

Single product：

$$
P=(x-x_A)(y_B-y_A)
$$

每個 operand：

$$
-7\sim+7
$$

所以：

$$
-49\le P\le49
$$

因此：

> **Product representation = at least 8-bit signed**

因為 8-bit signed：

$$
-128\sim127
$$

---

## 15.5 Edge Function Range

E12/E23 都是：

$$
E=P_1-P_2
$$

而兩個 product 各自可能：

$$
-49\sim+49
$$

因此：

$$
-49-(+49)\le E\le+49-(-49)
$$

得到：

$$
\boxed{-98\le E\le+98}
$$

數學結果可由 8-bit signed 表示。

但 RTL arithmetic 不應讓 subtraction 在可能不足的 expression width 下直接發生。

因此 architecture 規定：

> **E12/E23 final arithmetic intermediate shall use at least 9-bit signed representation.**

---

## 15.6 Required Arithmetic Structure

Architecture-level arithmetic contract：

```text
3-bit unsigned coordinate
        │
        │ explicit conversion
        ▼
4-bit signed coordinate operand
        │
        │ subtraction
        ▼
4-bit signed difference
        │
        │ multiplication
        ▼
8-bit signed product
        │
        │ sign extension
        ▼
9-bit signed edge-function arithmetic
        │
        ▼
E12 / E23 comparison against zero
```

---

## 15.7 Explicit Sign Extension

E12/E23 subtraction 必須在足夠寬度下進行。

概念上：

```text
product_1 : signed [7:0]
product_2 : signed [7:0]

product_1_ext : signed [8:0]
product_2_ext : signed [8:0]

E = product_1_ext - product_2_ext
```

不得依賴 implicit sign extension。

---

## 15.8 RTL Coding Constraint

RTL implementation 必須明確處理：

* signed declaration
* operand width
* sign extension
* multiplication width
* subtraction width
* zero comparison signedness

不得因「模擬看起來正確」而忽略 expression sizing。

---

# 16. Cycle-Level Architecture

## 16.1 Input Cycles

```text
Cycle N:
    nt = 1
    capture x1/y1
    busy = 0

Cycle N+1:
    nt = 0
    capture x2/y2
    busy = 1 after N+1 rising edge

Cycle N+2:
    nt = 0
    capture y3
    x3 represented by x1
    busy = 1
```

---

## 16.2 Scan Initialization

完成 P3 capture 後，建立：

```text
x_counter = min(x1,x2)
y_counter = y1
```

進入 SCAN。

---

## 16.3 Scan Cycle

每個 scan cycle 處理一個 candidate `(x,y)`。

Conceptually：

```text
candidate point
      │
      ▼
calculate E12 / E23
      │
      ▼
Inside?
   /      \
 YES       NO
  │         │
  ▼         │
po = 1      │
xo = x      │
yo = y      │
  │         │
  └────┬────┘
       ▼
advance scan
```

---

## 16.4 X Advance

若目前：

$$
x<\max(x_1,x_2)
$$

則：

$$
x\leftarrow x+1
$$

若：

$$
x=\max(x_1,x_2)
$$

則完成 current row：

```text
x_counter = min(x1,x2)
y_counter = y_counter + 1
```

---

## 16.5 Final Row

若目前：

$$
y=y_3
$$

且完成該 row 最後一個 X candidate：

> scan 完成，直接 transition 至 `IDLE`。

目前不增加 RELEASE state。

---

# 17. Busy Release Decision

目前採用：

> **Final output / final scan 完成後直接回 `IDLE`。**

因此目前 FSM 不包含：

```text
RELEASE
```

state。

這是目前的 working implementation decision，而不是宣稱 Project Specification 必然要求此 FSM structure。

如果 verification 發現：

* `busy` release timing 不符合 testbench
* final `po` / `busy` relationship 不符合 expected waveform

則應重新 review control architecture，必要時增加：

```text
SCAN → RELEASE → IDLE
```

而非由 RTL engineer 自行修改 protocol。

---

# 18. Candidate B

Candidate B 為：

> Incremental Boundary Architecture

其概念是以 row boundary incremental calculation 取代 Candidate C 的 candidate-point scan。

Potential advantages：

* fewer scan cycles
* reduced number of point-by-point inside evaluations
* potential performance improvement

Potential disadvantages：

* quotient / remainder management
* floor behavior
* variable boundary calculation
* greater arithmetic/control complexity
* higher verification complexity

目前不實作 Candidate B。

---

# 19. Candidate B vs Candidate C

| Dimension                    | Candidate B          | Candidate C          |
| ---------------------------- | -------------------- | -------------------- |
| Current implementation       | No                   | **Yes**              |
| Control complexity           | Higher               | Lower                |
| Arithmetic algorithm         | More complex         | Direct edge-function |
| Division / quotient handling | Potentially required | Not required         |
| Verification                 | More complex         | Simpler              |
| Cycle efficiency             | Potentially better   | Lower                |
| Traceability                 | Medium               | High                 |
| Optimization potential       | High                 | Medium               |
| Current evidence             | None                 | None                 |

目前不能僅依上述 qualitative analysis 宣稱 Candidate B 的 `Time × Area` 一定優於 Candidate C。

實際判斷必須依：

* `ncverilog.log`
* Design Compiler report
* timing report

等 evidence。

---

# 20. Timing Considerations

## 20.1 Clock

使用：

```text
clk
```

positive-edge triggered synchronous operation。

Reset 為 asynchronous。

---

## 20.2 Critical Combinational Path Candidate

Candidate C 的主要 combinational datapath 為：

```text
coordinate registers/counters
        ↓
signed conversion
        ↓
subtraction
        ↓
multiplication
        ↓
sign extension
        ↓
edge-function subtraction
        ↓
comparison
        ↓
inside decision
```

這是 architecture-level potential timing path。

實際 critical path 必須由 synthesis / STA 確認。

---

## 20.3 Timing Verification Status

目前沒有：

* target clock frequency
* technology node
* standard-cell library
* complete SDC
* STA report

因此：

```text
Timing Status = NOT VERIFIED
```

---

# 21. Area Considerations

主要 resources：

* 5 個 vertex coordinate registers
* x/y scan counters
* E12 arithmetic
* E23 arithmetic
* comparators
* FSM
* output logic

`x3` dedicated register 被移除，因：

$$
x_3=x_1
$$

Candidate C 不使用 E31 multiplier。

實際 area：

```text
Area Status = NOT VERIFIED
```

必須由 Design Compiler report 判斷。

---

# 22. Power Considerations

Candidate C 每個 candidate point 都會進行 arithmetic evaluation，因此存在：

* multiplier switching
* subtraction switching
* comparator switching
* counter switching

目前未提供 power budget 或 power constraints。

因此：

```text
Power Status = NOT VERIFIED
```

目前不加入額外 clock gating / power optimization architecture。

---

# 23. Physical Implementation Considerations

Potential physical concerns：

1. multiplier placement
2. arithmetic combinational depth
3. routing between counters and arithmetic datapath
4. control fanout
5. clock/reset distribution
6. output path timing

目前 arithmetic width 很小，但不能僅依 width 推論 physical closure。

實際結果必須由：

* synthesis
* STA
* placement
* CTS
* routing
* DRC
* LVS

確認。

---

# 24. Verification Architecture

## 24.1 Reference Model

Reference model 可依：

```text
for y = y1 to y3:
    for x = min(x1,x2) to max(x1,x2):
        calculate E12
        calculate E23
        evaluate Inside
        if Inside:
            expected_output = (x,y)
```

建立 golden output stream。

---

## 24.2 Functional Checks

Verification 必須確認：

### Input

* 三個 vertex 依 N/N+1/N+2 capture
* `nt` 僅於 idle / `busy=0` 時有效

### Busy

* N+1 rising edge 後 `busy=1`
* capture P3 時 `busy` 已為 1

### Geometry

* 所有 output point 位於 triangle interior / boundary
* 不可漏點
* 不可多點

### Ordering

* y ascending
* 同一 y 中 x ascending

### Boundary

* E12/E23 = 0 的 point 必須輸出
* vertices 必須輸出

### Output

* `po=1` 時 `xo/yo` 有效
* `po` 可以連續 High
* `po=0` 時不檢查 `xo/yo`

### Reset

* reset → `busy=0`
* reset → `po=0`

---

# 25. Corner Cases

## 25.1 Right-Facing

$$
x_1<x_2
$$

使用：

$$
E_{12}\le0
$$

$$
E_{23}\le0
$$

---

## 25.2 Left-Facing

$$
x_1>x_2
$$

使用：

$$
E_{12}\ge0
$$

$$
E_{23}\ge0
$$

---

## 25.3 Lowest Vertex

$$
y=y_1
$$

必須包含：

$$
(x_1,y_1)
$$

---

## 25.4 Middle Vertex

$$
y=y_2
$$

必須正確包含：

$$
(x_2,y_2)
$$

且不得重複輸出。

---

## 25.5 Highest Vertex

$$
y=y_3
$$

必須包含：

$$
(x_3,y_3)
$$

而：

$$
x_3=x_1
$$

---

## 25.6 Coordinate Boundary

必須能處理：

```text
x = 0
x = 7
y = 0
y = 7
```

---

# 26. Architecture Risks

## 26.1 Arithmetic Signedness Risk — MAJOR

若 RTL 依賴 Verilog implicit signedness / width propagation，可能導致：

* incorrect subtraction
* incorrect multiplication
* overflow / truncation
* incorrect comparison

Mitigation：

> 明確使用 signed intermediate arithmetic，並遵守 Section 15 的 width contract。

---

## 26.2 Busy Timing Risk — MAJOR

若 RTL 將 `busy` 延至 N+2 才拉高，則違反：

> N+1 rising edge 後 `busy=1`

Mitigation：

> `busy` assertion 必須與 P2 capture 對應。

---

## 26.3 Final Busy Release Risk — DEFERRED

目前直接：

```text
SCAN → IDLE
```

若 testbench 對 final `busy` release 有更嚴格 timing expectation，可能需要：

```text
SCAN → RELEASE → IDLE
```

目前等待 verification evidence。

---

## 26.4 Candidate C Performance Risk

Candidate C 可能因逐 candidate scan 而具有較高 Time Complexity。

目前不以理論推測取代 actual tool measurement。

若 performance 不足，Candidate B 可作為 optimization path。

---

# 27. Architecture-to-RTL Constraints

以下項目為 RTL implementation 的 mandatory constraints：

### Interface

不得修改 top-level port list。

### Input Timing

不得修改：

```text
N     : P1
N+1   : P2
N+2   : P3
```

### Busy

必須：

```text
N+1 rising edge → busy = 1
```

### Coordinate

Coordinate domain：

```text
0~7
```

### Triangle

合法 input：

```text
x1 = x3
y1 < y2 < y3
```

### Scan

```text
y1 → y3
min(x1,x2) → max(x1,x2)
```

### Geometry

使用 E12/E23 cross-product formulation。

### E31

不得為了 Candidate C 的 inside test 額外加入 E31 datapath。

### Arithmetic

不得依賴 implicit signedness / width propagation。

Required minimum conceptual widths：

```text
difference : 4-bit signed
product    : 8-bit signed
E12/E23    : 9-bit signed intermediate
```

### Output

`po=1` 時 `(xo,yo)` 有效。

`po=0` 時 `(xo,yo)` Don't Care。

### Reset

```text
busy = 0
po   = 0
```

### Final Transition

目前：

```text
final scan → IDLE
```

不加入 RELEASE state。

---

# 28. Verification Status

目前尚未有 EDA evidence，因此：

```text
RTL Simulation        : NOT VERIFIED
Gate-Level Simulation : NOT VERIFIED
Synthesis             : NOT VERIFIED
STA                   : NOT VERIFIED
P&R                   : NOT VERIFIED
DRC                   : NOT VERIFIED
LVS                   : NOT VERIFIED
```

本 Architecture Document 不代表 RTL correctness 或 physical closure。

---

# 29. Deferred Decisions

## 29.1 Candidate B

待 Candidate C 完成：

* simulation
* synthesis
* timing
* area analysis

後，再決定是否進行 Candidate B optimization。

---

## 29.2 RELEASE State

目前不加入。

只有 verification 證明目前：

```text
SCAN → IDLE
```

無法滿足 protocol/testbench 時，才重新 review。

---

## 29.3 Performance Optimization

目前不提前進行：

* pipeline
* divider optimization
* arithmetic restructuring
* clock gating
* resource sharing

除非 synthesis / verification evidence 顯示有必要。

---

# 30. Traceability Matrix

| Decision                           | Type                            | Architecture Reference |
| ---------------------------------- | ------------------------------- | ---------------------- |
| Top-level interface                | Explicit                        | Section 3.1            |
| Coordinate 0~7                     | Architecture Decision           | Section 3.2            |
| Legal triangle                     | Explicit / Testbench guarantee  | Section 3.3            |
| `x1=x2=x3` excluded                | Confirmed constraint            | Section 3.4            |
| N/N+1/N+2 input                    | Explicit                        | Section 4              |
| `busy` asserted at N+1 rising edge | Architecture Decision           | Section 4.2            |
| `busy=1` before P3 capture         | Derived                         | Section 4.2            |
| `po` continuous High allowed       | Architecture Decision           | Section 5.3            |
| `xo/yo` Don't Care when `po=0`     | Architecture Decision           | Section 5              |
| reset `busy=0`                     | Explicit / confirmed            | Section 6              |
| reset `po=0`                       | Explicit / confirmed            | Section 6              |
| no `x3` register                   | Derived / Architecture Decision | Section 10             |
| bounded X scan                     | Architecture Decision           | Section 11             |
| E12/E23 edge function              | Derived mathematical model      | Section 12             |
| E31 not implemented                | Architecture Decision           | Section 13             |
| Candidate C                        | Working baseline                | Section 11             |
| Candidate B                        | Deferred optimization           | Section 18             |
| signed arithmetic                  | RTL constraint                  | Section 15             |
| Difference = 4-bit signed          | RTL constraint                  | Section 15.2           |
| Product = 8-bit signed             | RTL constraint                  | Section 15.4           |
| E = 9-bit signed intermediate      | RTL constraint                  | Section 15.5           |
| final scan → IDLE                  | Working decision                | Section 17             |

---

# 31. Handoff Requirements

RTL engineer 開始 implementation 前，必須理解以下 architecture contract：

```text
P1 → P2 → P3
      │
      └── busy asserted at N+1 edge

P3 capture
      │
      ▼
bounded coordinate scan
      │
      ├── E12
      ├── E23
      └── Inside
            │
            ▼
       po / xo / yo
            │
            ▼
          IDLE
```

RTL engineer 不得自行改變：

* interface
* input protocol
* busy timing
* output ordering
* scan range
* E12/E23 definition
* E31 implementation decision
* signed arithmetic contract
* reset semantics

若 implementation 遇到無法直接實現的問題，必須回報 Architecture Review，而不是自行改變 architecture。

---

# 32. Final Architecture Statement

目前正式 Architecture baseline 為：

> **Candidate C — Bounded Coordinate Scan**

其核心為：

$$
y=y_1\rightarrow y_3
$$

以及：

$$
x=\min(x_1,x_2)\rightarrow\max(x_1,x_2)
$$

對每一個 candidate coordinate 使用：

$$
E_{12}
=
(x-x_1)(y_2-y_1)
-
(x_2-x_1)(y-y_1)
$$

以及：

$$
E_{23}
=
(x-x_2)(y_3-y_2)
-
(x_1-x_2)(y-y_2)
$$

進行 inside test。

Candidate C：

* 不使用 division
* 不實作 E31
* 不保存 dedicated x3 register
* 使用 explicit signed arithmetic
* difference 至少 4-bit signed
* product 至少 8-bit signed
* E12/E23 intermediate 至少 9-bit signed
* `po` 可連續 High
* `po=0` 時 `xo/yo` Don't Care
* `busy` 於 N+1 rising edge 後為 High
* final scan 目前直接回 IDLE

目前：

```text
Architecture Status : FROZEN
RTL Status          : NOT STARTED / NOT VERIFIED
Candidate            : C
Optimization Path   : B
```

本文件為正式 Architecture artifact，不代表 Architecture 已經 FROZEN。

只有經 Human / Project Owner 明確 review 與 approval 後，才能將 status 更新為：

```text
FROZEN
```
