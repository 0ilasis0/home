# Architecture Specification v1.0

## 8×8 Signed Matrix Multiplier — Output-Stationary Systolic Architecture

**Document Status:** PROPOSED
**RTL Readiness:** READY FOR RTL
**Freeze Status:** NOT FROZEN
**Architecture Version:** v1.0
**Primary Objective:** Correctness → Traceability → Verifiability → Physical Feasibility → Area × Timing Optimization

---

# 1. Executive Summary

本設計為一個 **8×8 signed integer matrix multiplier**，採用 **Output-Stationary Systolic Array** 架構。

輸入矩陣：

$$
A \in \mathbb{Z}^{8\times8}
$$

$$
B \in \mathbb{Z}^{8\times8}
$$

輸出矩陣：

$$
C=A\times B
$$

其中：

$$
C[i][j]=\sum_{k=0}^{7}A[i][k]\times B[k][j]
$$

設計使用 8×8 PE array，共 **64 個 PE**。每個 PE 負責一個輸出元素 `C[i][j]`，並在本地保存 32-bit accumulator。

資料流採：

* A：由左向右傳遞
* B：由上向下傳遞
* Partial Sum：留在 PE 本地，不在 PE 間傳遞

整個 operation 分為：

1. Start acceptance / PE clear
2. 22-cycle computation phase
3. 64-cycle output phase
4. 1-cycle done pulse

`start && !busy` 即接受新 operation。

特別規定：

> **在 `done=1` 的 cycle，只要 `busy=0` 且 `start=1`，即可直接接受下一個 operation，不需要額外 idle cycle。**

本架構目前已完成主要 ambiguity clarification，可進入 RTL implementation；但在 human 明確批准前仍不宣稱 FROZEN。

---

# 2. Requirements Interpretation

## 2.1 Explicit Requirements

本架構直接遵循以下明確需求：

| Item               | Requirement                      |
| ------------------ | -------------------------------- |
| Matrix size        | 8×8                              |
| Input type         | signed 8-bit two's complement    |
| Output type        | signed 32-bit accumulator result |
| Architecture       | Output-Stationary Systolic Array |
| PE count           | 64                               |
| Product width      | 16-bit signed                    |
| Accumulator width  | 32-bit signed                    |
| A dataflow         | left → right                     |
| B dataflow         | top → bottom                     |
| Partial sum        | local to each PE                 |
| Computation cycles | 22                               |
| Output cycles      | 64                               |
| Output order       | row-major                        |
| Reset              | asynchronous active-low          |
| PE clear           | synchronous                      |
| Saturation         | none                             |
| Exception flag     | none                             |
| Backpressure       | none                             |
| Clock domains      | single clock domain              |

## 2.2 Derived Requirements

由上述架構推導：

* PE `[i][j]` 必須計算 `C[i][j]`。
* PE `[i][j]` 的第 `k` 個 MAC 發生於：

$$
t=k+i+j
$$

* 最晚的 PE `[7][7]` 在：

$$
7+7+7=21
$$

完成最後一次 MAC。

* 因此 computation phase 為：

$$
t=0\ldots21
$$

共 22 cycles。

* Output phase 從 operation-relative cycle 22 開始。
* 64 個 output cycle 為：

$$
t=22\ldots85
$$

* final output 為 `C[7][7]`，發生於 `t=85`。
* `done` 發生於下一 cycle `t=86`。

## 2.3 Assumptions

目前未指定的 technology/library/PVT/frequency 不作為 architecture-level 固定條件。

Area 與 timing 的優化目標定義為：

> **Minimize Area × Timing**

實際數值必須由 synthesis / STA / physical implementation 得到。

---

# 3. Behavioral Model

## 3.1 Matrix Operation

對每個：

$$
0\le i,j<8
$$

計算：

$$
C[i][j]=
A[i][0]B[0][j]+
A[i][1]B[1][j]+\cdots+
A[i][7]B[7][j]
$$

每個 PE 只負責一個 `C[i][j]`。

---

## 3.2 Transaction Definition

一次 transaction：

```text
start accepted
    ↓
PE clear
    ↓
22-cycle systolic computation
    ↓
64-cycle result output
    ↓
done pulse
    ↓
ready for next transaction
```

Start acceptance condition：

$$
accept\_start=start\land\neg busy
$$

只要此條件成立，即接受新的 operation。

---

## 3.3 Start Acceptance

接受 `start` 的 clock edge：

* 接受新的 operation
* 所有 PE synchronous clear
* `busy` 進入 active 狀態
* 不執行 computation MAC
* 此 edge 不定義為 computation `t=0`

下一個 rising edge 開始：

$$
t=0
$$

---

## 3.4 Done / Back-to-Back Operation

Final output cycle：

```text
result_valid = 1
result_last  = 1
busy         = 1
done         = 0
```

下一個 cycle：

```text
result_valid = 0
result_last  = 0
busy         = 0
done         = 1
```

若此時：

```text
start = 1
```

則：

```text
start && !busy = 1
```

因此可以在 `done` cycle **直接接受下一個 operation**。

不要求額外 idle cycle。

該 acceptance edge 只負責：

* 接受新 operation
* clear PE
* 建立新的 busy state

新的 computation `t=0` 仍從下一個 rising edge 開始。

---

# 4. Interface Architecture

## 4.1 Clock / Reset

### Clock

單一 clock domain。

不使用：

* derived clock
* internally generated clock
* data-as-clock
* combinational clock gating

Clock enable 概念透過 `enable` 控制 datapath operation。

### Reset

`reset_n`：

* active-low
* asynchronous
* reset assertion asynchronous
* reset deassertion asynchronous
* architecture 不加入 reset synchronizer

Physical implementation 必須檢查：

* recovery
* removal
* reset distribution

若後續 flow 證明必須加入 reset synchronization，視為 architecture change，需重新 review。

---

# 5. Top-Level Interface Architecture

## 5.1 Control Signals

### `start`

Input。

有效條件：

> `start=1` 且 `busy=0`，維持一個 cycle。

接受條件：

$$
accept\_start=start\land\neg busy
$$

`busy=1` 時，`start` ignored。

---

### `busy`

Output。

表示目前 operation 正在進行。

`busy=1` 涵蓋：

* 22 computation cycles
* 64 output cycles
* 包含 final output cycle

Final output cycle：

```text
busy = 1
result_valid = 1
result_last = 1
```

下一 cycle：

```text
busy = 0
done = 1
```

---

### `done`

Output。

一個 cycle pulse。

發生在 final result output cycle 的下一個 cycle。

因此：

```text
result_last = 1
```

與：

```text
done = 1
```

不在同一 cycle。

---

## 5.2 Result Interface

### `result_valid`

在 output phase：

```text
result_valid = 1
```

持續 64 cycles。

其他時間：

```text
result_valid = 0
```

---

### `result_data`

32-bit signed result。

當：

```text
result_valid = 1
```

時有效。

當：

```text
result_valid = 0
```

時，`result_data` 為 undefined / don't-care。

Verification 不得在 `result_valid=0` 時檢查 `result_data`。

---

### `result_row`

表示目前輸出的 row。

範圍：

```text
0..7
```

只在：

```text
result_valid=1
```

時有效。

---

### `result_col`

表示目前輸出的 column。

範圍：

```text
0..7
```

只在：

```text
result_valid=1
```

時有效。

---

### `result_last`

只在最後一個 output cycle assert。

最後一筆：

```text
result_data = C[7][7]
result_row  = 7
result_col  = 7
result_last = 1
result_valid = 1
busy = 1
done = 0
```

---

# 6. Input Packing

## 6.1 A Input

Input bus 採 LSB-first channel packing。

Channel `i`：

```text
a_stream[i] = A[i][k]
```

對應：

```text
A channel i → bus [8*i+7 : 8*i]
```

因此：

```text
channel 0 → [7:0]
channel 1 → [15:8]
...
channel 7 → [63:56]
```

---

## 6.2 B Input

B input 同樣採 LSB-first channel packing。

```text
channel j → bus [8*j+7 : 8*j]
```

其中 channel `j` 對應 B matrix 的 column `j`。

---

# 7. Systolic Dataflow Architecture

## 7.1 PE Array

建立：

$$
8\times8
$$

PE array：

```text
PE[0][0] ... PE[0][7]
...
PE[7][0] ... PE[7][7]
```

每個 PE：

```text
PE[i][j] → C[i][j]
```

---

## 7.2 A Dataflow

A data：

```text
left → right
```

在每個 PE 延遲一個 cycle。

因此：

```text
PE[i][j]
```

收到的 A data 經過 `j` 個水平 propagation stage。

---

## 7.3 B Dataflow

B data：

```text
top → bottom
```

在每個 PE 延遲一個 cycle。

因此：

```text
PE[i][j]
```

收到的 B data 經過 `i` 個垂直 propagation stage。

---

## 7.4 Partial Sum

Partial sum：

```text
local to each PE
```

不得：

* 傳到下一個 PE
* 在 PE 間交換
* 經由 systolic network 傳遞

---

# 8. PE Architecture

每個 PE 至少包含以下 architectural state：

* `a_out`
* `b_out`
* `acc_out`

## 8.1 Reset

當：

```text
reset_n = 0
```

PE asynchronous clear：

```text
a_out  = 0
b_out  = 0
acc_out = 0
```

---

## 8.2 Clear

當：

```text
clear = 1
```

且 reset 未作用時：

```text
a_out  = 0
b_out  = 0
acc_out = 0
```

`clear` 是：

> **internal controller-generated signal**

不是 top-level input。

定義：

$$
clear=accept\_start
$$

因此 start acceptance 會對所有 PE 產生一次 synchronous clear。

---

## 8.3 Enable = 1

當：

```text
clear = 0
enable = 1
```

PE：

```text
a_out = a_in
b_out = b_in
acc_out = acc_out + signed(a_in) * signed(b_in)
```

其中 product 先形成 16-bit signed value，再 sign-extend 到 32-bit accumulator width。

---

## 8.4 Enable = 0

當：

```text
clear = 0
enable = 0
```

PE：

```text
a_out = 0
b_out = 0
acc_out = hold
```

因此 output phase：

* 不 forwarding
* 不 MAC
* accumulator 保持穩定

---

# 9. Computation Timing

## 9.1 Computation Cycle

定義 operation-relative computation cycle：

$$
t=0
$$

為 start acceptance 後的下一個 rising edge。

Computation：

$$
t=0\ldots21
$$

共：

$$
22
$$

cycles。

---

## 9.2 Systolic Timing Equation

PE `[i][j]` 的第 `k` 個 pair：

$$
(A[i][k],B[k][j])
$$

於：

$$
t=k+i+j
$$

抵達並進行 MAC。

其中：

$$
0\le i,j,k<8
$$

---

## 9.3 Final MAC

PE `[7][7]` 的：

```text
k = 7
```

最後一次 MAC：

$$
t=7+7+7=21
$$

因此：

```text
PE[7][7]
final MAC = t=21
```

---

# 10. Input Skewing

## 10.1 A Skew

對 computation cycle `t`：

$$
a\_stream[i]=
\begin{cases}
A[i][t-i], & 0\le t-i<8\\
0, & otherwise
\end{cases}
$$

---

## 10.2 B Skew

$$
b\_stream[j]=
\begin{cases}
B[t-j][j], & 0\le t-j<8\\
0, & otherwise
\end{cases}
$$

---

## 10.3 Wavefront

Non-zero wavefront 主要存在於：

$$
t=0\ldots14
$$

為確保 systolic pipeline 完整 drain：

$$
t=15\ldots21
$$

所有對應 out-of-range stream data 必須為 zero。

---

# 11. Enable Timing

`enable` 必須：

```text
t=0..21 → 1
```

共 22 cycles。

即使：

```text
t=21
```

已是最後一次 MAC，仍必須保持：

```text
enable = 1
```

因為 PE `[7][7]` 必須在該 cycle 完成：

$$
A[7][7]\times B[7][7]
$$

Output phase：

```text
enable = 0
```

---

# 12. Output Architecture

## 12.1 Output Source

`result_data` 直接由 PE accumulator 取得。

不增加：

* output latch
* output storage array
* output register stage

Output phase 中所有 PE：

```text
enable = 0
```

因此：

```text
acc_out = stable
```

---

## 12.2 Output Order

採 row-major：

```text
C[0][0]
C[0][1]
...
C[0][7]
C[1][0]
...
C[7][7]
```

---

## 12.3 Output Timeline

Computation：

```text
t = 0..21
```

Output：

```text
t = 22..85
```

共 64 cycles。

其中：

```text
t=22 → C[0][0]
t=23 → C[0][1]
...
t=85 → C[7][7]
```

---

# 13. Control Architecture

控制器至少具有以下 logical phases：

1. IDLE / Ready
2. COMPUTE
3. OUTPUT
4. DONE / completion boundary

實際 RTL FSM state naming 可由 RTL engineer 決定，但不得改變 observable behavior。

---

## 13.1 Start Acceptance

條件：

$$
start=1\land busy=0
$$

動作：

* accept operation
* clear all PE
* busy → 1
* initialize computation control
* next cycle begins computation `t=0`

---

## 13.2 Compute State

條件：

```text
busy=1
result_valid=0
```

持續 22 cycles。

counter：

```text
0 → 21
```

每 cycle：

* drive skewed A/B
* enable PE
* perform MAC
* advance computation counter

---

## 13.3 Output State

從 computation 完成後進入。

持續 64 cycles。

每 cycle：

* `result_valid=1`
* select corresponding PE accumulator
* output row/column
* advance output index

PE：

```text
enable=0
```

---

## 13.4 Final Output

最後一個 output：

```text
output index = 63
row = 7
col = 7
result_last = 1
result_valid = 1
busy = 1
done = 0
```

---

## 13.5 Done

下一 cycle：

```text
result_valid = 0
result_last = 0
busy = 0
done = 1
```

此 cycle 若：

```text
start = 1
```

則：

```text
accept_start = 1
```

可直接接受下一筆 operation。

因此：

> DONE 並不是強制性的 idle-only state。

---

# 14. Priority Definition

控制優先級：

## Priority 1 — Reset

```text
reset_n = 0
```

最高優先權。

所有 architecture-defined state / PE state 進入 reset state。

---

## Priority 2 — Start Acceptance

```text
start = 1
busy = 0
```

接受 operation。

這包括：

> `done=1` 的 cycle。

---

## Priority 3 — Computation

```text
busy = 1
result_valid = 0
```

執行 22-cycle computation。

---

## Priority 4 — Output

Output phase：

```text
result_valid = 1
```

持續 64 cycles。

---

# 15. Arithmetic Architecture

## 15.1 Input Representation

A、B：

* 8-bit
* signed
* two's complement

range：

$$
-128\ldots127
$$

---

## 15.2 Product

：

$$
P=A\times B
$$

product width：

$$
16\text{ bits}
$$

signed two's complement。

---

## 15.3 Accumulation

Product 必須 sign-extend 到 32 bits：

$$
P_{ext}=SignExtend_{32}(P)
$$

Accumulator：

$$
ACC_{next}=ACC+P_{ext}
$$

---

## 15.4 Overflow

不使用：

* saturation
* exception flag
* overflow flag

定義為 32-bit two's-complement arithmetic：

$$
ACC_{next}
=
(ACC+P_{ext})\bmod2^{32}
$$

因此超出 32-bit 表示範圍時保留低 32 bits。

---

# 16. Bit Width and Signedness

| Signal / Object | Width | Signed |
| --------------- | ----: | ------ |
| A element       |     8 | Yes    |
| B element       |     8 | Yes    |
| Product         |    16 | Yes    |
| Accumulator     |    32 | Yes    |
| result_data     |    32 | Yes    |
| result_row      |     3 | No     |
| result_col      |     3 | No     |
| ARRAY_SIZE      |     8 | N/A    |
| DATA_WIDTH      |     8 | N/A    |
| ACC_WIDTH       |    32 | N/A    |
| INDEX_WIDTH     |     3 | N/A    |

不得依賴 implicit signed conversion 完成 correctness-critical arithmetic。

---

# 17. Parameter Architecture

允許保留 parameter。

Required project configuration：

```text
ARRAY_SIZE = 8
DATA_WIDTH = 8
ACC_WIDTH  = 32
INDEX_WIDTH = 3
```

限制：

$$
ACC\_WIDTH\ge2\times DATA\_WIDTH
$$

本 project 只保證：

```text
8 × 8
8-bit input
32-bit accumulator
3-bit result index
```

其他 parameter configuration：

> **NOT VERIFIED / NOT SUPPORTED**

若修改 parameter，必須重新檢查：

* systolic timing
* skew
* drain cycles
* counter width
* output indexing
* accumulation range
* verification coverage

---

# 18. Memory Architecture

本設計不依賴大型 SRAM。

Matrix input data 透過 input stream / skewing 方式提供給 systolic array。

PE accumulator 為 local register state。

不定義額外：

* SRAM macro
* FIFO
* cache
* register file

作為 architecture requirement。

---

# 19. Clock Architecture

目前：

```text
Single clock domain
```

所有：

* controller
* PE array
* output control

使用同一 clock。

不需要 CDC。

不存在：

* asynchronous data crossing
* generated clock
* derived clock

---

# 20. Reset Architecture

## Reset

```text
reset_n
```

active-low asynchronous。

Reset 時：

* controller state reset
* control counters reset
* PE `a_out` reset
* PE `b_out` reset
* PE `acc_out` reset

---

## Reset Release

Reset deassertion 不要求 architecture-level clock alignment。

目前不加入 reset synchronizer。

Physical implementation 必須確認：

* recovery
* removal
* reset tree behavior

---

# 21. Candidate Architecture Evaluation

## Candidate A — Full 8×8 PE Array

### Characteristics

* 64 PE
* 每 PE 一個 multiplier
* local accumulator
* 完整 systolic dataflow
* 22-cycle computation

### Advantages

* 完全符合既有 architecture
* throughput 明確
* control relatively simple
* partial sums local
* no multiplier sharing
* no additional scheduling complexity

### Risks

* 64 multipliers
* PE routing
* output mux
* reset distribution
* area

---

## Candidate B — Shared / Time-Multiplexed Multipliers

優點：

* 可能降低 multiplier area

缺點：

* 改變 PE architecture
* 改變 computation scheduling
* 需要增加 time-multiplex control
* 可能改變 latency / throughput
* 不符合目前指定的 output-stationary 64-PE architecture

**結論：Rejected for current architecture.**

---

## Candidate C — Pipelined PE

優點：

* 可能改善 multiplier/adder critical path

缺點：

* 增加 latency
* 改變 systolic timing
* 需要重新定義 `t=k+i+j`
* 需要重新定義 output timing

**結論：Rejected for current architecture。**

---

# 22. Recommended Architecture

採用：

> **Candidate A — Full 8×8 Output-Stationary Systolic PE Array**

理由：

1. 直接符合 project specification。
2. 每個 output element 有固定 PE。
3. Partial sum 完全 local。
4. timing schedule 明確。
5. control architecture deterministic。
6. verification 可建立 cycle-accurate reference model。
7. 不需要額外 multiplier scheduling。
8. 不需要額外 pipeline latency。
9. 保持既有 22-cycle computation / 64-cycle output contract。

---

# 23. Performance Architecture

## Latency

從 start acceptance edge 開始：

### Start acceptance

1 cycle boundary。

### Computation

22 cycles。

### Output

64 cycles。

### Done

1 cycle。

因此 final result output 的 operation-relative timing：

$$
t=85
$$

`done`：

$$
t=86
$$

---

## Throughput

由於：

> `done` cycle 可以接受下一筆 start

在連續操作情況下，不需要額外 idle cycle。

但新的 operation 的第一個 computation cycle 仍為 acceptance 後下一個 rising edge。

因此 back-to-back operation 的控制邊界為：

```text
Operation N:
final output
    ↓
Operation N done + Operation N+1 start acceptance
    ↓
Operation N+1 t=0
```

---

# 24. Area Considerations

Architecture-level major area contributor：

1. 64 multipliers
2. 64 accumulators
3. PE forwarding registers
4. output selection logic
5. control / counters
6. reset distribution

預期最大的 architecture-level area contributor 為 PE datapath。

目前沒有實際 synthesis area evidence。

因此：

> **Area = UNKNOWN / NOT MEASURED**

不得宣稱 area optimal。

---

# 25. Timing Considerations

Potential timing-sensitive structures：

1. multiplier
2. accumulator adder
3. PE forwarding path
4. output mux
5. high-fanout reset
6. systolic interconnect

目前沒有：

* technology node
* standard-cell library
* PVT
* clock constraint
* synthesis report
* STA report

因此：

> **Timing = UNKNOWN / NOT MEASURED**

不得宣稱 timing clean 或 timing compliant。

---

# 26. Area × Timing Optimization Objective

Project 沒有指定 hard area limit，也沒有指定 hard frequency target。

因此 architecture-level optimization objective 定義為：

$$
\boxed{\text{Minimize Area}\times\text{Timing Cost}}
$$

實際 optimization 必須依：

* synthesis area
* critical path
* slack
* implementation result

判斷。

不可在沒有 tool evidence 的情況下宣稱某架構已達到最佳 Area × Timing。

---

# 27. Physical Design Considerations

主要 physical risks：

## 27.1 PE Array Placement

8×8 regular array 適合 physical regularity。

應盡量保持：

```text
PE[i][j]
```

在 physical 上接近其 systolic neighbors。

---

## 27.2 Horizontal A Routing

A data：

```text
left → right
```

需要規則化水平 routing。

---

## 27.3 Vertical B Routing

B data：

```text
top → bottom
```

需要規則化垂直 routing。

---

## 27.4 Reset Distribution

64 PE 共享 asynchronous reset。

reset fanout 與 reset tree 需要 physical implementation 處理。

---

## 27.5 Output Mux

64 個 accumulator 對應 output selection。

Output selection logic 可能形成 timing-sensitive path。

實際影響必須由 synthesis / STA 判定。

---

# 28. Verification Architecture

Verification 必須至少涵蓋：

## 28.1 Functional

* zero matrix
* identity matrix
* positive values
* negative values
* mixed sign
* random matrices
* maximum positive
* maximum negative
* boundary values

---

## 28.2 Arithmetic

驗證：

$$
signed(A)\times signed(B)
$$

以及：

$$
ACC_{next}=ACC+SignExtend(P)
$$

確認：

* sign extension
* 32-bit accumulation
* two's-complement overflow
* no saturation

---

## 28.3 Systolic Timing

驗證：

$$
t=k+i+j
$$

所有：

```text
i = 0..7
j = 0..7
k = 0..7
```

都必須符合預期 arrival timing。

---

## 28.4 Wavefront

驗證：

* t=0..14：合法 wavefront
* t=15..21：zero drain
* t=21：final PE MAC

---

## 28.5 Control

驗證：

* start while idle
* start while busy
* start during computation
* start during output
* start during done
* back-to-back operation
* final output
* done pulse
* busy transition
* result_valid transition

特別必須驗證：

```text
done=1
busy=0
start=1
```

可以接受新 operation。

---

## 28.6 Output

驗證：

```text
C[0][0] ... C[7][7]
```

row-major order。

確認：

* result_row
* result_col
* result_last
* result_valid

---

## 28.7 Reset

驗證：

* reset during idle
* reset before start
* reset during computation
* reset during output
* reset during done
* asynchronous assertion
* asynchronous deassertion

---

# 29. Architectural Invariants

以下 invariant 必須成立：

### INV-001

每個 PE 只負責：

$$
C[i][j]
$$

### INV-002

Partial sum 不離開其 PE。

### INV-003

Computation phase：

$$
22\text{ cycles}
$$

### INV-004

Output phase：

$$
64\text{ cycles}
$$

### INV-005

PE `[i][j]` 第 `k` 個 MAC：

$$
t=k+i+j
$$

### INV-006

Final MAC：

$$
PE[7][7], k=7, t=21
$$

### INV-007

Output order：

```text
row-major
```

### INV-008

`result_last=1` 僅出現在最後 output。

### INV-009

`done` 在 final output 後一 cycle pulse。

### INV-010

`busy=0` 時：

```text
start=1
```

必須可觸發 operation acceptance。

### INV-011

因此：

```text
done=1 && start=1
```

可以形成新的 operation acceptance。

### INV-012

`enable=0` 時：

```text
a_out=0
b_out=0
acc_out=hold
```

---

# 30. Corner Cases

## Case 1 — start while busy

忽略 start。

不得重新 clear PE。

---

## Case 2 — start during output

忽略 start。

因為：

```text
busy=1
```

---

## Case 3 — start during final output

忽略 start。

因為 final output cycle：

```text
busy=1
```

---

## Case 4 — start during done

接受。

因為：

```text
busy=0
```

---

## Case 5 — zero input

所有結果：

```text
C[i][j] = 0
```

---

## Case 6 — negative operands

必須依 signed two's-complement arithmetic 計算。

---

## Case 7 — maximum negative value

`-128` 必須以 signed 8-bit 正確處理。

---

## Case 8 — output phase

PE accumulators 不得改變。

---

## Case 9 — reset

reset 必須立即清除 architecture-defined PE state。

---

# 31. Traceability

| Requirement          | Architecture |
| -------------------- | ------------ |
| 8×8 matrix           | Section 7    |
| Output stationary    | Section 7    |
| A left→right         | Section 7.2  |
| B top→bottom         | Section 7.3  |
| Local partial sum    | Section 7.4  |
| 8-bit signed         | Section 15   |
| 16-bit product       | Section 15   |
| 32-bit accumulator   | Section 15   |
| 22-cycle computation | Section 9    |
| Zero drain           | Section 10   |
| 64-cycle output      | Section 12   |
| Row-major output     | Section 12   |
| Async reset          | Section 20   |
| Synchronous clear    | Section 8    |
| Start acceptance     | Section 13   |
| Done timing          | Section 13.5 |
| Done-cycle start     | Section 13.5 |
| No saturation        | Section 15.4 |
| Parameter policy     | Section 17   |
| Verification         | Section 28   |

---

# 32. Architecture Risks

| ID       | Risk                                         | Severity | Status                     |
| -------- | -------------------------------------------- | -------- | -------------------------- |
| RISK-001 | 64 multipliers may dominate area             | Medium   | Open / measured later      |
| RISK-002 | Multiplier + accumulator may dominate timing | Medium   | Open / measured later      |
| RISK-003 | Output selection mux timing                  | Medium   | Open / STA required        |
| RISK-004 | PE interconnect congestion                   | Medium   | Open / P&R required        |
| RISK-005 | Async reset recovery/removal                 | Medium   | Open / STA required        |
| RISK-006 | Parameter configurations other than baseline | Low      | Unsupported / not verified |
| RISK-007 | No hard timing constraint currently defined  | Low      | Architecture assumption    |

---

# 33. Evidence Classification

目前 evidence 狀態：

| Item                    | Classification          |
| ----------------------- | ----------------------- |
| Functional architecture | Reasoned                |
| Cycle schedule          | Reasoned                |
| Arithmetic behavior     | Reasoned                |
| Systolic timing         | Reasoned                |
| Interface contract      | Spec-defined / Reasoned |
| RTL implementation      | Not yet implemented     |
| Lint                    | NOT VERIFIED            |
| CDC                     | NOT VERIFIED            |
| Synthesis               | NOT VERIFIED            |
| STA                     | NOT VERIFIED            |
| Area                    | NOT MEASURED            |
| Power                   | NOT MEASURED            |
| P&R                     | NOT VERIFIED            |
| DRC                     | NOT VERIFIED            |
| LVS                     | NOT VERIFIED            |

任何 downstream implementation claim 必須以實際 tool evidence 更新。

---

# 34. RTL Handoff Contract

RTL engineer 必須遵守以下 frozen-intent contract，即使本 Architecture 尚未正式 freeze，也不得自行改寫其已確認行為。

## Must Preserve

* 8×8 PE topology
* Output-Stationary dataflow
* local accumulator
* A left→right
* B top→bottom
* signed arithmetic
* 8-bit input
* 16-bit product
* 32-bit accumulator
* modulo 32-bit accumulation
* async active-low reset
* synchronous PE clear
* 22-cycle computation
* 64-cycle output
* row-major result
* `t=k+i+j`
* zero drain
* direct accumulator output
* `result_last` timing
* `done` timing
* `busy` timing
* start acceptance while `busy=0`
* **start acceptance during `done` cycle**

## Must Not Add

* extra pipeline stage
* multiplier sharing
* additional output register
* reset synchronizer
* additional protocol signal
* ready/backpressure mechanism
* extra idle cycle after done
* saturation
* exception flag
* alternate output ordering

除非重新進行 architecture review 並取得 human approval。

---

# 35. Open Issues

目前沒有 Blocking architecture issue。

仍存在以下 implementation-level unknowns：

### OI-001 — Technology / Library

**Missing Information:**
Target technology / standard-cell library / PVT 未指定。

**Why It Matters:**
影響實際 area、timing、power。

**Current Treatment:**
不作為 architecture blocker。

---

### OI-002 — Clock Constraint

**Missing Information:**
尚未提供實際 clock period / SDC constraint。

**Why It Matters:**
無法判定 timing slack。

**Current Treatment:**
Area × Timing 為 optimization objective；實際 constraint 由 implementation environment 決定。

---

### OI-003 — Physical Constraint

**Missing Information:**
尚未提供 floorplan、placement、utilization、routing constraint。

**Why It Matters:**
影響 physical timing/congestion。

**Current Treatment:**
P&R 階段處理。

---

# 36. Freeze Status

## Current Status

**PROPOSED**

Architecture 已：

* 完成 requirements interpretation
* 完成 behavioral model
* 完成 interface definition
* 完成 cycle-level definition
* 完成 control definition
* 完成 datapath definition
* 完成 arithmetic definition
* 完成 bit-width/signedness definition
* 完成 candidate architecture comparison
* 完成 verification architecture
* 完成 physical risk review
* 完成主要 ambiguity clarification

## RTL Readiness

**READY FOR RTL**

## Freeze

**NOT FROZEN**

正式 FROZEN 必須由 project owner / human 明確批准。

---

# 37. Final Architecture Statement

本設計採用：

> **8×8 Output-Stationary Systolic Array**

每個 PE 對應一個輸出：

$$
PE[i][j]\rightarrow C[i][j]
$$

A 由左向右傳遞，B 由上向下傳遞，partial sum 留在 PE 本地。

每個 PE 執行：

$$
ACC_{next}
=
ACC+
SignExtend_{32}
\left(
signed(A)\times signed(B)
\right)
$$

computation：

$$
t=0\ldots21
$$

output：

$$
t=22\ldots85
$$

final output：

$$
C[7][7]\quad at\quad t=85
$$

done：

$$
t=86
$$

並且：

> **任何 `busy=0` 的 cycle，只要 `start=1`，即可接受新的 operation；因此 `done=1` 的 cycle 也可以同時接受下一個 `start`，不需要額外 idle cycle。**

此 Architecture Specification v1.0 為 RTL implementation 的正式架構基準，但在 human explicit approval 前維持 **PROPOSED / NOT FROZEN** 狀態。
