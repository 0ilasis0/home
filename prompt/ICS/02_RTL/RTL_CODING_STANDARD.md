# RTL Coding Standard

## 1. Purpose

本文件定義本專案所有 RTL 程式碼的統一 Coding Standard。

本文件不是 RTL Engineer 的角色定義，也不是 Architecture Specification。

其目的在於確保 RTL：

* 使用一致且可預期的 Verilog coding style
* 可被主流 ASIC synthesis flow 正確解析
* 容易被 Verification Engineer 建立 testbench、reference model 與 regression
* 容易進行 incremental implementation 與 TDD-like development
* 容易進行 code review、debug、maintenance 與 handoff
* 降低因 coding style、implicit behavior、width/signedness 或 module organization 所造成的 RTL bug
* 避免過早引入對 synthesis、STA、P&R 不利的 coding pattern
* 保持 Architecture、RTL、Verification 三者之間清楚的責任邊界

本文件適用於：

* RTL Engineer
* Verification Engineer（涉及 RTL/TB interface 或 shared RTL structure 時）
* Independent Reviewer
* 任何新增或修改 RTL source code 的 AI 或工程師

---

# 2. Language Standard

## 2.1 Verilog Only

本專案 RTL 必須使用：

> **Verilog HDL**

RTL source file 必須使用：

```text
.v
```

禁止使用：

```text
.sv
```

本專案不使用 SystemVerilog 作為 RTL implementation language。

---

## 2.2 Verilog Version

RTL 應以 **Verilog-2001 compatible syntax** 為主要 coding baseline。

除非 project toolchain 明確批准，禁止依賴較新的語言特性。

Coding style 應優先選擇：

```verilog
always @(*)
always @(posedge clk)
always @(posedge clk or posedge reset)
assign
wire
reg
parameter
localparam
generate
```

而不是依賴 SystemVerilog syntax。

---

# 3. Forbidden SystemVerilog Constructs

RTL 中禁止使用 SystemVerilog-only constructs。

例如：

```text
logic
always_comb
always_ff
always_latch
typedef
enum
struct
union
interface
package
class
virtual
modport
property
sequence
SystemVerilog assertion syntax
```

也禁止使用需要 SystemVerilog 才能成立的語法來繞過 Verilog 限制。

例如：

```verilog
logic [7:0] data;
```

禁止。

應使用：

```verilog
reg [7:0] data;
```

或：

```verilog
wire [7:0] data;
```

依照實際 driver 類型決定。

---

# 4. File Organization

## 4.1 File Extension

RTL：

```text
*.v
```

Shared Verilog include file：

```text
*.vh
```

禁止將 RTL implementation 放在 `.vh` 中。

---

## 4.2 One Primary Module per RTL File

原則上：

> 一個 `.v` 檔案只放一個主要 module。

例如：

```text
rtl/
├── top.v
├── controller.v
├── datapath.v
├── counter.v
└── arithmetic_unit.v
```

Module name 應與主要 source file name 對應。

例如：

```text
counter.v
```

應包含：

```verilog
module counter (...);
```

這項規則的目的：

* 容易搜尋 module
* 容易由 AI 定位與修改
* 容易建立 incremental implementation
* 容易建立獨立 verification scope
* 容易維護 file list
* 降低 synthesis/elaboration debug 困難

---

# 5. Module Decomposition

## 5.1 Module 化是必要的

RTL 不應將整個設計無限制地寫在單一 module 中。

應根據硬體架構將設計拆成合理的 module，例如：

```text
top
 ├── controller
 ├── datapath
 ├── arithmetic_unit
 └── output_logic
```

Module boundary 應反映實際硬體結構或清楚的功能責任。

---

## 5.2 Module 不等於 Increment

必須區分：

> Increment 是開發與驗證單位。

> Module 是硬體結構單位。

兩者不要求一對一。

禁止為了滿足 TDD 而任意建立大量沒有實際硬體意義的 module。

例如不應因為：

```text
Increment 1
Increment 2
Increment 3
```

就建立：

```text
increment1.v
increment2.v
increment3.v
```

如果它們實際上屬於同一個硬體 block，應維持合理 module structure。

---

## 5.3 Module 拆分原則

適合獨立成 module 的情況包括：

* 明確的硬體功能單元
* 可重複使用的硬體
* 複雜 arithmetic block
* 清楚的 controller
* 清楚的 datapath
* memory interface
* protocol/interface block
* 需要獨立驗證的重要 block

不適合拆成 module 的情況：

* 只有一兩行簡單 combinational logic
* 完全沒有獨立功能意義
* 只是為了增加檔案數量
* 會使 signal routing 與 hierarchy 變得不必要複雜

---

# 6. Module Interface

Module port 必須明確宣告方向與 width。

推薦：

```verilog
module example (
    input        clk,
    input        reset,
    input  [7:0] data_in,
    output [7:0] data_out
);
```

禁止依賴 implicit port width。

應避免：

```verilog
module example (
    clk,
    reset,
    data_in,
    data_out
);
```

如果使用傳統 ANSI-style declaration 不適用於特定舊工具，必須由 project/toolchain 明確批准。

---

# 7. Module Instantiation

優先使用 named port connection。

推薦：

```verilog
sub_module u_sub_module (
    .clk      (clk),
    .reset    (reset),
    .data_in  (data_in),
    .data_out (data_out)
);
```

不推薦：

```verilog
sub_module u_sub_module (
    clk,
    reset,
    data_in,
    data_out
);
```

Named connection 可以降低：

* port order 錯誤
* interface 修改造成的錯接
* code review 困難
* AI 修改時的 accidental connection error

---

# 8. parameter / localparam / `define / .vh

## 8.1 General Principle

必須明確區分：

| Mechanism       | 用途                                   |
| --------------- | ------------------------------------ |
| `parameter`     | Module 外部可配置的 compile-time parameter |
| `localparam`    | Module 內部固定的 compile-time constant   |
| `` `define ``   | Preprocessor macro                   |
| `.vh`           | Shared Verilog include definitions   |
| `reg/wire`      | Runtime hardware signals             |
| memory/register | 真正的 hardware state                   |

---

# 9. parameter

使用 `parameter` 表示：

> Module 使用者可以在 instantiation 時修改的 compile-time configuration。

例如：

```verilog
module counter #(
    parameter WIDTH = 8
) (
    input              clk,
    input              reset,
    output [WIDTH-1:0] count
);
```

適合：

* data width
* counter width
* configurable depth
* configurable implementation option

不應將不需要外部修改的內部常數宣告為 parameter。

---

# 10. localparam

使用 `localparam` 表示：

> Module 內部使用且不應由 module instance 修改的 compile-time constant。

例如：

```verilog
localparam DATA_WIDTH = 8;
localparam STATE_IDLE = 2'b00;
localparam STATE_RUN  = 2'b01;
localparam STATE_DONE = 2'b10;
```

特別適合：

* FSM state encoding
* internal width
* fixed threshold
* internal constant
* fixed operation code

---

# 11. `define

`define 是 preprocessor macro，不是 runtime hardware，也不是一般的 local variable。

應謹慎使用。

不應使用：

```verilog
`define DATA_WIDTH 8
```

來取代一個 module 明確需要的 parameter：

```verilog
parameter DATA_WIDTH = 8
```

如果某個值屬於 module configuration，優先使用 `parameter`。

如果只是 module 內部 constant，優先使用 `localparam`。

---

# 12. .vh Usage

`.vh` 僅應用於真正需要 Verilog preprocessor/include 機制的 shared definitions。

例如：

```text
include/
└── project_defs.vh
```

可能包含：

```verilog
`define PROJECT_VERSION 1
`define FEATURE_ENABLE  1
```

但不應把一般 RTL logic 放進 `.vh`：

禁止：

```verilog
always @(posedge clk)
begin
    ...
end
```

也不應將大量 module implementation 放入 `.vh`。

---

# 13. .vh Include Discipline

如果 `.vh` 可能被多次 include，應考慮 include guard。

例如：

```verilog
`ifndef PROJECT_DEFS_VH
`define PROJECT_DEFS_VH

`define PROJECT_VERSION 1

`endif
```

Macro naming 應避免與其他 library 或 module 發生 collision。

可使用 project-specific prefix，例如：

```verilog
`define PROJ_FEATURE_ENABLE
```

而不是過度通用的：

```verilog
`define ENABLE
```

---

# 14. Avoid Macro Overuse

不要使用大量 `` `define `` 來模擬：

* variable
* parameter
* function
* module
* state
* hardware connection

Macro 應保持少量、清楚且有必要。

一般優先順序：

```text
parameter
    ↓
localparam
    ↓
explicit signal
    ↓
`define
```

只有在真正需要 preprocessor behavior 時才使用 `` `define ``。

---

# 15. No Implicit Nets

RTL 不應依賴 undeclared signal 自動產生 implicit net。

推薦在 project/toolchain 支援且確認 integration 不會受到負面影響時，採用：

```verilog
`default_nettype none
```

並在 source/include 邊界正確恢復設定：

```verilog
`default_nettype wire
```

是否將此規則設為整個 project 的強制規範，應由 project toolchain baseline 明確確認。

---

# 16. Signal Declaration

Signal 必須有明確的：

* name
* direction
* width
* driver type
* signedness（需要時）

例如：

```verilog
wire [7:0] data_in;
reg  [7:0] data_q;
reg  [7:0] data_d;
```

不要依賴 implicit width 或 implicit signedness。

---

# 17. Naming Convention

命名必須保持一致且具有硬體語意。

推薦：

```text
clk
reset

*_q       registered/current state
*_d       next-state/combinational value
*_en      enable
*_valid   valid signal
*_ready   ready signal
*_cnt     counter
*_idx     index
*_state   state
```

例如：

```verilog
reg [7:0] count_q;
reg [7:0] count_d;
reg       count_en;
```

---

# 18. Clock Naming

Clock signal 優先使用清楚且一致的名稱：

```text
clk
clk_core
clk_mem
```

Reset：

```text
reset
reset_n
```

實際 active-high / active-low semantics 必須由 Architecture / Interface / Clock-Reset specification 決定。

Coding Standard 不得自行改變 reset semantics。

---

# 19. Sequential Logic

Sequential logic 使用：

```verilog
always @(posedge clk)
```

或依照 architecture 定義的 reset：

```verilog
always @(posedge clk or posedge reset)
```

Sequential register assignment 使用 non-blocking assignment：

```verilog
always @(posedge clk or posedge reset)
begin
    if (reset)
        data_q <= 8'd0;
    else
        data_q <= data_d;
end
```

禁止在一般 sequential RTL 中使用 blocking assignment：

```verilog
data_q = data_d;
```

除非存在經 review 明確批准的特殊理由。

---

# 20. Combinational Logic

Combinational logic 優先使用：

```verilog
always @(*)
```

或 continuous assignment：

```verilog
assign out = a & b;
```

Combinational procedural logic 使用 blocking assignment：

```verilog
always @(*)
begin
    result = a + b;
end
```

---

# 21. Blocking vs Non-blocking

基本規則：

```text
Combinational logic → =
Sequential logic    → <=
```

例如：

```verilog
always @(*)
begin
    next_state = state;
end
```

以及：

```verilog
always @(posedge clk)
begin
    state <= next_state;
end
```

避免在同一 sequential block 中混用 blocking 與 non-blocking，除非有明確且經 review 的理由。

---

# 22. Avoid Latches

所有 combinational output 必須在所有 control path 被賦值。

推薦：

```verilog
always @(*)
begin
    next_state = state;

    if (enable)
        next_state = STATE_RUN;
end
```

避免：

```verilog
always @(*)
begin
    if (enable)
        next_state = STATE_RUN;
end
```

後者可能產生 latch。

---

# 23. Avoid Multiple Drivers

一個 signal 不應被多個獨立 procedural block 或 continuous assignment 同時驅動。

禁止類似：

```verilog
always @(*)
    data = a;

always @(*)
    data = b;
```

除非該結構經 Architecture 明確定義且 tool-supported；一般 RTL 應避免。

---

# 24. Avoid Combinational Loops

禁止無意義的 combinational feedback。

例如：

```text
A → B → C → A
```

任何 combinational loop 都必須有明確 architecture justification，否則視為 RTL defect。

---

# 25. FSM Coding Style

FSM 應使用 `localparam` 定義 state。

例如：

```verilog
localparam STATE_IDLE = 2'b00;
localparam STATE_RUN  = 2'b01;
localparam STATE_DONE = 2'b10;
```

State register：

```verilog
reg [1:0] state_q;
reg [1:0] state_d;
```

Sequential state update：

```verilog
always @(posedge clk or posedge reset)
begin
    if (reset)
        state_q <= STATE_IDLE;
    else
        state_q <= state_d;
end
```

Combinational next-state：

```verilog
always @(*)
begin
    state_d = state_q;

    case (state_q)
        STATE_IDLE: begin
            ...
        end

        STATE_RUN: begin
            ...
        end

        STATE_DONE: begin
            ...
        end

        default: begin
            state_d = STATE_IDLE;
        end
    endcase
end
```

禁止使用：

```verilog
typedef enum
```

或其他 SystemVerilog FSM constructs。

---

# 26. case Statement

一般 FSM 與 control logic 優先使用：

```verilog
case
```

避免沒有明確理由就使用：

```verilog
casex
casez
```

因為 X/Z masking 可能隱藏 simulation 問題。

若必須使用 `casex` 或 `casez`，應有明確理由並經 review。

---

# 27. Arithmetic Coding

Arithmetic RTL 必須明確處理：

* width
* signedness
* overflow
* truncation
* extension
* comparison behavior

不要假設 Verilog expression sizing 一定符合預期。

---

# 28. Explicit Width

在重要 arithmetic、comparison、counter、index、address 等邏輯中，應使用明確 width。

例如：

```verilog
reg [7:0] count;
```

以及：

```verilog
count <= count + 8'd1;
```

比：

```verilog
count <= count + 1;
```

更容易確認 arithmetic intent。

---

# 29. Signedness

需要 signed arithmetic 時，必須明確處理 signedness。

禁止假設：

```verilog
reg [7:0] a;
reg [7:0] b;
```

就會自動產生預期的 signed behavior。

Signed/unsigned arithmetic 必須在 Architecture 與 RTL 中保持一致。

---

# 30. Width Growth

對加法、乘法、減法、比較等 operation，必須確認：

* operand width
* result width
* intermediate width
* sign extension
* zero extension
* truncation

例如：

```text
8-bit + 8-bit
```

不能只看 input width，而忽略：

```text
possible 9-bit result
```

任何 intentional truncation 必須是有意義且可被說明的設計決策。

---

# 31. Magic Numbers

避免在 RTL 中大量散落 magic numbers。

不推薦：

```verilog
if (count == 8'd127)
```

如果 `127` 是 architecture-defined constant，應考慮：

```verilog
localparam COUNT_MAX = 8'd127;
```

但如果數值本身就是非常明確的 literal，例如：

```verilog
if (enable == 1'b0)
```

則不需要為每個 literal 建立 constant。

---

# 32. Reset Coding

Reset 的：

* polarity
* synchronous/asynchronous behavior
* reset value
* release behavior

必須由 Architecture / Clock-Reset specification 決定。

Coding Standard 只規範 coding form，不得自行改變 architecture。

例如 architecture 定義 asynchronous active-high reset：

```verilog
always @(posedge clk or posedge reset)
begin
    if (reset)
        state_q <= STATE_IDLE;
    else
        state_q <= state_d;
end
```

如果 architecture 定義 synchronous reset：

```verilog
always @(posedge clk)
begin
    if (reset)
        state_q <= STATE_IDLE;
    else
        state_q <= state_d;
end
```

不可由 RTL Engineer 自行選擇另一種方式。

---

# 33. Clock Discipline

禁止任意建立 gated clock。

不推薦：

```verilog
assign gated_clk = clk & enable;
```

除非 Architecture 明確要求 clock gating，並且已有完整 clocking architecture 與 implementation strategy。

一般功能控制優先使用 enable：

```verilog
always @(posedge clk)
begin
    if (enable)
        data_q <= data_d;
end
```

---

# 34. Clock as Data

不得將 clock 任意當成一般 data signal 使用。

Clock 必須具有清楚的 clock domain 與用途。

任何：

* generated clock
* divided clock
* gated clock
* derived clock

都必須有 Architecture 層級定義。

---

# 35. CDC Awareness

跨 clock domain 的 signal 不得直接假設安全。

例如：

```text
clk_a domain
    ↓
clk_b domain
```

若 Architecture 存在 CDC，必須有明確 CDC strategy，例如：

* synchronizer
* handshake
* FIFO
* protocol-based crossing

RTL Engineer 不得為了「讓 simulation 通過」而任意修改 CDC architecture。

---

# 36. Memory Coding

Memory implementation 必須遵循 architecture 定義。

例如：

```verilog
reg [7:0] mem [0:255];
```

必須確認：

* read/write behavior
* synchronous/asynchronous read
* reset behavior
* initialization
* read-during-write behavior
* inference target

不要為了 simulation convenience 加入未經批准的 memory initialization。

---

# 37. Generate

Verilog `generate` 可以使用於：

* parameterized hardware
* repeated hardware structure
* compile-time structural selection

但不應為了 code style 而過度使用。

如果一般 procedural logic 已足夠，不應無意義地引入 generate hierarchy。

---

# 38. Synthesizable RTL

RTL 必須以 synthesis 為最終 implementation target。

除非明確批准，不得在 synthesizable RTL 使用：

```text
#delay
initial
force
release
$display
$finish
file I/O
randomization
testbench-only constructs
DPI
class
interface
SystemVerilog-only constructs
```

特別注意：

```verilog
#10 data = 1'b1;
```

不是一般 synthesizable RTL coding style。

---

# 39. Initial Block

一般 synthesizable RTL 不使用：

```verilog
initial begin
    ...
end
```

如果特定 technology / FPGA / memory implementation 明確支援 initialization，必須由 Architecture / implementation constraint 明確批准。

不可因為 simulation 需要而自行加入。

---

# 40. Testbench Code Must Not Enter RTL

RTL source 不應包含 testbench-only logic。

例如：

```verilog
$display
$monitor
$random
$fopen
$fwrite
```

等不得出現在 production synthesizable RTL 中。

Verification-specific logic 應放在：

```text
TB/
```

或其他明確 verification source。

---

# 41. RTL and Verification Boundary

RTL 應提供清楚且穩定的 architectural interface。

Verification 不應要求 RTL 加入：

* debug-only output
* testbench-only state
* simulation-only behavior

除非該 debug interface 本身是經批准的 project requirement。

Verification 應優先從 architectural observable behavior 驗證設計。

---

# 42. TDD / Incremental Development Compatibility

RTL structure 必須支援 incremental development。

基本流程：

```text
Architecture
    ↓
Implementation Plan
    ↓
Increment
    ↓
RTL Change
    ↓
Verification
    ↓
Fix
    ↓
Regression
    ↓
Next Increment
```

RTL Engineer 不應為了「一次完成」而建立過度複雜、難以獨立驗證的單體 RTL。

---

# 43. Stable Module Boundaries

為支援 TDD 與 Verification：

* module interface 應穩定
* module responsibility 應清楚
* combinational/sequential responsibility 應可理解
* state ownership 應明確
* arithmetic ownership 應明確

不要頻繁任意改 module boundary。

如果 Architecture 已 freeze，module boundary 若涉及 architecture change，必須走 Change Request。

---

# 44. No Testbench-Driven RTL Hacks

不得為了通過某一個 test case 而在 RTL 加入特殊 hack。

禁止概念上類似：

```text
if (specific_input_pattern)
    force_expected_answer;
```

或：

```text
if (simulation_only_condition)
    bypass_normal_logic;
```

Verification failure 必須先分析：

```text
Specification
    ↓
Architecture
    ↓
RTL
    ↓
Testbench / Reference Model
```

再判斷問題來源。

---

# 45. Comments

Comment 應解釋：

* 為什麼這樣設計
* 重要 arithmetic intent
* 特殊 width handling
* 特殊 timing requirement
* architecture constraint
* 非直覺的 implementation decision

不要只重複程式碼本身。

不推薦：

```verilog
// Add one
count = count + 1'b1;
```

較有價值：

```verilog
// Counter wraps after the architecture-defined maximum cycle.
count_d = count_q + 8'd1;
```

---

# 46. No Dead Code

不要保留：

* 已經不使用的 signal
* 已經移除的 state
* obsolete parameter
* dead module
* debug code
* commented-out large code blocks

如果 code 已經不再使用，應移除，而不是長期保留。

Version control / project history 應負責保存舊版本。

---

# 47. No Silent Design Change

Coding Standard 不允許 RTL Engineer 透過 coding style 偷渡 architecture change。

例如以下內容若與 Frozen Architecture 不一致，不能自行修改：

* interface
* protocol
* latency
* throughput
* FSM behavior
* output order
* arithmetic definition
* clock behavior
* reset behavior
* memory behavior
* CDC strategy

應提出 Change Request。

---

# 48. Parameterization Discipline

不要為了「看起來 reusable」而過度 parameterize。

不應把所有常數都變成：

```verilog
parameter
```

只有真正具有 reuse/configuration value 的項目才 parameterize。

原則：

```text
External configuration → parameter
Internal fixed constant → localparam
Shared compile-time macro → `define / .vh
Runtime value → wire/reg
```

---

# 49. Module Reuse

如果建立 reusable module，應確認：

* interface 清楚
* parameter 意義清楚
* width handling 明確
* reset behavior 明確
* signedness 明確
* latency 明確
* synthesis behavior 可預期

不可只因為「未來可能會用」就建立過度泛化的 module。

---

# 50. Hierarchy Discipline

Hierarchy 應反映設計責任。

推薦：

```text
top
 ├── control
 ├── datapath
 │    ├── arithmetic
 │    └── register
 └── interface
```

避免：

```text
top
 ├── tiny_module_1
 ├── tiny_module_2
 ├── tiny_module_3
 ├── tiny_module_4
 ├── tiny_module_5
 └── ...
```

如果 hierarchy 只增加管理成本而沒有 verification、reuse 或 architectural value，應重新檢視。

---

# 51. ASIC Awareness

RTL coding 必須考慮後續：

```text
RTL
 ↓
Synthesis
 ↓
Gate-level Netlist
 ↓
STA
 ↓
Physical Design
 ↓
DRC / LVS
 ↓
Signoff
```

RTL 不應只以 RTL simulation pass 作為唯一品質標準。

需要避免：

* 不必要的巨大 combinational logic
* 不必要的超寬 arithmetic
* 不必要的 deep combinational path
* 不可控的 fanout
* 不必要的 duplicated hardware
* 不必要的 clock generation
* 不清楚的 memory inference
* 不可預期的 synthesis behavior

但：

> Coding Standard 不得自行決定 architecture optimization。

如果需要改變：

* pipeline
* resource sharing
* arithmetic architecture
* memory architecture
* clock architecture

必須回到 Architecture Change Process。

---

# 52. Synthesis-Friendly Coding

RTL 應保持 synthesis tool 能夠清楚推導 intended hardware。

優先使用：

* clear register boundaries
* clear combinational logic
* explicit width
* explicit signedness
* simple FSM
* clear enable conditions
* clear module hierarchy

避免：

* ambiguous expression sizing
* hidden implicit behavior
* unnecessary macro tricks
* simulation-only constructs
* unusual procedural tricks

---

# 53. Physical Design Awareness

RTL Engineer 不需要直接進行 floorplan 或 routing，但應避免明顯會增加 physical implementation risk 的 coding pattern。

例如：

* 不必要的巨大 fanout
* 不必要的全域控制 signal
* 巨大的 combinational cone
* 不必要的 duplicated logic
* 過度寬的 datapath
* 任意 generated clock

如果 architecture-level trade-off 可能影響 physical implementation，應回報 AI-0 / System Architect，而不是自行修改 architecture。

---

# 54. Verification-Friendly RTL

RTL 應使 Verification Engineer 能夠明確觀察：

* input behavior
* output behavior
* valid timing
* state transitions
* latency
* protocol
* reset behavior

Verification 不應依賴 implementation accident。

例如，不應假設：

```text
某個 internal signal 一定存在
```

除非該 signal 已被定義為 verification-visible requirement。

---

# 55. Assertions

本專案 RTL 使用 Verilog-only language baseline。

因此不得要求 RTL 使用 SystemVerilog Assertion：

```text
assert property
property
sequence
```

Verification 若需要 assertion-like checking，應使用符合 project toolchain 與 Verilog baseline 的 verification mechanism，或由外部 verification environment / simulator mechanism 執行。

---

# 56. Debug Signals

Debug signal 是否加入 RTL 必須有明確目的。

不要為了方便 debug 就大量增加：

```text
output debug_state
output debug_counter
output debug_internal_signal
```

如果 debug signal 改變 module interface，必須確認：

* Architecture impact
* Verification impact
* synthesis impact
* integration impact

---

# 57. Code Formatting

RTL 應保持一致格式。

推薦：

```verilog
always @(posedge clk or posedge reset)
begin
    if (reset)
        data_q <= 8'd0;
    else
        data_q <= data_d;
end
```

Indentation 應一致。

例如：

```text
4 spaces
```

或 project 明確定義的 indentation。

禁止同一 project 混用多種完全不同的 style。

---

# 58. Signal Assignment Style

推薦將 default assignment 放在 combinational block 前面。

例如：

```verilog
always @(*)
begin
    next_state = state_q;
    data_d     = data_q;
    valid_d    = 1'b0;

    case (state_q)
        ...
    endcase
end
```

這有助於：

* 避免 latch
* 清楚表達 default behavior
* 增加 review 可讀性

---

# 59. Sequential State Separation

對複雜 FSM，推薦分離：

```text
State register
Next-state logic
Output/control logic
```

例如：

```verilog
always @(posedge clk or posedge reset)
begin
    ...
end

always @(*)
begin
    ...
end
```

不應把大量 combinational decision 與 sequential storage 混在一個難以閱讀的 block 中。

---

# 60. Arithmetic Functionality Separation

如果 arithmetic logic 足夠複雜，應考慮獨立 module。

例如：

```text
controller.v
datapath.v
multiplier.v
comparator.v
```

但簡單 arithmetic 不需要強制拆 module。

原則：

> Module boundary 應服務硬體結構與 verification，而不是服務檔案數量。

---

# 61. No Premature Optimization

不要在 RTL implementation 初期為了：

* 少幾個 gate
* 少幾個 cycle
* 少幾個 signal
* 看起來更 clever

而直接改變 architecture。

正確順序：

```text
Correct behavior
    ↓
Verification
    ↓
Synthesis evidence
    ↓
Timing / Area analysis
    ↓
Optimization
```

Optimization 必須有 evidence 或明確 architecture requirement 支持。

---

# 62. Error-Prone Constructs

以下 coding pattern 應特別 review：

* unsized arithmetic constants
* implicit signedness
* implicit width conversion
* incomplete combinational assignments
* multiple drivers
* accidental latch
* accidental combinational loop
* unintended priority logic
* unintended arithmetic truncation
* `casex`
* `casez`
* mixed blocking/non-blocking assignment
* accidental clock generation
* accidental asynchronous behavior
* hidden state
* macro side effects

---

# 63. RTL Review Checklist

每次重要 RTL increment 完成後，至少檢查：

## Language

* [ ] File extension is `.v`
* [ ] No SystemVerilog syntax
* [ ] Verilog-2001 compatible style

## Structure

* [ ] Module responsibility is clear
* [ ] Module/file naming is consistent
* [ ] Module hierarchy is reasonable
* [ ] No unnecessary module fragmentation

## Parameters

* [ ] `parameter` only for external configuration
* [ ] `localparam` for internal constants
* [ ] `define` used only when necessary
* [ ] `.vh` contains only appropriate shared definitions

## Signals

* [ ] All signals explicitly declared
* [ ] Width is explicit where important
* [ ] Signedness is intentional
* [ ] Naming convention is consistent

## Sequential Logic

* [ ] Correct clock sensitivity
* [ ] Correct reset sensitivity
* [ ] Non-blocking assignment used
* [ ] No unintended state

## Combinational Logic

* [ ] Blocking assignment used
* [ ] All outputs assigned
* [ ] No unintended latch
* [ ] No combinational loop

## FSM

* [ ] State encoding uses `localparam`
* [ ] Default state behavior exists
* [ ] Illegal state behavior is defined
* [ ] State transition matches Architecture

## Arithmetic

* [ ] Operand widths checked
* [ ] Result widths checked
* [ ] Signedness checked
* [ ] Overflow/truncation intentional

## ASIC

* [ ] Synthesizable
* [ ] No simulation-only construct
* [ ] No arbitrary gated clock
* [ ] No unintended large combinational logic
* [ ] No obvious physical implementation risk

## Verification

* [ ] Interface remains stable
* [ ] Observable behavior is clear
* [ ] Increment can be independently verified
* [ ] No testbench-specific hack
* [ ] No modification made merely to hide a verification failure

---

# 64. Compliance Levels

RTL coding compliance can be classified as:

### COMPLIANT

RTL follows this Coding Standard and no known violation exists.

### COMPLIANT WITH APPROVED EXCEPTION

RTL violates a general rule for a documented reason and the exception has been approved.

### NON-COMPLIANT

RTL violates the Coding Standard without an approved reason.

### NOT REVIEWED

RTL has not yet undergone coding-standard review.

---

# 65. Exception Process

如果某個 design requirement 確實需要違反 Coding Standard：

不得直接忽略規則。

應記錄：

```text
Rule:
Reason:
Affected RTL:
Technical justification:
Risk:
Alternative considered:
Approval:
```

如果 exception 涉及 architecture，必須另外遵循 Architecture Change Process。

Coding Standard exception 不等於 Architecture approval。

---

# 66. Relationship with Architecture

責任邊界：

```text
Specification
    ↓
Architecture
    ↓
RTL Coding Standard
    ↓
RTL Implementation
```

Architecture 定義：

> 硬體應該做什麼，以及 cycle-level / interface / control / datapath 等行為。

Coding Standard 定義：

> RTL 應該以什麼 coding discipline 實現。

RTL Engineer 定義：

> 如何把 frozen architecture 實作成 RTL。

Coding Standard 不得凌駕於正式 Specification 或 Frozen Architecture。

如果兩者衝突：

```text
Specification / Frozen Architecture
        ↓
Coding Standard
```

應停止並回報衝突，而不是自行修改 architecture。

---

# 67. Relationship with Verification

Verification Engineer 應使用本 Coding Standard 判斷 RTL 是否：

* 可讀
* 可觀察
* 可驗證
* module boundary 清楚
* interface 穩定
* 不包含 simulation-only hack

但 Verification Engineer 不應因為 coding style 偏好而改變 RTL architecture。

Verification 發現問題時應區分：

```text
Functional bug
Coding-standard violation
Architecture mismatch
Verification environment bug
Unknown
```

不能將所有問題都視為 RTL bug。

---

# 68. Relationship with Incremental RTL Development

每個 RTL increment 都應盡量保持：

```text
Small change
    ↓
Compile
    ↓
Basic verification
    ↓
Regression
    ↓
Review
```

避免：

```text
Large uncontrolled change
    ↓
Compile everything
    ↓
Unknown failure source
```

RTL coding structure 必須協助定位問題來源。

---

# 69. Change Discipline

任何 RTL 修改都必須能回答：

```text
What changed?
Why changed?
Which module changed?
Which requirement does it affect?
Which architecture decision does it depend on?
How was it verified?
Did interface change?
Did timing behavior change?
Did latency change?
Did resource behavior change?
```

若無法回答，該 change 不應直接視為完成。

---

# 70. Final Principles

本專案 RTL coding 的核心原則：

```text
1. Verilog only.
2. .v only for RTL.
3. Use parameter for external configuration.
4. Use localparam for internal constants.
5. Use .vh / `define only when preprocessor sharing is actually required.
6. Do not use .vh as a container for RTL logic.
7. Keep module boundaries meaningful.
8. Increment is a development unit, not necessarily a module.
9. Prefer explicit width and signedness.
10. Use blocking assignment for combinational logic.
11. Use non-blocking assignment for sequential logic.
12. Avoid latches and multiple drivers.
13. Keep FSM structure explicit.
14. Keep reset and clock behavior consistent with Architecture.
15. Avoid arbitrary clock gating or generated clocks.
16. Keep RTL synthesizable.
17. Keep testbench logic outside production RTL.
18. Do not modify RTL merely to hide verification failures.
19. Do not silently change Frozen Architecture.
20. Optimize only with evidence and controlled design changes.
21. Write RTL so that another engineer or AI can understand, verify, review, and maintain it.
```

---

# 71. Core Engineering Principle

本專案採用以下責任分工：

```text
Specification
    ↓
defines WHAT is required

Architecture
    ↓
defines HOW the hardware should behave and be structured

RTL Coding Standard
    ↓
defines HOW RTL code must be written

RTL Engineer
    ↓
implements the frozen Architecture

Verification Engineer
    ↓
determines whether implemented behavior matches requirements

Synthesis / STA / P&R
    ↓
provide implementation evidence

Human
    ↓
approves major design decisions and controlled changes
```

最重要的原則：

> **Architecture defines WHAT and the hardware behavior.
> RTL defines HOW that Architecture is implemented.
> Coding Standard defines HOW RTL code is written.
> Verification determines WHETHER the implementation matches the intended behavior.
> EDA tools provide implementation evidence.**

任何單一 AI 都不得透過 coding style、implementation convenience 或 verification convenience，偷偷改變已確認的 architecture。


# 71. 在coding前需要先標註以下資訊
1. RTL Version:
2. RTL Baseline ID:
3. RTL Commit/Revision: