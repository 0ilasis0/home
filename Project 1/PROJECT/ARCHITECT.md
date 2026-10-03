Digital Circuit Design and Analysis 115-1 Project I

Triangle Rendering Engine

Architecture Specification — Revision A1

Document Status: UNDER REVIEW
Revision: A1
Document Type: Architecture Specification / Freeze Candidate
Architecture Freeze: PENDING PROJECT-OWNER APPROVAL
RTL Status: NOT STARTED

---

1. Architecture Purpose

本 Architecture 定義 Triangle Rendering Engine 的硬體行為、資料路徑、控制流程、儲存方式、幾何計算方式、clock-level operation、output protocol，以及 verification boundary。

本 Architecture 的目的，是將 Project Specification 轉換為：

1. 可實作的 synchronous digital architecture
2. 可由獨立 Reference Model 驗證的 deterministic behavior
3. 可直接進入 RTL implementation 的明確 datapath/control specification
4. 可供後續 synthesis、timing 與 physical implementation 使用的 architecture baseline

本文件不定義 RTL coding style，也不以 Final Testbench implementation 細節反向決定 architecture。

---

2. Source-of-Truth Hierarchy

本專案採用以下 hierarchy：

Project Specification
        ↓
Architecture Specification
        ↓
Interface Specification
        ↓
Clock / Reset Specification
        ↓
Verification Specification
        ↓
RTL
        ↓
EDA Tool Reports

若後續發現不同文件互相衝突：

- 不得自行修改 Frozen Architecture。
- 必須先確認衝突來源。
- 若屬 Specification ambiguity，必須由 project owner 釐清。
- Verification artifact 不得反向修改 Architecture。
- RTL 不得自行改變 Architecture 定義。

---

3. Requirement Classification

3.1 Explicit Requirements

以下為 Project Specification 已明確要求的條件：

Triangle input

輸入三個頂點：

[
P_1=(x_1,y_1)
]

[
P_2=(x_2,y_2)
]

[
P_3=(x_3,y_3)
]

且：

[
x_1=x_3
]

[
y_1<y_2<y_3
]

[
x_2\ne x_1
]

Coordinate width

所有 coordinate data 為 3-bit unsigned：

[
0\le x,y\le7
]

Input protocol

輸入三個 cycle：

C1 : nt=1 → P1
C2 : nt=0 → P2
C3 : nt=0 → P3

"nt" 僅允許在 "busy=0" 時 assertion。

Output ordering

所有 triangle plane covered points 必須依：

1. y 由小到大
2. 同一 y 中 x 由小到大

輸出。

即：

y = y1 → y3
    x = x_left → x_right

Clock

Circuit 為 positive-edge triggered synchronous design。

Reset

"reset" 為 active-high asynchronous reset。

---

4. Derived Architecture Requirements

由上述 requirements 推導出：

1. Triangle projection 在 x direction 為連續區域。
2. 對每一個 x，triangle interior 可表示成一個連續 y interval。
3. 因此不需要逐 pixel 執行完整 point-in-triangle geometry test。
4. 可以將 triangle 表示為：

[
Column(x)=[ylow(x),yup(x)]
]

5. Output scanner 只需查詢：

[
ylow(x)\le y\le yup(x)
]

即可決定 "po"。
6. 因為 coordinate domain 僅 0~7，因此使用 8-entry x-indexed column storage。

---

5. High-Level Architecture

Architecture 採用：

                 ┌─────────────────────┐
Input P1,P2,P3 →│ Input Capture       │
                 └──────────┬──────────┘
                            │
                            ▼
                 ┌─────────────────────┐
                 │ Geometry Parameters │
                 │ dx, dy12, dy32, s   │
                 └──────────┬──────────┘
                            │
                            ▼
                 ┌─────────────────────┐
                 │ Column Storage      │
                 │ ylow[0:7]           │
                 │ yup [0:7]           │
                 └──────────┬──────────┘
                            │
                 ┌──────────┴──────────┐
                 │                     │
                 ▼                     ▼
          LOWER Tracer            UPPER Tracer
                 │                     │
                 └──────────┬──────────┘
                            │
                            ▼
                 ┌─────────────────────┐
                 │ Output Scanner      │
                 │ y ascending        │
                 │ x ascending        │
                 └──────────┬──────────┘
                            │
                            ▼
                    po / xo / yo

Architecture 採用 shared tracer datapath，LOWER 與 UPPER 不使用兩套獨立 tracer。

---

6. Interface Architecture

Top-level interface：

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

Inputs

Signal| Width| Type| Meaning
"clk"| 1| clock| positive-edge clock
"reset"| 1| control| active-high asynchronous reset
"nt"| 1| control| new triangle input indicator
"xi"| 3| unsigned| input x
"yi"| 3| unsigned| input y

Outputs

Signal| Width| Type| Meaning
"busy"| 1| control| engine busy status
"po"| 1| control| output point valid
"xo"| 3| unsigned| output x
"yo"| 3| unsigned| output y

"po", "xo", "yo" are registered outputs.

---

7. Input Capture Architecture

Three input points are captured sequentially.

Cycle C1

nt = 1
xi = x1
yi = y1

Capture:

x1
y1

Transition:

IDLE → CAPTURE_P2

Cycle C2

nt = 0
xi = x2
yi = y2

Capture:

x2
y2

Transition:

CAPTURE_P2 → CAPTURE_P3

Cycle C3

nt = 0
xi = x3
yi = y3

Capture:

x3
y3

完成 input capture 後進入:

INIT_COLUMN

---

8. Geometry Parameters

P3 capture 完成後建立：

[
dx=x_2-x_1
]

[
dy_{12}=y_2-y_1
]

[
dy_{32}=y_2-y_3
]

以及 direction：

[
s=sign(x_2-x_1)
]

因此：

x2 > x1 → s = +1
x2 < x1 → s = -1

由 specification：

[
dy_{12}>0
]

[
dy_{32}<0
]

---

9. Datapath Width and Signedness

Coordinates

x1, x2, x3 : unsigned 3-bit
y1, y2, y3 : unsigned 3-bit

range：

[
0\sim7
]

dx

signed 4-bit

range：

[
-7\sim7
]

dy12

signed 4-bit

range：

[
1\sim7
]

dy32

signed 4-bit

range：

[
-7\sim-1
]

Edge accumulator

"E_trace" 採：

signed 9-bit

作為 architecture-level conservative width。

所有 RTL arithmetic 必須明確處理：

- signedness
- sign extension
- expression width
- comparison width

不得依賴 Verilog implicit sizing 產生正確結果。

---

10. Column Storage Architecture

採用 8-entry x-indexed storage。

ylow[0:7]
yup [0:7]

每個 x 對應一個 column：

[
Column(x)=
\left[
ylow[x],yup[x]
\right]
]

因 x domain 為 0~7，因此不需要 "valid[0:7]"。

---

11. Endpoint Initialization

P3 capture 後：

ylow[x1] = y1
yup [x1] = y3

以及：

ylow[x2] = y2
yup [x2] = y2

因此兩個 endpoint columns 已經存在於 common column storage。

不額外建立 endpoint-specific output storage。

---

12. x Projection

定義：

[
x_{left}=\min(x_1,x_2)
]

[
x_{right}=\max(x_1,x_2)
]

所有 intermediate columns 位於：

[
x_{left}<x<x_{right}
]

Output scanner 的 x range：

[
x_{left}\rightarrow x_{right}
]

---

13. "|dx| = 1" Shortcut

若：

[
|dx|=1
]

則不存在 intermediate x columns。

兩個 endpoint columns 已足以描述整個 triangle。

因此：

INIT_COLUMN → OUTPUT_SCAN

不執行：

- LOWER_INIT
- LOWER_TRACE
- LOWER_CLAMP
- UPPER_INIT
- UPPER_TRACE
- UPPER_CLAMP

---

14. Edge Function Architecture

Architecture 將 LOWER 與 UPPER edge function normalize 成相同的 inside comparison：

[
\boxed{sE\ge0}
]

其中：

[
s=sign(x_2-x_1)
]

這樣不論 triangle 向左或向右，LOWER / UPPER 都使用相同 comparator。

---

15. LOWER Edge Function

LOWER edge：

[
E_L=
dx(y-y_1)

(x-x_1)dy_{12}
]

Inside condition：

[
\boxed{sE_L\ge0}
]

---

16. LOWER Incremental Calculation

當 x 往 direction "s" 移動：

[
x\leftarrow x+s
]

則：

[
\boxed{
E_L\leftarrow E_L-sdy_{12}
}
]

當 y 往上移動：

[
y\leftarrow y+1
]

則：

[
\boxed{
E_L\leftarrow E_L+dx
}
]

因此不需要重新計算完整 edge equation。

---

17. LOWER Initialization

第一個 intermediate candidate：

[
x_{trace}=x_1+s
]

[
y_{trace}=y_1
]

[
\boxed{
E_{trace}=-sdy_{12}
}
]

"LOWER_INIT" 只建立 candidate，不在該 state 完成 candidate evaluation。

下一 cycle 進入 "LOWER_TRACE"。

---

18. LOWER Trace Behavior

定義：

(x_trace, y_trace, E_trace)

永遠代表目前 candidate。

若：

[
sE_{trace}\ge0
]

則 candidate 為 inside。

執行：

ylow[x_trace] = y_trace

之後：

- 若下一個 x 是 x2 → LOWER 完成。
- 否則：
  [
  x_{trace}\leftarrow x_{trace}+s
  ]
    E_{trace}\leftarrow E_{trace}-sdy_{12}
  

若：

[
sE_{trace}<0
]

則 candidate outside。

執行：

[
y_{trace}\leftarrow y_{trace}+1
]

[
E_{trace}\leftarrow E_{trace}+dx
]

x 保持不變。

---

19. LOWER Y2 Clamp

當 LOWER candidate outside 且下一個 y 已經到達：

[
y_2
]

則目前 x column 的 lower boundary 必定為：

[
ylow[x]=y_2
]

因此：

ylow[x_trace] = y2

之後進入：

LOWER_CLAMP

在 "LOWER_CLAMP"：

x_trace += s

每個 intermediate x 直接：

ylow[x_trace] = y2

直到下一個 x 為 x2。

不再進行 edge evaluation。

---

20. UPPER Edge Function

UPPER edge：

[
E_U=
(x-x_3)dy_{32}

dx(y-y_3)
]

Inside condition：

[
\boxed{sE_U\ge0}
]

---

21. UPPER Incremental Calculation

當 x 往 direction "s" 移動：

[
x\leftarrow x+s
]

則：

[
\boxed{
E_U\leftarrow E_U+sdy_{32}
}
]

當 y 往下：

[
y\leftarrow y-1
]

則：

[
\boxed{
E_U\leftarrow E_U+dx
}
]

---

22. UPPER Initialization

第一個 intermediate candidate：

[
x_{trace}=x_1+s
]

[
y_{trace}=y_3
]

[
\boxed{
E_{trace}=sdy_{32}
}
]

"UPPER_INIT" 只建立 candidate。

下一 cycle 進入 "UPPER_TRACE"。

---

23. UPPER Trace Behavior

若：

[
sE_{trace}\ge0
]

則：

yup[x_trace] = y_trace

若下一個 x 為 x2：

UPPER → OUTPUT_SCAN

否則：

[
x_{trace}\leftarrow x_{trace}+s
]

[
E_{trace}\leftarrow E_{trace}+sdy_{32}
]

若：

[
sE_{trace}<0
]

則：

[
y_{trace}\leftarrow y_{trace}-1
]

[
E_{trace}\leftarrow E_{trace}+dx
]

x 保持不變。

---

24. UPPER Y2 Clamp

若 UPPER candidate outside，且下一個 y 為：

[
y_2
]

則：

yup[x_trace] = y2

之後進入：

UPPER_CLAMP

所有剩餘 intermediate x：

yup[x] = y2

直到 x2。

之後：

UPPER_CLAMP → OUTPUT_SCAN

---

25. Shared Tracer Datapath

LOWER 與 UPPER 共用：

x_trace
y_trace
E_trace
trace_mode

"trace_mode" 用於表示目前 edge operation：

LOWER
UPPER

不建立兩套完整 tracer datapath。

---

26. FSM Architecture

Architecture FSM：

IDLE
  ↓
CAPTURE_P2
  ↓
CAPTURE_P3
  ↓
INIT_COLUMN
  ↓
 ┌───────────────┐
 │ |dx| == 1 ?   │
 └───────┬───────┘
      yes│       │no
         │       │
         │       ▼
         │   LOWER_INIT
         │       ↓
         │   LOWER_TRACE
         │       ↓
         │   LOWER_CLAMP
         │       ↓
         │   UPPER_INIT
         │       ↓
         │   UPPER_TRACE
         │       ↓
         │   UPPER_CLAMP
         │       │
         └───────┘
             ↓
        OUTPUT_SCAN
             ↓
          RELEASE
             ↓
            IDLE

"LOWER_CLAMP" / "UPPER_CLAMP" 僅在需要時進入。

---

27. FSM State Definitions

IDLE

功能：

- 等待新 triangle。
- "busy=0"。
- 等待 "nt=1"。

---

CAPTURE_P2

功能：

- capture P2。

---

CAPTURE_P3

功能：

- capture P3。
- 完成 triangle geometry input capture。

---

INIT_COLUMN

功能：

- 初始化 endpoint columns。
- 計算 "dx"、"dy12"、"dy32"、"s"。
- 建立 "x_left/x_right"。
- 判斷 "|dx|=1" shortcut。

---

LOWER_INIT

功能：

建立：

[
(x_1+s,y_1,-sdy_{12})
]

不進行 candidate evaluation。

---

LOWER_TRACE

功能：

- evaluate lower edge。
- 更新 "ylow"。
- x/y/E incremental movement。
- 判斷是否進 Y2 Clamp。
- 判斷是否完成 LOWER。

---

LOWER_CLAMP

功能：

直接將剩餘 intermediate columns：

[
ylow[x]=y_2
]

---

UPPER_INIT

功能：

建立：

[
(x_1+s,y_3,sdy_{32})
]

不進行 candidate evaluation。

---

UPPER_TRACE

功能：

- evaluate upper edge。
- 更新 "yup"。
- x/y/E incremental movement。
- 判斷是否進 Y2 Clamp。
- 判斷是否完成 UPPER。

---

UPPER_CLAMP

功能：

直接將剩餘 intermediate columns：

[
yup[x]=y_2
]

完成後進入 OUTPUT_SCAN。

---

OUTPUT_SCAN

功能：

依：

y ascending
x ascending

逐一掃描 projection。

對 candidate "(x,y)"：

[
inside =
(ylow[x]\le y)\land(y\le yup[x])
]

若 inside：

po = 1
xo = x
yo = y

若 outside：

po = 0

仍繼續 scanner。

---

RELEASE

功能：

- 保持 "busy=1"
- "po=0"
- 不再產生 valid point
- 準備返回 IDLE

---

28. Output Scanner Architecture

Scanner registers：

x_scan
y_scan

初始化：

[
x_{scan}=x_{left}
]

[
y_{scan}=y_1
]

每 cycle 掃一個 candidate。

---

29. Output Scan Ordering

掃描順序：

for y = y1 → y3
    for x = x_left → x_right

因此：

[
(y_1,x_{left})
]

之後：

[
(y_1,x_{left}+1)
]

直到：

[
(y_1,x_{right})
]

再：

[
(y_1+1,x_{left})
]

如此持續直到：

[
(y_3,x_{right})
]

---

30. Output Timing

"po", "xo", "yo" 為 registered outputs。

對目前 scan candidate：

candidate
    ↓
rising edge
    ↓
register po/xo/yo
    ↓
clock-to-Q
    ↓
output valid for that cycle

因此第一個 OUTPUT_SCAN candidate：

«進入 "OUTPUT_SCAN" 時只初始化 scan candidate；下一個 rising edge 後，第一個 registered output 才有效。»

---

31. Output Valid Condition

若：

[
ylow[x]\le y\le yup[x]
]

則：

po = 1

否則：

po = 0

"po=0" 的 candidate 不會停止 scanner。

---

32. Busy Protocol

在 triangle processing period：

busy = 1

"busy=1" 時禁止新的 "nt" transaction。

最後一個 valid point：

po   = 1
busy = 1

下一 cycle：

po   = 0
busy = 1

此 cycle 為 "RELEASE"。

再下一 cycle：

po   = 0
busy = 0

回到 "IDLE"。

---

33. Last Output Timing Contract

定義最後一個 output point cycle 為 N：

Cycle| "busy"| "po"| Meaning
N| 1| 1| last valid point
N+1| 1| 0| RELEASE
N+2| 0| 0| IDLE

此 timing 為 Architecture contract。

---

34. Reset Architecture

"reset" 為 active-high asynchronous reset。

Reset assertion 時：

- FSM → "IDLE"
- "busy" → 0
- "po" → 0
- output registers → architecture-defined reset value
- tracer registers → reset state
- scanner registers → reset state
- control registers → reset state

Reset release 後 circuit 由 "IDLE" 開始正常 synchronous operation。

---

35. Latency Architecture

Input capture 固定需要：

[
3\ cycles
]

"|dx|=1" 時：

不需要 tracer

因此 latency 為：

input capture
+
INIT_COLUMN
+
output scan preparation
+
output scan
+
RELEASE / return-to-IDLE

"|dx|>1" 時，額外 latency 來自：

LOWER_INIT
LOWER_TRACE
LOWER_CLAMP（若需要）
UPPER_INIT
UPPER_TRACE
UPPER_CLAMP（若需要）

因此總 latency 不是固定常數，而是由 triangle geometry 決定。

---

36. Output Scan Count

projection width：

[
N_x=|x_2-x_1|+1
]

projection height：

[
N_y=y_3-y_1+1
]

因此 candidate scan 數：

[
\boxed{
N_{scan}

(|x_2-x_1|+1)(y_3-y_1+1)
}
]

注意：

«"N_scan" 是 candidate 數，不是 triangle interior point 數。»

即使 candidate 為 outside：

po = 0

仍消耗一個 scan cycle。

---

37. Memory Architecture Decision

採用：

8 × ylow
8 × yup

而不是：

endpoint registers
+
intermediate registers

理由：

1. x domain 固定為 0~7。
2. projection 為 continuous x range。
3. endpoint 也自然存在於 common storage。
4. 不需要 "valid" bit。
5. scanner 可以直接以 x 作為 storage index。
6. 避免 endpoint/intermediate 特殊 mux。
7. 控制邏輯較簡單。
8. architecture 更容易驗證。

此 decision：

APPROVED

---

38. Algorithmic Correctness Evidence

已針對所有合法 triangle combinations 做 exhaustive verification。

合法輸入數：

[
8\times {8\choose3}\times7

3136
]

目前 geometric / tracer reference verification：

[
\boxed{3136/3136\ PASS}
]

涵蓋：

- "x2 > x1"
- "x2 < x1"
- "|dx|=1"
- "|dx|>1"
- LOWER edge
- UPPER edge
- inclusive boundary
- Y2 Clamp
- x2 termination
- endpoint columns
- incremental edge accumulator
- output scan ordering

---

39. Important Correctness Property

Y2 row 必須完整存在。

對：

[
y=y_2
]

triangle interior 包含：

[
x_{left}\le x\le x_{right}
]

因此：

[
ylow[x]\le y_2
]

且：

[
y_2\le yup[x]
]

對 projection 內所有 x 成立。

Y2 Clamp 正是用來保證此 property。

---

40. Edge Normalization Property

LOWER：

[
E_L=
dx(y-y_1)-(x-x_1)dy_{12}
]

UPPER：

[
E_U=
(x-x_3)dy_{32}-dx(y-y_3)
]

兩者均使用：

[
\boxed{sE\ge0}
]

因此 RTL 不需要為：

x2 > x1

與：

x2 < x1

建立兩套不同的 comparator architecture。

---

41. Corner Cases

Architecture 必須處理：

Case 1

[
|dx|=1
]

→ bypass tracer。

Case 2

[
x_2>x_1
]

→

[
s=+1
]

Case 3

[
x_2<x_1
]

→

[
s=-1
]

Case 4

LOWER edge 在某 x 需要多個 y step。

→ "x_trace" 保持不變，"y_trace" 遞增。

Case 5

UPPER edge 在某 x 需要多個 y step。

→ "x_trace" 保持不變，"y_trace" 遞減。

Case 6

LOWER 到達 y2。

→ 使用 LOWER_CLAMP。

Case 7

UPPER 到達 y2。

→ 使用 UPPER_CLAMP。

Case 8

candidate 已位於 x2。

→ 不額外產生 x2 tracer candidate，直接進下一 stage。

---

42. x2 Termination Priority

x2 endpoint 已於 INIT_COLUMN 建立：

ylow[x2] = y2
yup [x2] = y2

因此 tracer 不需要再對 x2 執行 edge search。

當目前 intermediate column 完成，且：

[
x_{trace}+s=x_2
]

直接完成該 edge stage。

此 rule 優先於產生新的 x2 candidate。

---

43. Architecture Invariants

以下 invariants 必須在 RTL verification 中成立。

Column invariant

對每個有效 projection x：

[
ylow[x]\le yup[x]
]

Endpoint invariant

[
ylow[x_1]=y_1
]

[
yup[x_1]=y_3
]

[
ylow[x_2]=y_2
]

[
yup[x_2]=y_2
]

Y2 invariant

對所有：

[
x_{left}\le x\le x_{right}
]

有：

[
ylow[x]\le y_2\le yup[x]
]

Scanner invariant

scanner 永遠按照：

y non-decreasing
x non-decreasing within same y

Busy invariant

"nt" transaction 不得在 "busy=1" 時接受。

Output invariant

只有：

[
ylow[x_o]\le y_o\le yup[x_o]
]

時：

po = 1

---

44. Verification Strategy

Verification 必須分層。

Level 1 — Mathematical / Geometry Reference Model

比較：

independent geometric model
vs
column/tracer model

目前：

[
3136/3136 PASS
]

---

Level 2 — Cycle-Accurate Architecture Model

驗證：

- FSM sequence
- tracer timing
- column writes
- scanner timing
- "po"
- "xo"
- "yo"
- "busy"

狀態：

IN PROGRESS / NOT YET FINALIZED AS SIGNOFF EVIDENCE

---

Level 3 — RTL Simulation

Reference Model：

Architecture-derived expected behavior

對 RTL：

RTL outputs

進行 cycle-by-cycle comparison。

---

Level 4 — Final Verification Testbench

Final TB 為：

FROZEN VERIFICATION ARTIFACT

其作用為驗證 RTL。

若 Final TB 與 Architecture 有衝突：

STOP
→ classify discrepancy
→ do not silently modify Architecture

---

45. Verification Independence

Reference Model 不得直接複製 RTL implementation。

例如：

不得：

RTL code
    ↓
copy algorithm
    ↓
reference model

應：

Specification
      ↓
Mathematical model
      ↓
Reference model

如此才能避免相同 RTL bug 同時存在於 DUT 與 checker。

---

46. RTL Implementation Constraints

RTL implementation 不得自行改變：

- FSM architecture
- column storage architecture
- tracer sharing
- edge definitions
- Y2 Clamp behavior
- x2 termination
- output ordering
- clocking model
- reset strategy
- output timing
- protocol
- latency behavior

若 RTL implementation 遇到無法實現的問題：

«必須回報 Architecture issue，而不是自行修改 Architecture。»

---

47. Physical / Implementation Considerations

目前 architecture 為小型 finite-domain digital design。

主要 datapath：

signed arithmetic
+
small register arrays
+
FSM
+
comparators

主要 timing-sensitive paths 預期來自：

1. edge accumulator arithmetic
2. signed comparison
3. column lookup / comparison
4. FSM decision logic

目前尚未進行：

- synthesis
- STA
- area estimation
- power analysis
- placement
- routing

因此不能提前宣稱 timing/area/power feasibility。

---

48. Non-Goals

本 Architecture 不包含：

- pipeline redesign
- parallel tracer
- RAM macro inference decision
- multi-pixel-per-cycle output
- CDC architecture
- dynamic triangle input while busy
- speculative output
- point-by-point full barycentric computation

這些均不屬於目前 A1 architecture。

---

49. Architecture Decisions
| Decision                           | Status      |                 |       |                                                                                                                                 |              |
| ---------------------------------- | ----------- | --------------- | ----- | ------------------------------------------------------------------------------------------------------------------------------- | ------------ |
| Column representation              | APPROVED    |                 |       |                                                                                                                                 |              |
| 8-entry x-indexed storage          | APPROVED    |                 |       |                                                                                                                                 |              |
| No valid bit                       | APPROVED    |                 |       |                                                                                                                                 |              |
| Endpoint in common storage         | APPROVED    |                 |       |                                                                                                                                 |              |
| Shared tracer                      | APPROVED    |                 |       |                                                                                                                                 |              |
| LOWER / UPPER normalized `sE >= 0` | APPROVED    |                 |       |                                                                                                                                 |              |
| Y2 Clamp                           | APPROVED    |                 |       |                                                                                                                                 |              |
| x2 termination priority            | APPROVED    |                 |       |                                                                                                                                 |              |
| **`                                | dx          |= 1`shortcut：當`| x2-x1 | = 1` 時，不存在 intermediate column，INIT_COLUMN 完成 endpoint column initialization 後直接進入 OUTPUT_SCAN，不執行 LOWER/UPPER tracer 與 Clamp** | **APPROVED** |
| Registered `po/xo/yo`              | APPROVED    |                 |       |                                                                                                                                 |              |
| RELEASE cycle                      | APPROVED    |                 |       |                                                                                                                                 |              |
| Active-high asynchronous reset     | APPROVED    |                 |       |                                                                                                                                 |              |
| 9-bit signed E accumulator         | APPROVED    |                 |       |                                                                                                                                 |              |
| Architecture Freeze                | FROZEN |                 |       |                                                                                                                                 |              |

---

50. Current Architecture Status

Architecture Revision

A1

Status

UNDER REVIEW

Functional Algorithm

DEFINED / VERIFIED

Exhaustive Geometry Verification

[
\boxed{3136/3136\ PASS}
]

Cycle-Level Architecture

DEFINED

RTL

NOT STARTED

Synthesis

NOT STARTED

Timing

NOT VERIFIED

Physical Implementation

NOT STARTED

---

51. Open Issues Before Freeze

目前沒有需要重新設計核心演算法的已知 issue。

Freeze 前仍應確認以下 documentation/evidence：

O1 — Exact cycle-accurate reference model

需將 A1 所定義的 state transition 與 output register timing 完整整理成獨立 reference model。

Status: OPEN

O2 — Exact latency characterization

需由 reference model 統計所有 3136 個合法 triangle 的 latency range，並確認 maximum / minimum。

Status: OPEN

O3 — Final TB independent comparison

Final TB 應在 Architecture 定義完成後作為獨立 verification artifact 使用。

其結果不得反向修改 A1，除非發現真正的 Specification ambiguity。

Status: OPEN

O4 — RTL implementation

尚未開始。

Status: NOT STARTED

---

52. Freeze Criteria

A1 Architecture 在以下條件全部滿足後，才適合 Freeze：

1. Specification requirements 已映射。
2. Interface 已定義。
3. Clock / Reset 已定義。
4. FSM 已定義。
5. Datapath 已定義。
6. Bit width / signedness 已定義。
7. Column storage 已決定。
8. LOWER / UPPER algorithm 已定義。
9. Y2 Clamp 已定義。
10. Output timing 已定義。
11. Busy protocol 已定義。
12. Corner cases 已定義。
13. Architecture-derived reference model 完成。
14. Exhaustive algorithm verification 完成。
15. Open issues 不包含未解決的 architecture ambiguity。
16. Project owner 明確批准 Freeze。

---

53. Freeze Decision

Current decision:

Architecture Revision A1
        ↓
UNDER REVIEW
        ↓
NOT FROZEN

本文件目前為：

«Freeze Candidate»

而非已 Frozen Architecture。

只有 Project Owner 明確批准後，狀態才可變更為：

Architecture Revision A1
        ↓
FROZEN

Freeze 後，任何架構修改都必須建立新的 Architecture Revision，例如：

A1 → A2

不得直接修改 A1。

---

54. Final Architecture Summary

本 Triangle Rendering Engine 採用：

3-point input capture
        ↓
geometry parameter generation
        ↓
8-entry x-indexed column storage
        ↓
shared LOWER tracer
        ↓
LOWER Y2 Clamp
        ↓
shared UPPER tracer
        ↓
UPPER Y2 Clamp
        ↓
row-major output scan
        ↓
registered po/xo/yo
        ↓
RELEASE
        ↓
IDLE

核心 geometry representation：

[
\boxed{
Column(x)=[ylow(x),yup(x)]
}
]

核心 edge decision：

[
\boxed{
sE\ge0
}
]

其中：

[
s=sign(x_2-x_1)
]

LOWER：

[
\boxed{
E_L=dx(y-y_1)-(x-x_1)dy_{12}
}
]

UPPER：

[
\boxed{
E_U=(x-x_3)dy_{32}-dx(y-y_3)
}
]

Output condition：

[
\boxed{
ylow[x]\le y\le yup[x]
}
]

Output ordering：

[
\boxed{
y\uparrow,\quad x\uparrow
}
]

最後 valid point：

po   = 1
busy = 1

下一 cycle：

po   = 0
busy = 1

再下一 cycle：

po   = 0
busy = 0

---

55. Architecture Control Status

Architecture Revision: A1
Status: UNDER REVIEW
Freeze Candidate: YES
Functional Algorithm: VERIFIED
Exhaustive Geometry Evidence: 3136/3136 PASS
Cycle-Accurate Architecture: DEFINED
RTL: NOT STARTED
Synthesis: NOT STARTED
STA: NOT STARTED
Physical Design: NOT STARTED
Final Architecture Approval: PENDING PROJECT OWNER

這是已經freeze 的Architecture，依照目前工作與角色，接下來你要使用兩位AI（RTL ENGINEER, RTL VERIFICATION ）來寫出最終的RTL code