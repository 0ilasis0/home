# Architect Task: RTL 1.1.0 Optimization Candidates 1–4

## 1. Task Identity

**Project:** Triangle Rendering Engine
**Current RTL:** Version 1.1.0
**Current Baseline ID:** `RTL-BASE-A1-TASK1-BUGFIX-2`
**Current Change:** `ARCH-CHANGE-OUTPUT-SCAN-EARLY-EXIT`
**Architecture Status:** A1 PENDING / A2 NOT FROZEN
**Task Type:** Architecture Optimization Feasibility Analysis

---

# 2. Objective

針對目前 RTL 1.1.0，僅分析以下四個 optimization candidate：

1. `x2 < x1` OUTPUT_SCAN leading-zero skip / row-start detection
2. `E_trace` width reduction
3. `dx_ext / dy12_ext / dy32_ext` arithmetic width reduction
4. Remove redundant `x3` storage register

本任務：

> **只做 Architecture Analysis / Change Proposal，不修改 RTL。**

不得直接交由 Engineer 實作。

---

# 3. Current RTL Baseline

目前 RTL 1.1.0 已包含：

```text
ARCH-CHANGE-OUTPUT-SCAN-EARLY-EXIT
```

目前 OUTPUT_SCAN 行為：

```text
x2 > x1:
    current_inside == 0
        → terminate current row
        → y++

x2 < x1:
    preserve full x_left → x_right scan
```

目前 Final TB 已有：

```text
PASS
Total delay = 124000 ns
```

舊 RTL 1.0.2：

```text
146000 ns
```

目前尚未進行本次 optimization candidates 的 exhaustive regression。

---

# 4. Candidate 1

# `x2 < x1` Leading-Zero Skip / Row-Start Detection

## 4.1 Current Problem

當：

```text
x2 < x1
```

目前：

```text
x_left = x2
x_right = x1
```

OUTPUT_SCAN 必須：

```text
x_left → x_right
```

完整掃描。

因此某些 row 會出現：

```text
invalid
invalid
invalid
valid
valid
valid
invalid
```

目前無法像 `x2 > x1` 一樣，在找到 trailing invalid 後立即結束 row。

---

## 4.2 Architect 必須分析

研究是否可以在不破壞：

```text
y ascending
x ascending
registered po/xo/yo
interface
busy protocol
RELEASE
ylow/yup semantics
```

的情況下：

> 跳過 `x2 < x1` 情況下的 leading invalid candidates。

---

## 4.3 必須證明

對所有合法 triangle：

```text
x1 = 0..7
y1 = 0..7
y2 = y1+1..7
y3 = y2+1..7
x2 = 0..7
x2 != x1
x3 = x1
```

必須證明：

### Property A

固定 y 時 valid x set 是 contiguous interval。

### Property B

可以正確取得：

```text
row_start_x(y)
```

### Property C

取得 row_start 不會漏掉第一個 valid point。

### Property D

取得 row_start 不會產生 invalid `po=1`。

### Property E

同一 y 的 output ordering 仍然：

```text
x ascending
```

---

## 4.4 必須比較的 implementation approaches

至少比較：

### Approach A

維持目前 full scan。

### Approach B

增加 row-start detection。

### Approach C

直接由 geometry equation 計算 row start。

### Approach D

利用既有 `ylow/yup` information 搜尋第一個 valid x。

必須分析：

```text
cycle reduction
area
combinational logic
critical path
control complexity
verification complexity
```

---

## 4.5 特別要求

不得因為此 optimization：

```text
自動加入 row_started
```

除非 Architect 證明這是最小必要 control。

如果需要新增 register/state：

必須明確列出：

```text
register name
width
purpose
reset value
update condition
```

---

# 5. Candidate 2

# `E_trace` Width Reduction

## 5.1 Current RTL

目前：

```verilog
reg signed [8:0] E_trace;
```

也就是：

```text
signed 9-bit
range = -256 ~ +255
```

---

## 5.2 Objective

研究是否能安全將：

```text
9-bit
```

縮小至：

```text
8-bit
```

或其他最小安全 width。

---

## 5.3 Mandatory Range Analysis

不得只看 declaration。

必須完整分析：

```text
LOWER_INIT
LOWER_TRACE
LOWER_CLAMP
UPPER_INIT
UPPER_TRACE
UPPER_CLAMP
```

以及所有 arithmetic：

```text
E_trace + dx
E_trace - dy12
E_trace + dy32
E_trace - dy32
```

必須考慮：

```text
x1 = 0..7
x2 = 0..7
y1 = 0..7
y2 = y1+1..7
y3 = y2+1..7
x2 != x1
```

---

## 5.4 必須找出

```text
minimum possible E_trace
maximum possible E_trace
```

並分別分析：

```text
LOWER
UPPER
x2 > x1
x2 < x1
```

以及：

```text
initial value
maximum excursion
maximum absolute value
```

---

## 5.5 Width Decision

Architect 必須明確提出：

```text
Current:
9-bit signed

Candidate:
8-bit signed
```

是否：

```text
SAFE
UNSAFE
NEEDS FURTHER ANALYSIS
```

如果可以縮小：

必須說明：

```text
required minimum width
signed range
worst-case value
proof basis
```

---

# 6. Candidate 3

# `dx_ext / dy12_ext / dy32_ext` Width Reduction

## 6.1 Current RTL

目前：

```verilog
wire signed [8:0] dx_ext;
wire signed [8:0] dy12_ext;
wire signed [8:0] dy32_ext;
```

它們主要是配合 9-bit `E_trace` arithmetic。

---

## 6.2 Objective

分析：

> 如果 `E_trace` width 可以降低，相關 arithmetic extension 是否也可以同步縮減？

不得只因為：

```text
input coordinate = 3-bit
```

就直接宣稱可以縮小。

---

## 6.3 必須分析

對：

```text
dx
dy12
dy32
```

分別找出：

```text
minimum
maximum
required signed range
minimum safe width
```

合法 domain：

```text
dx   ∈ [-7, +7]
dy12 ∈ [1, +7]
dy32 ∈ [-7, -1]
```

但是必須進一步分析：

> arithmetic expression 的 operand width 與 result width 是否仍然安全。

必須避免：

```text
Verilog implicit truncation
signedness mismatch
expression width mismatch
overflow
```

---

## 6.4 必須分析的 downstream effect

如果：

```text
E_trace 9 → 8 bit
```

則必須分析：

```text
dx_ext
dy12_ext
dy32_ext
```

是否可以同步變更。

並評估：

```text
adder width
subtractor width
comparator width
routing
area
timing
```

---

# 7. Candidate 4

# Remove Redundant `x3` Storage

## 7.1 Current RTL

目前：

```verilog
reg [2:0] x1, y1, x2, y2, x3, y3;
```

P3 capture：

```verilog
CAPTURE_P3: begin
    x3 <= xi;
    y3 <= yi;
end
```

---

## 7.2 Architectural Invariant

Project constraint：

```text
x3 = x1
```

因此目前需要分析：

> `x3` 是否 functionally redundant。

---

## 7.3 Mandatory Analysis

搜尋 RTL 中所有：

```text
x3
```

的 usage。

必須確認：

```text
geometry
tracer
column storage
scanner
FSM
output
reset
```

是否有任何 logic 真正依賴 `x3` register。

---

## 7.4 Candidate Change

如果確認：

```text
x3 is never functionally consumed
```

則分析是否可以：

```text
remove x3 register
```

但必須確認：

* P3 input protocol 不變
* P3 cycle 不變
* `y3` capture 不變
* interface 不變
* reset behavior 不受影響
* FSM latency 不變
* synthesis 不會因 removal 產生 unexpected logic

---

## 7.5 Area Estimation

至少估算：

```text
x3 register:
3 flip-flops
```

以及：

```text
reset logic
routing
clock load
```

可能節省多少。

不得只寫：

```text
3 FF saved
```

就宣稱 area 一定改善。

---

# 8. Cross-Candidate Interaction

Architect 必須特別分析：

```text
Candidate 1
Candidate 2
Candidate 3
Candidate 4
```

是否互相依賴。

至少分析：

### Case A

```text
E_trace 9 → 8
+
dx_ext/dy12_ext/dy32_ext width reduction
```

### Case B

```text
Remove x3
```

是否與其他 change independent。

### Case C

```text
row-start optimization
+
existing x2>x1 early exit
```

是否可以形成：

```text
both-direction early exit
```

---

# 9. Performance Analysis

目前已知：

```text
RTL 1.0.2:
146000 ns

RTL 1.1.0:
124000 ns
```

Architect 不得假設 Candidate 1 一定改善固定比例。

必須建立：

```text
best case
worst case
typical case
```

的 cycle-level reasoning。

---

# 10. Area Analysis

因目前沒有新的 synthesis report：

禁止宣稱：

```text
area reduced
timing improved
```

只能寫：

```text
EXPECTED
POTENTIAL
NOT VERIFIED
```

所有 area / timing 結論必須標記為：

```text
NOT VERIFIED
```

直到 synthesis / STA evidence available。

---

# 11. Verification Impact

本階段不要求 Engineer 執行 regression。

Architect 只需要分析：

每一個 candidate 未來需要重新驗證什麼。

至少包含：

```text
directed cases
3136 exhaustive triangles
Final TB
output ordering
protocol
reset
boundary
|dx|=1
x2>x1
x2<x1
```

---

# 12. Architecture Change Boundary

Architect 不得直接修改：

```text
RTL 1.1.0
```

不得建立新的 RTL version。

本任務只產生：

```text
Architecture Optimization Analysis
```

如果認為值得進入 implementation：

必須建立獨立 change proposal。

---

# 13. Required Deliverable

Architect 必須回覆以下內容：

## 13.1 Candidate 1

```text
Feasible: YES / NO / NEEDS ANALYSIS
Expected cycle reduction
Area impact
Timing impact
Control impact
Architecture impact
Risk
Recommendation
```

## 13.2 Candidate 2

```text
E_trace required range
Minimum safe width
Overflow proof
Area impact
Timing impact
Risk
Recommendation
```

## 13.3 Candidate 3

```text
dx required width
dy12 required width
dy32 required width
Arithmetic safety
Area impact
Timing impact
Risk
Recommendation
```

## 13.4 Candidate 4

```text
x3 usage audit
Functional redundancy
Estimated area impact
Protocol impact
Risk
Recommendation
```

---

# 14. Final Recommendation

Architect 最後必須將四項分類：

```text
APPROVE FOR ENGINEER STUDY
```

或：

```text
REJECT
```

或：

```text
DEFER
```

不得直接宣布：

```text
A2 FROZEN
```

也不得直接要求 Engineer 修改 RTL。

---

# 15. Strict Change Control

若任何 candidate 需要修改：

```text
interface
input protocol
output protocol
clock
reset
FSM architecture
tracer architecture
column storage
ylow/yup semantics
arithmetic algorithm
output ordering
RELEASE
Final Testbench
reference model
```

則：

```text
STOP
```

並標記：

```text
ARCHITECTURE CHANGE REQUIRED
```

不得自行擴大 scope。

---

# 16. Project Control Priority

分析時依以下優先順序：

```text
Correctness
>
Traceability
>
Verifiability
>
Physical feasibility
>
Optimization
>
Code elegance
```

不得為了：

```text
cycle reduction
```

而犧牲：

```text
functional correctness
protocol stability
verification simplicity
```

也不得為了：

```text
small area reduction
```

引入 disproportionate timing / verification risk。
