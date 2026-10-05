# 8×8 Signed Output-Stationary Systolic Matrix Multiplier

## Architecture Specification v1.0

**Status:** PROPOSED — NOT FROZEN
**Architecture:** 8×8 Output-Stationary Systolic PE Array
**Data Type:** Signed two's-complement
**Primary Objective:** Functional correctness → verifiability → physical feasibility → minimize Area × Timing

---

# 1. Executive Summary

本設計為一個 8×8 signed integer matrix multiplier：

$$
C=A\times B
$$

其中：

$$
C_{i,j}=\sum_{k=0}^{7}A_{i,k}B_{k,j}
$$

輸入 A 與 B 以 systolic wavefront 方式送入 8×8 PE array：

* A 從每一列左側進入，向右傳遞。
* B 從每一欄上方進入，向下傳遞。
* 每個 PE 負責一個固定的 `C[i][j]`。
* 每個 PE 在本地累積 8 個 products。
* Partial sum 不在 PE 之間傳遞。
* 運算完成後直接讀取各 PE 的 `acc_out`，依 row-major 順序輸出。

本架構不使用 output latch；output phase 時停止 PE forwarding/MAC，使 64 個 `acc_out` 保持穩定。

---

# 2. Fixed Project Configuration

| Parameter     | Value | Constraint          |
| ------------- | ----: | ------------------- |
| `ARRAY_SIZE`  |     8 | 本專案 required value  |
| `DATA_WIDTH`  |     8 | signed              |
| `ACC_WIDTH`   |    32 | `>= 2 × DATA_WIDTH` |
| `INDEX_WIDTH` |     3 | 8×8 requires 3 bits |

Input range：

$$
-128 \leq A,B \leq 127
$$

Single product：

$$
8\times8\rightarrow16\text{-bit signed}
$$

Accumulator：

$$
32\text{-bit signed}
$$

---

# 3. Functional Model

矩陣：

$$
A[8][8],\quad B[8][8]
$$

輸出：

$$
C[8][8]
$$

每個 PE：

$$
PE[i][j]\rightarrow C[i][j]
$$

其最終結果：

$$
C[i][j]
=
\sum_{k=0}^{7}A[i][k]\times B[k][j]
$$

PE 不將 partial sum 傳給相鄰 PE。

---

# 4. Data Representation

## 4.1 Input

所有 input elements 為 8-bit signed two's-complement。

Input packing 為 LSB-first：

```text
bus[7:0]     = channel 0
bus[15:8]    = channel 1
...
bus[63:56]   = channel 7
```

A：

```text
a_stream[i] = A row i
```

B：

```text
b_stream[j] = B column j
```

---

# 5. PE Architecture

每個 PE 包含：

1. `a_out` forwarding register
2. `b_out` forwarding register
3. `acc_out` accumulator

Conceptually：

```text
             b_in
               │
               ▼
          ┌──────────┐
a_in ───► │   PE     │ ───► a_out
          │          │
          │  MAC     │
          │          │
          └──────────┘
               │
               ▼
             b_out
```

其中：

```text
a_out → 下一個 column 的 a_in
b_out → 下一個 row 的 b_in
```

---

# 6. PE Control Behavior

PE control priority：

### Priority 1 — asynchronous reset

```text
reset_n = 0
```

立即：

```text
a_out  = 0
b_out  = 0
acc_out = 0
```

### Priority 2 — synchronous clear

`clear=1` 時：

```text
a_out  = 0
b_out  = 0
acc_out = 0
```

### Priority 3 — computation

```text
clear = 0
enable = 1
```

執行：

```text
a_out <= a_in
b_out <= b_in

acc_out <= acc_out + sign_extended(a_in × b_in)
```

### Priority 4 — inactive forwarding

```text
clear = 0
enable = 0
```

架構決策：

```text
a_out = 0
b_out = 0
acc_out = hold
```

因此 output phase 不會修改 accumulator。

---

# 7. Arithmetic Architecture

Input：

```text
a_in : signed [7:0]
b_in : signed [7:0]
```

Product：

$$
P=A\times B
$$

得到 16-bit signed product。

之後 sign-extension：

$$
P_{ext}=\operatorname{SignExtend}_{32}(P)
$$

Accumulator：

$$
ACC_{next}
=
(ACC+P_{ext})\bmod2^{32}
$$

因此 overflow 行為為 two's-complement truncation。

沒有：

* saturation
* overflow exception
* exception flag

---

# 8. Accumulation Range

對本專案合法 input，8 個 product 的實際數學結果範圍約為：

$$
-130048 \leq C_{i,j}\leq131072
$$

遠小於 signed 32-bit range：

$$
-2^{31}\leq ACC\leq2^{31}-1
$$

但 architecture **仍採用規格要求的 32-bit two's-complement accumulation semantics**，而非依目前 8×8 input range 縮減 accumulator width。

---

# 9. Systolic Data Movement

A 向右：

```text
A[i][k]
   │
   ▼
PE[i][0] → PE[i][1] → ... → PE[i][7]
```

B 向下：

```text
B[k][j]
   │
   ▼
PE[0][j]
   │
   ▼
PE[1][j]
   │
   ▼
...
   │
   ▼
PE[7][j]
```

---

# 10. Input Skewing

A stream：

$$
a\_stream[i](t)=
\begin{cases}
A[i][t-i], & 0\leq t-i<8\\
0,&otherwise
\end{cases}
$$

B stream：

$$
b\_stream[j](t)=
\begin{cases}
B[t-j][j], & 0\leq t-j<8\\
0,&otherwise
\end{cases}
$$

因此對 PE `[i][j]`：

$$
A[i][k]
$$

與

$$
B[k][j]
$$

會在：

$$
\boxed{t=k+i+j}
$$

同一 computation cycle 到達。

---

# 11. Computation Schedule

Computation cycle 定義：

```text
t = 0 ... 21
```

共：

$$
22\text{ cycles}
$$

有效 wavefront：

```text
t = 0 ... 14
```

drain：

```text
t = 15 ... 21
```

因此 input stream 在：

```text
t=15..21
```

全部提供 zero，以完成 systolic pipeline drain。

最後一個有效 product：

```text
A[7][7] × B[7][7]
```

於：

```text
t = 7 + 7 + 7 = 21
```

抵達 PE[7][7] 並完成最後一次 MAC。

---

# 12. Start / Computation Boundary

當：

```text
busy = 0
start = 1
```

controller 接受新 operation。

該 clock edge：

```text
accept_start = 1
clear = 1
busy = 1
```

所有 PE synchronous clear。

**此 edge 不算 computation cycle 0。**

下一個 rising edge：

```text
t = 0
enable = 1
```

正式開始 computation。

---

# 13. Computation Enable

PE enable：

$$
enable = busy \land \lnot output\_active
$$

在 computation：

```text
t=0  ... t=21
```

皆：

```text
enable = 1
```

特別是：

```text
t=21 → enable = 1
```

確保最後抵達 PE[7][7] 的 product 完成 MAC。

---

# 14. Output Phase

Computation 完成後：

```text
t=22
```

開始 output phase。

Output phase：

$$
64\text{ cycles}
$$

每一個 cycle：

```text
result_valid = 1
busy = 1
enable = 0
```

PE accumulator 保持：

```text
acc_out = hold
```

Output controller 直接選擇 PE 的 `acc_out`。

不建立額外 output latch/register。

---

# 15. Output Ordering

輸出採 row-major：

```text
C[0][0]
C[0][1]
...
C[0][7]

C[1][0]
...
C[7][7]
```

每一個 output cycle：

```text
result_data = selected PE acc_out
result_row  = row index
result_col  = column index
```

最後一筆：

```text
result_data = C[7][7]
result_row  = 7
result_col  = 7
result_last = 1
result_valid = 1
busy = 1
```

---

# 16. Result / Done Timing

`result_last` 與 `done` 不在同一 cycle。

最後 output cycle：

```text
result_valid = 1
result_last  = 1
busy         = 1
done         = 0
```

下一 cycle：

```text
result_valid = 0
result_last  = 0
busy         = 0
done         = 1
```

`done` pulse 寬度為 one cycle。

---

# 17. Controller Architecture

Controller 可採用 FSM。

建議狀態：

```text
IDLE
  │
  │ start
  ▼
CLEAR / ACCEPT
  │
  ▼
COMPUTE
  │
  │ 22 cycles complete
  ▼
OUTPUT
  │
  │ final result sampled
  ▼
DONE
  │
  ▼
IDLE
```

## IDLE

條件：

```text
busy = 0
```

若：

```text
start = 1
```

接受 operation。

---

## CLEAR / ACCEPT

執行：

```text
accept_start = 1
clear = 1
busy = 1
```

完成所有 PE synchronous clear。

下一 rising edge 進入 computation cycle 0。

---

## COMPUTE

計數：

```text
compute_count = 0..21
```

每 cycle：

```text
enable = 1
```

當：

```text
compute_count == 21
```

完成最後 computation cycle。

下一 cycle 進入 OUTPUT。

---

## OUTPUT

計數：

```text
output_count = 0..63
```

其中：

$$
row = output\_count[5:3]
$$

$$
col = output\_count[2:0]
$$

每 cycle：

```text
result_valid = 1
busy = 1
enable = 0
```

當：

```text
output_count == 63
```

輸出：

```text
C[7][7]
result_last = 1
```

下一 cycle 進入 DONE。

---

## DONE

輸出：

```text
done = 1
busy = 0
result_valid = 0
```

下一 cycle 回到 IDLE。

---

# 18. Controller Counters

## Computation counter

需要表示：

```text
0..21
```

因此至少：

$$
\lceil\log_2(22)\rceil=5
$$

使用 5-bit counter。

## Output counter

需要表示：

```text
0..63
```

因此：

$$
\lceil\log_2(64)\rceil=6
$$

使用 6-bit counter。

## Row / Column

8×8：

```text
row : 3 bits
col : 3 bits
```

可由 6-bit `output_count` decode：

```text
row = output_count[5:3]
col = output_count[2:0]
```

---

# 19. Busy / Start Protocol

只有：

```text
busy = 0
```

時接受 `start`。

若：

```text
busy = 1
start = 1
```

則：

```text
start ignored
```

本設計沒有 `ready`。

Receiver 不可 stall。

---

# 20. Output Data Selection

Output controller 使用 row-major selection：

```text
output_count = 0
    → PE[0][0].acc_out

output_count = 1
    → PE[0][1].acc_out

...

output_count = 63
    → PE[7][7].acc_out
```

架構上不增加 64 個 output registers。

這使 accumulator 本身同時作為：

1. computation state
2. final result storage

以降低額外 storage。

---

# 21. Clock Architecture

目前採用單一 clock domain。

所有：

* controller registers
* PE forwarding registers
* PE accumulators
* output control

使用同一 clock。

不建立：

* derived clock
* generated clock
* clock gating logic

目前 clock enable 由：

```text
enable
```

控制資料路徑活動，而非產生額外 clock。

---

# 22. Reset Architecture

Reset：

```text
reset_n
```

為：

```text
asynchronous active-low
```

作用於 controller 與 PE state。

Reset assertion 時立即將相關 state 清除至定義初值。

PE：

```text
a_out   = 0
b_out   = 0
acc_out = 0
```

Controller：

```text
state         = IDLE
busy          = 0
result_valid  = 0
done          = 0
compute_count = 0
output_count  = 0
```

目前架構決策採 asynchronous reset release；後續實體 flow 仍需對 reset recovery/removal 進行實際檢查。

---

# 23. Parameterization Policy

RTL 保留 parameters：

```text
ARRAY_SIZE
DATA_WIDTH
ACC_WIDTH
INDEX_WIDTH
```

但本專案 required configuration：

```text
ARRAY_SIZE  = 8
DATA_WIDTH  = 8
ACC_WIDTH   = 32
INDEX_WIDTH = 3
```

若修改 parameters，必須重新檢查：

* product width
* accumulation range
* overflow semantics
* compute counter width
* output counter width
* row/column width
* skew schedule
* drain cycles
* verification model

因此 parameterization 不代表本專案 verification automatically covers arbitrary parameter values。

---

# 24. Candidate Architecture Comparison

## Candidate A — Full 8×8 Output-Stationary Systolic Array

64 PEs。

每個 PE：

```text
1 multiplier
1 accumulator
2 forwarding registers
```

優點：

* 完全符合 output-stationary specification
* 64 個 C elements 可同時累積
* 固定 22-cycle computation
* control relatively simple
* 高度規則化 physical structure
* local data movement

缺點：

* multiplier / register 數量較高
* 面積較大

---

## Candidate B — Shared / Time-Multiplexed Multipliers

減少 multiplier 數量，讓不同 matrix elements 共用 arithmetic resources。

優點：

* 潛在 area reduction

缺點：

* 需要更複雜 scheduling
* throughput 降低
* control complexity 增加
* 與既定 22-cycle systolic architecture 不一致
* 會改變 architecture，而非單純 implementation optimization

**不推薦。**

---

## Candidate C — Pipelined / Rebalanced PE

對 PE arithmetic path 做額外 pipeline。

優點：

* 可能降低 critical path

缺點：

* 改變 cycle-level behavior
* 改變 latency
* 必須重新定義 systolic alignment
* 與已確立的 22-cycle computation schedule 衝突

**目前不採用。**

---

# 25. Recommended Architecture

推薦：

$$
\boxed{\text{Candidate A: 8×8 Output-Stationary Systolic Array}}
$$

理由：

1. 完全符合 project specification。
2. 每個 PE 固定對應一個 `C[i][j]`。
3. Partial sum 保持 local。
4. Data movement 規則且高度 regular。
5. 22-cycle computation schedule 已明確定義。
6. output phase 不需額外 output storage。
7. control FSM 簡單。
8. physical placement 可以自然映射為 8×8 PE array。
9. 不需要為了 area speculation 而改變既定 architecture。

Area × Timing optimization 應優先在**不改變 architecture contract**的範圍內進行，例如：

* synthesis optimization
* arithmetic implementation
* cell selection
* physical placement
* buffering
* routing optimization

而不是透過任意改變 systolic schedule、latency 或 protocol 來取得結果。

---

# 26. Timing / Area Objective

目前沒有硬性：

* maximum area
* minimum frequency
* maximum clock period
* maximum delay

因此 optimization objective：

$$
\boxed{\min(Area\times Timing)}
$$

其中 timing 必須透過實際 synthesis/STA measurement 取得。

Architecture level 不預先宣稱任何：

* critical path
* delay
* slack
* frequency
* area

這些皆屬於後續 measured evidence。

---

# 27. Verification Architecture

Verification 至少應覆蓋：

## Functional

* 正常矩陣乘法
* 全 0
* identity matrix
* positive values
* negative values
* mixed signs
* maximum positive
* maximum negative

## Systolic timing

驗證：

$$
t=k+i+j
$$

並確認：

* wavefront `t=0..14`
* drain `t=15..21`
* final product `t=21`
* output start `t=22`

## Output

確認：

```text
C[0][0] → C[7][7]
```

row-major ordering。

確認：

```text
result_last
```

只在最後一筆 output assertion。

## Control

驗證：

* start while idle
* start while busy
* busy duration
* 22 computation cycles
* 64 output cycles
* done one-cycle pulse
* done after result_last

## Reset

驗證：

* reset during idle
* reset during computation
* reset during output
* asynchronous assertion
* state recovery

## Arithmetic

驗證：

* signed multiplication
* sign extension
* 32-bit accumulation
* two's-complement truncation
* no saturation

---

# 28. Important Assertions / Invariants

建議建立以下 invariants。

### Busy

正常 operation：

```text
start accepted
→ busy = 1
→ final result sampled
→ busy = 0
```

### Output stability

Output phase：

```text
enable = 0
```

因此：

```text
acc_out must not change
```

### Result validity

```text
result_valid = 1
```

時：

```text
result_data
result_row
result_col
```

必須對應目前 output counter。

### Final result

```text
result_last = 1
```

必須代表：

```text
result_row = 7
result_col = 7
```

### Done

```text
result_last(t) = 1
→ done(t+1) = 1
```

而：

```text
done(t) = 0
```

在 `result_last` cycle。

---

# 29. Cycle-Level Master Timeline

| Phase         |  Relative cycle | `busy` | `enable` | `result_valid` | Main action              |
| ------------- | --------------: | -----: | -------: | -------------: | ------------------------ |
| Accept        |      start edge |      1 |        0 |              0 | clear all PE             |
| Compute       |             t=0 |      1 |        1 |              0 | first skewed data        |
| Compute       |         t=1..14 |      1 |        1 |              0 | wavefront                |
| Drain         |        t=15..20 |      1 |        1 |              0 | zero input               |
| Final compute |            t=21 |      1 |        1 |              0 | final MAC                |
| Output        |        t=22..84 |      1 |        0 |              1 | C[0][0]..C[7][6]         |
| Final output  |            t=85 |      1 |        0 |              1 | C[7][7], `result_last=1` |
| Done          |      next cycle |      0 |        0 |              0 | `done=1`                 |
| Idle          | following cycle |      0 |        0 |              0 | wait for start           |

其中 output 共：

$$
64\text{ cycles}
$$

因而：

$$
22+64=86
$$

個 operation-relative cycles（不含 start acceptance edge 作為 computation cycle）。

---

# 30. Physical Design Considerations

PE array 天然形成規則 8×8 topology：

```text
       B0     B1     B2          B7
       ↓      ↓      ↓            ↓
A0 →  PE00 → PE01 → PE02 → ... → PE07
      ↓
A1 →  PE10 → PE11 → PE12 → ... → PE17
      ↓
...
A7 →  PE70 → PE71 → PE72 → ... → PE77
```

此規則結構有利於：

* local interconnect
* regular placement
* predictable data movement
* systolic routing

主要 physical risks：

* multiplier area
* PE-to-PE routing
* input fanout
* output selection mux
* clock/reset distribution
* output mux timing

實際風險程度必須由 synthesis / P&R evidence 判定。

---

# 31. Known / Estimated / Unknown

## Known

* 8×8 array
* 8-bit signed input
* 32-bit accumulator
* output-stationary
* 22 computation cycles
* 64 output cycles
* row-major output
* asynchronous active-low reset
* zero forwarding output when disabled
* two's-complement overflow
* no saturation
* no exception flag

## Estimated / Reasoned

* 8×8 systolic array 的規則 physical topology 有利於 implementation
* Candidate A 相較 shared-resource architecture 更符合目前 specification
* output direct-from-accumulator 可避免額外 64-entry output storage

以上不是 synthesis / P&R measurement。

## Unknown / Not Yet Measured

* critical path
* data arrival time
* slack
* maximum frequency
* area
* power
* congestion
* routing delay
* actual PVT timing
* DRC/LVS

---

# 32. Architecture Risks

| Risk                           | Impact                 | Mitigation                                              |
| ------------------------------ | ---------------------- | ------------------------------------------------------- |
| 64 multipliers                 | Area                   | synthesis measurement / arithmetic optimization         |
| Output mux                     | Timing                 | physical-aware synthesis / placement                    |
| PE routing                     | Timing / congestion    | regular placement                                       |
| Async reset distribution       | Reset timing           | reset-tree implementation and recovery/removal analysis |
| Parameter changes              | Functional correctness | re-run width/cycle/verification analysis                |
| Timing environment unspecified | STA                    | define implementation flow before signoff               |

---

# 33. Freeze Criteria

Architecture 在以下項目完成 review 並獲得 project owner 明確批准前，不得標記 FROZEN：

* Requirements interpretation
* Interface definition
* Start/busy protocol
* Reset behavior
* PE behavior
* Systolic timing
* 22-cycle computation
* 64-cycle output
* `result_last` / `done` relationship
* Arithmetic / signedness
* Overflow semantics
* Parameter semantics
* Controller FSM
* Datapath architecture
* Verification plan
* Physical considerations
* Area × Timing optimization objective

---

# 34. Current Architecture Status

$$
\boxed{\text{PROPOSED — NOT FROZEN}}
$$

本文件 v1.0 為第一版 architecture baseline。

任何後續 RTL 實作都不得自行修改：

* array size
* systolic dataflow
* output-stationary behavior
* computation latency
* output ordering
* handshake semantics
* reset semantics
* arithmetic semantics

若 RTL / synthesis / STA / physical implementation 發現上述 architecture 存在問題，應依：

```text
Evidence
→ Root Cause
→ Affected Architecture Decision
→ Alternatives
→ Consequences
→ Human Review
→ Approval
→ Architecture Update
```

流程處理。
