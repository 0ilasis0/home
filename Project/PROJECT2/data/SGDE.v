<<<<<<< Updated upstream
// =============================================================================
// File Name   : SGDE.v
// Module Name : SGDE, SGDE_Object_Collector, SGDE_Sprite_Fetcher,
//               SGDE_Collision_Engine, SGDE_Framebuffer_Renderer
// RTL Version : v1.7
// Baseline ID : SGDE_BASELINE_2026_01
// Revision    : rev_014_opt_candidate_mask
// Arch Status : FROZEN
// Description : Synthesizable Verilog-2001 implementation of SGDE:
//               1. SGDE: Top-level coordinator managing object collection,
//                  ROM fetch, pixel-level collision detection, conditional
//                  Man sprite reload, 64x64 framebuffer rendering, and
//                  single-cycle done handshake. Multiplexes Man read port
//                  between Collision Engine and Framebuffer Renderer.
//               2. SGDE_Object_Collector: 20-entry object storage, sole owner
//                  of obj_count (0..20), supporting clean batch re-arming and
//                  7-bit unsigned bounding-box + type candidate masks (B1+B2c)
//                  for objects 1..19 (gc_hit_mask, flw_hit_mask).
//               3. SGDE_Sprite_Fetcher: Negedge-driven SR_CEN/SR_A launch and
//                  posedge SR_Q capture into 1280-word (20x64) sprite_mem
//                  with dual combinational read ports (full 13-bit Man port
//                  and general candidate port; no 3rd read port added).
//               4. SGDE_Collision_Engine: Pixel-accurate 8x8 mask intersection
//                  scanner with Candy > Ghost > Live priority (UNCHANGED).
//               5. SGDE_Framebuffer_Renderer: Optimized 64x64 row-major pixel
//                  compositor integrating B1 (Type Mask), B2c (In-Box Mask),
//                  B3 (Early Termination), and B4-Man (Same-Cycle Parallel Man
//                  & Top Candidate Evaluation) with registered lookahead
//                  priority encoding to break critical combinational paths.
// =============================================================================

// =============================================================================
// Module 1: SGDE (Top-Level Coordinator)
// =============================================================================
module SGDE (
    ready, done, clk, reset, sprite, start, type, X, Y,
    SR_CEN, SR_A, SR_Q, FB_CEN, FB_WEN, FB_A, FB_D, FB_Q
);

input         clk, reset, sprite, start;
input  [1:0]  type;
input  [5:0]  X, Y;
input  [12:0] SR_Q;
input  [11:0] FB_Q;

output        ready, done;
output        SR_CEN, FB_CEN, FB_WEN;
output [8:0]  SR_A;
output [11:0] FB_A, FB_D;

// -----------------------------------------------------------------------------
// Registered Top-Level Outputs Owned by SGDE Coordinator
// (SR_CEN and SR_A are registered outputs owned by SGDE_Sprite_Fetcher)
// -----------------------------------------------------------------------------
reg        ready, done;
reg        FB_CEN, FB_WEN;
reg [11:0] FB_A, FB_D;

reg        ready_d, done_d;

// -----------------------------------------------------------------------------
// Architectural Constants (localparam)
// -----------------------------------------------------------------------------
localparam [8:0]  BASE_MAN_LIVE  = 9'd0;
localparam [8:0]  BASE_MAN_HAPPY = 9'd64;
localparam [8:0]  BASE_MAN_DIE   = 9'd128;
localparam [11:0] BG_COLOR       = 12'hCF0;

// Man resolved display state encodings
localparam [1:0]  MAN_LIVE       = 2'd0;
localparam [1:0]  MAN_HAPPY      = 2'd1;
localparam [1:0]  MAN_DIE        = 2'd2;

// Top-Level Batch Coordinator FSM States
localparam [2:0] ST_RESET        = 3'd0;
localparam [2:0] ST_COLLECT      = 3'd1;
localparam [2:0] ST_FETCH_ALL    = 3'd2; // Fetch all 0..obj_count-1 sprites
localparam [2:0] ST_COLLISION    = 3'd3; // Pixel-level collision evaluation
localparam [2:0] ST_MAN_RELOAD   = 3'd4; // Reload Man Happy/Die sprite if hit
localparam [2:0] ST_RENDER       = 3'd5; // Render all 4096 pixels to FB SRAM
localparam [2:0] ST_DONE         = 3'd6; // 1-cycle done pulse after 4095 write
localparam [2:0] ST_RETURN       = 3'd7; // Re-arm collector for next batch

// -----------------------------------------------------------------------------
// Top-Level Coordinator Registers & Inter-Module Wires
// -----------------------------------------------------------------------------
reg  [2:0] state_q, state_d;
reg  [8:0] man_state_base_q, man_state_base_d;
reg  [1:0] man_state_q, man_state_d;
reg        candy_hit_q, candy_hit_d;
reg        ghost_hit_q, ghost_hit_d;
reg        fetch_start_q, fetch_start_d;
reg        reload_man_start_q, reload_man_start_d;
reg        coll_start_q, coll_start_d;
reg        rend_start_q, rend_start_d;

// Control strobes to submodules
wire       collect_en;
wire       batch_rearm;
wire       batch_armed;
wire       new_batch_init;

// Collector read port, candidate mask & status wires
wire [4:0]  obj_count;
wire [4:0]  collector_rd_idx;
wire [1:0]  collector_rd_type;
wire [5:0]  collector_rd_x;
wire [5:0]  collector_rd_y;
wire [5:0]  man_x;
wire [5:0]  man_y;
wire [5:0]  rend_query_x;
wire [5:0]  rend_query_y;
wire [19:1] collector_gc_hit_mask;
wire [19:1] collector_flw_hit_mask;

// Sprite Fetcher interconnect wires
wire [4:0]  fetcher_obj_idx;
wire        fetcher_rom_valid;
wire [4:0]  fetcher_rom_valid_obj_idx;
wire [5:0]  fetcher_rom_valid_pixel_idx;
wire [12:0] fetcher_rom_data;
wire [11:0] fetcher_rom_rgb;
wire        fetcher_rom_mask;
wire [5:0]  mux_man_rd_pixel_idx;
wire [4:0]  mux_buf_rd_obj_idx;
wire [5:0]  mux_buf_rd_pixel_idx;
wire [5:0]  coll_man_rd_pixel_idx;
wire [5:0]  rend_man_rd_pixel_idx;
wire [12:0] fetcher_man_rd_data;
wire        fetcher_man_rd_mask;
wire        fetcher_man_rd_valid;
wire [12:0] fetcher_buf_rd_data;
wire        fetcher_buf_rd_valid;
wire        fetch_busy;
wire        fetch_done;

// Collision Engine interconnect wires
wire        coll_busy;
wire        coll_done;
wire [4:0]  coll_obj_idx;
wire [5:0]  coll_target_pixel_idx;
wire        coll_candy_hit;
wire        coll_ghost_hit;
wire [1:0]  coll_man_state;
wire [8:0]  coll_man_state_base;
wire        coll_rd_invalid_err;

// Framebuffer Renderer interconnect wires
wire        rend_busy;
wire        rend_done;
wire [4:0]  rend_obj_idx;
wire [5:0]  rend_pixel_idx;
wire        rend_fb_wr_req;
wire [11:0] rend_fb_wr_addr;
wire [11:0] rend_fb_wr_data;

// Multiplex Collector read index across active processing phases
assign collector_rd_idx     = (state_q == ST_RENDER)    ? rend_obj_idx :
                              (state_q == ST_COLLISION) ? coll_obj_idx :
                                                          fetcher_obj_idx;

// Multiplex Man dedicated read port between Renderer (B4-Man) and Collision Engine
assign mux_man_rd_pixel_idx = (state_q == ST_RENDER) ? rend_man_rd_pixel_idx : coll_man_rd_pixel_idx;

// Multiplex general Sprite Buffer read port between Renderer and Collision Engine
assign mux_buf_rd_obj_idx   = (state_q == ST_RENDER) ? rend_obj_idx   : coll_obj_idx;
assign mux_buf_rd_pixel_idx = (state_q == ST_RENDER) ? rend_pixel_idx : coll_target_pixel_idx;

// Submodule control enables
assign collect_en     = (state_q == ST_COLLECT) && ready;
assign batch_rearm    = (state_q == ST_RETURN);
assign new_batch_init = collect_en && sprite && batch_armed;

// -----------------------------------------------------------------------------
// Submodule Instantiation 1: SGDE_Object_Collector
// -----------------------------------------------------------------------------
SGDE_Object_Collector u_object_collector (
    .clk          (clk),
    .reset        (reset),
    .collect_en   (collect_en),
    .batch_rearm  (batch_rearm),
    .sprite       (sprite),
    .type         (type),
    .X            (X),
    .Y            (Y),
    .rd_idx       (collector_rd_idx),
    .query_x      (rend_query_x),
    .query_y      (rend_query_y),
    .batch_armed  (batch_armed),
    .obj_count    (obj_count),
    .rd_type      (collector_rd_type),
    .rd_x         (collector_rd_x),
    .rd_y         (collector_rd_y),
    .man_x        (man_x),
    .man_y        (man_y),
    .gc_hit_mask  (collector_gc_hit_mask),
    .flw_hit_mask (collector_flw_hit_mask)
);

// -----------------------------------------------------------------------------
// Submodule Instantiation 2: SGDE_Sprite_Fetcher
// -----------------------------------------------------------------------------
SGDE_Sprite_Fetcher u_sprite_fetcher (
    .clk                 (clk),
    .reset               (reset),
    .fetch_start         (fetch_start_q),
    .reload_man_start    (reload_man_start_q),
    .batch_clr           (new_batch_init),
    .obj_count           (obj_count),
    .man_state_base      (man_state_base_q),
    .obj_rd_type         (collector_rd_type),
    .SR_Q                (SR_Q),
    .man_rd_pixel_idx    (mux_man_rd_pixel_idx),
    .buf_rd_obj_idx      (mux_buf_rd_obj_idx),
    .buf_rd_pixel_idx    (mux_buf_rd_pixel_idx),
    .obj_idx             (fetcher_obj_idx),
    .SR_CEN              (SR_CEN),
    .SR_A                (SR_A),
    .rom_valid           (fetcher_rom_valid),
    .rom_valid_obj_idx   (fetcher_rom_valid_obj_idx),
    .rom_valid_pixel_idx (fetcher_rom_valid_pixel_idx),
    .rom_data_reg        (fetcher_rom_data),
    .rom_rgb_reg         (fetcher_rom_rgb),
    .rom_mask_reg        (fetcher_rom_mask),
    .man_rd_data         (fetcher_man_rd_data),
    .man_rd_mask         (fetcher_man_rd_mask),
    .man_rd_valid        (fetcher_man_rd_valid),
    .buf_rd_data         (fetcher_buf_rd_data),
    .buf_rd_valid        (fetcher_buf_rd_valid),
    .fetch_busy          (fetch_busy),
    .fetch_done          (fetch_done)
);

// -----------------------------------------------------------------------------
// Submodule Instantiation 3: SGDE_Collision_Engine (UNCHANGED)
// -----------------------------------------------------------------------------
SGDE_Collision_Engine u_collision_engine (
    .clk              (clk),
    .reset            (reset),
    .coll_start       (coll_start_q),
    .batch_clr        (new_batch_init),
    .obj_count        (obj_count),
    .man_x            (man_x),
    .man_y            (man_y),
    .man_rd_mask      (fetcher_man_rd_mask),
    .man_rd_valid     (fetcher_man_rd_valid),
    .cand_type        (collector_rd_type),
    .cand_x           (collector_rd_x),
    .cand_y           (collector_rd_y),
    .cand_rd_mask     (fetcher_buf_rd_data[0]),
    .cand_rd_valid    (fetcher_buf_rd_valid),
    .man_rd_pixel_idx (coll_man_rd_pixel_idx),
    .obj_idx          (coll_obj_idx),
    .target_pixel_idx (coll_target_pixel_idx),
    .candy_hit        (coll_candy_hit),
    .ghost_hit        (coll_ghost_hit),
    .man_state        (coll_man_state),
    .man_state_base   (coll_man_state_base),
    .rd_invalid_err   (coll_rd_invalid_err),
    .coll_busy        (coll_busy),
    .coll_done        (coll_done)
);

// -----------------------------------------------------------------------------
// Submodule Instantiation 4: SGDE_Framebuffer_Renderer (B1+B3+B4-Man+B2c)
// -----------------------------------------------------------------------------
SGDE_Framebuffer_Renderer u_framebuffer_renderer (
    .clk              (clk),
    .reset            (reset),
    .rend_start       (rend_start_q),
    .batch_clr        (new_batch_init),
    .obj_count        (obj_count),
    .man_x            (man_x),
    .man_y            (man_y),
    .man_rd_data      (fetcher_man_rd_data),
    .man_rd_valid     (fetcher_man_rd_valid),
    .cand_x           (collector_rd_x),
    .cand_y           (collector_rd_y),
    .buf_rd_data      (fetcher_buf_rd_data),
    .buf_rd_valid     (fetcher_buf_rd_valid),
    .gc_hit_mask      (collector_gc_hit_mask),
    .flw_hit_mask     (collector_flw_hit_mask),
    .query_x          (rend_query_x),
    .query_y          (rend_query_y),
    .man_rd_pixel_idx (rend_man_rd_pixel_idx),
    .obj_idx          (rend_obj_idx),
    .pixel_idx        (rend_pixel_idx),
    .fb_wr_req        (rend_fb_wr_req),
    .fb_wr_addr       (rend_fb_wr_addr),
    .fb_wr_data       (rend_fb_wr_data),
    .rend_busy        (rend_busy),
    .rend_done        (rend_done)
);

// -----------------------------------------------------------------------------
// Top-Level Coordinator Combinational Next-State Logic
// -----------------------------------------------------------------------------
always @(*) begin
    state_d            = state_q;
    ready_d            = 1'b0;
    done_d             = 1'b0;

    man_state_base_d   = man_state_base_q;
    man_state_d        = man_state_q;
    candy_hit_d        = candy_hit_q;
    ghost_hit_d        = ghost_hit_q;
    fetch_start_d      = 1'b0;
    reload_man_start_d = 1'b0;
    coll_start_d       = 1'b0;
    rend_start_d       = 1'b0;

    case (state_q)
        // ---------------------------------------------------------------------
        // 1. Post-Reset Entry: transition to ST_COLLECT and assert ready
        // ---------------------------------------------------------------------
        ST_RESET: begin
            ready_d            = 1'b1;
            done_d             = 1'b0;
            man_state_base_d   = BASE_MAN_LIVE;
            man_state_d        = MAN_LIVE;
            candy_hit_d        = 1'b0;
            ghost_hit_d        = 1'b0;
            fetch_start_d      = 1'b0;
            reload_man_start_d = 1'b0;
            coll_start_d       = 1'b0;
            rend_start_d       = 1'b0;
            state_d            = ST_COLLECT;
        end

        // ---------------------------------------------------------------------
        // 2. Object Collection Phase (Storage handled by SGDE_Object_Collector)
        // ---------------------------------------------------------------------
        ST_COLLECT: begin
            ready_d = 1'b1;
            done_d  = 1'b0;

            if (ready && sprite) begin
                if (batch_armed) begin
                    // First object (index 0) of a new batch: initialize Man to Live
                    man_state_base_d = BASE_MAN_LIVE;
                    man_state_d      = MAN_LIVE;
                    candy_hit_d      = 1'b0;
                    ghost_hit_d      = 1'b0;
                end
                state_d = ST_COLLECT;
            end else if (ready && start) begin
                ready_d          = 1'b0;
                man_state_base_d = BASE_MAN_LIVE;
                man_state_d      = MAN_LIVE;
                candy_hit_d      = 1'b0;
                ghost_hit_d      = 1'b0;
                fetch_start_d    = 1'b1;
                state_d          = ST_FETCH_ALL;
            end
        end

        // ---------------------------------------------------------------------
        // 3. Initial Sprite ROM Fetch Phase (Fetches Man Live + Objects 1..N-1)
        // ---------------------------------------------------------------------
        ST_FETCH_ALL: begin
            ready_d       = 1'b0;
            fetch_start_d = 1'b0;

            if (fetch_done) begin
                coll_start_d = 1'b1;
                state_d      = ST_COLLISION;
            end else begin
                state_d      = ST_FETCH_ALL;
            end
        end

        // ---------------------------------------------------------------------
        // 4. Pixel-Level Collision Scan Phase (SGDE_Collision_Engine)
        // ---------------------------------------------------------------------
        ST_COLLISION: begin
            ready_d      = 1'b0;
            coll_start_d = 1'b0;

            if (coll_done) begin
                candy_hit_d      = coll_candy_hit;
                ghost_hit_d      = coll_ghost_hit;
                man_state_d      = coll_man_state;
                man_state_base_d = coll_man_state_base;

                // If Man changed to Happy (64) or Die (128), reload Object 0
                // in sprite_mem[0:63] before starting 64x64 rendering.
                if (coll_man_state_base != BASE_MAN_LIVE) begin
                    reload_man_start_d = 1'b1;
                    state_d            = ST_MAN_RELOAD;
                end else begin
                    rend_start_d       = 1'b1;
                    state_d            = ST_RENDER;
                end
            end else begin
                state_d = ST_COLLISION;
            end
        end

        // ---------------------------------------------------------------------
        // 5. Reload Resolved Man Sprite (Happy/Die) into Object 0 Buffer Slot
        // ---------------------------------------------------------------------
        ST_MAN_RELOAD: begin
            ready_d            = 1'b0;
            reload_man_start_d = 1'b0;

            if (fetch_done) begin
                rend_start_d = 1'b1;
                state_d      = ST_RENDER;
            end else begin
                state_d      = ST_MAN_RELOAD;
            end
        end

        // ---------------------------------------------------------------------
        // 6. 64x64 Framebuffer Rendering Phase (SGDE_Framebuffer_Renderer)
        //    rend_done asserts only AFTER address 4095 write has been sampled
        //    by FB.v on posedge clk and FB_CEN/FB_WEN return high on negedge.
        // ---------------------------------------------------------------------
        ST_RENDER: begin
            ready_d      = 1'b0;
            rend_start_d = 1'b0;

            if (rend_done) begin
                done_d  = 1'b1;
                state_d = ST_DONE;
            end else begin
                state_d = ST_RENDER;
            end
        end

        // ---------------------------------------------------------------------
        // 7. One-Cycle Completion Pulse
        // ---------------------------------------------------------------------
        ST_DONE: begin
            done_d  = 1'b0;
            ready_d = 1'b0;
            state_d = ST_RETURN;
        end

        // ---------------------------------------------------------------------
        // 8. Batch Return & Re-Arm Collection
        //    Preserves resolved Man state until the next batch begins.
        // ---------------------------------------------------------------------
        ST_RETURN: begin
            done_d             = 1'b0;
            ready_d            = 1'b1;
            fetch_start_d      = 1'b0;
            reload_man_start_d = 1'b0;
            coll_start_d       = 1'b0;
            rend_start_d       = 1'b0;
            state_d            = ST_COLLECT;
        end

        default: begin
            state_d = ST_RESET;
        end
    endcase
end

// -----------------------------------------------------------------------------
// Negedge-Driven Framebuffer Interface Registers
// Ensures 10 ns setup and 10 ns hold margin around posedge CLK for FB.v
// -----------------------------------------------------------------------------
always @(negedge clk or posedge reset) begin
    if (reset) begin
        FB_CEN <= 1'b1;
        FB_WEN <= 1'b1;
        FB_A   <= 12'd0;
        FB_D   <= BG_COLOR;
    end else if (new_batch_init) begin
        FB_CEN <= 1'b1;
        FB_WEN <= 1'b1;
        FB_A   <= 12'd0;
        FB_D   <= BG_COLOR;
    end else begin
        FB_CEN <= ~rend_fb_wr_req;
        FB_WEN <= ~rend_fb_wr_req;
        if (rend_fb_wr_req) begin
            FB_A <= rend_fb_wr_addr;
            FB_D <= rend_fb_wr_data;
        end
    end
end

// -----------------------------------------------------------------------------
// Top-Level Coordinator Sequential Logic (Asynchronous Active-High Reset)
// -----------------------------------------------------------------------------
always @(posedge clk or posedge reset) begin
    if (reset) begin
        state_q            <= ST_RESET;
        ready              <= 1'b0;
        done               <= 1'b0;
        man_state_base_q   <= BASE_MAN_LIVE;
        man_state_q        <= MAN_LIVE;
        candy_hit_q        <= 1'b0;
        ghost_hit_q        <= 1'b0;
        fetch_start_q      <= 1'b0;
        reload_man_start_q <= 1'b0;
        coll_start_q       <= 1'b0;
        rend_start_q       <= 1'b0;
    end else begin
        state_q            <= state_d;
        ready              <= ready_d;
        done               <= done_d;
        man_state_base_q   <= man_state_base_d;
        man_state_q        <= man_state_d;
        candy_hit_q        <= candy_hit_d;
        ghost_hit_q        <= ghost_hit_d;
        fetch_start_q      <= fetch_start_d;
        reload_man_start_q <= reload_man_start_d;
        coll_start_q       <= coll_start_d;
        rend_start_q       <= rend_start_d;
    end
end

endmodule


// =============================================================================
// Module 2: SGDE_Object_Collector (Updated with B1 + B2c Candidate Masks)
// Responsibility: Sole owner of 20-entry object arrays and obj_count (0..20).
//                 Generates 19-bit candidate masks (gc_hit_mask[19:1] and
//                 flw_hit_mask[19:1]) for query pixel (query_x, query_y) using
//                 overflow-safe 7-bit unsigned coordinate comparisons.
// =============================================================================
module SGDE_Object_Collector (
    input             clk,
    input             reset,
    input             collect_en,
    input             batch_rearm,
    input             sprite,
    input      [1:0]  type,
    input      [5:0]  X,
    input      [5:0]  Y,
    input      [4:0]  rd_idx,
    input      [5:0]  query_x,
    input      [5:0]  query_y,
    output reg        batch_armed,
    output reg [4:0]  obj_count,
    output     [1:0]  rd_type,
    output     [5:0]  rd_x,
    output     [5:0]  rd_y,
    output     [5:0]  man_x,
    output     [5:0]  man_y,
    output reg [19:1] gc_hit_mask,
    output reg [19:1] flw_hit_mask
);

localparam [4:0] MAX_OBJECTS = 5'd20;

localparam [1:0] TYPE_MAN    = 2'b00;
localparam [1:0] TYPE_GHOST  = 2'b01;
localparam [1:0] TYPE_CANDY  = 2'b10;
localparam [1:0] TYPE_FLOWER = 2'b11;

reg [1:0] obj_type [0:19];
reg [5:0] obj_x    [0:19];
reg [5:0] obj_y    [0:19];
reg [4:0] obj_count_d;
reg       batch_armed_d;

wire [4:0] effective_count;
wire       accept_sprite;
wire [4:0] safe_rd_idx;

wire [6:0] qx_ext;
wire [6:0] qy_ext;
reg  [6:0] ox_start;
reg  [6:0] oy_start;
reg  [6:0] ox_end;
reg  [6:0] oy_end;
reg        obj_valid;
reg        obj_in_box;

integer k;
integer m;

assign effective_count = batch_armed ? 5'd0 : obj_count;
assign accept_sprite   = collect_en && sprite && (effective_count < MAX_OBJECTS);

assign safe_rd_idx = (rd_idx < MAX_OBJECTS) ? rd_idx : 5'd0;
assign rd_type     = obj_type[safe_rd_idx];
assign rd_x        = obj_x[safe_rd_idx];
assign rd_y        = obj_y[safe_rd_idx];

assign man_x       = obj_x[0];
assign man_y       = obj_y[0];

assign qx_ext      = {1'b0, query_x};
assign qy_ext      = {1'b0, query_y};

// B1 + B2c Candidate Mask Generation for Objects 1..19
// Uses explicit 7-bit unsigned arithmetic so obj_x/obj_y = 56 (+8 = 64) never wraps.
always @(*) begin
    gc_hit_mask  = 19'd0;
    flw_hit_mask = 19'd0;

    for (m = 1; m < 20; m = m + 1) begin
        obj_valid  = (m[4:0] < obj_count);
        ox_start   = {1'b0, obj_x[m]};
        oy_start   = {1'b0, obj_y[m]};
        ox_end     = ox_start + 7'd8;
        oy_end     = oy_start + 7'd8;
        obj_in_box = (qx_ext >= ox_start) && (qx_ext < ox_end) &&
                     (qy_ext >= oy_start) && (qy_ext < oy_end);

        if (obj_valid && obj_in_box) begin
            if ((obj_type[m] == TYPE_GHOST) || (obj_type[m] == TYPE_CANDY)) begin
                gc_hit_mask[m]  = 1'b1;
            end else if (obj_type[m] == TYPE_FLOWER) begin
                flw_hit_mask[m] = 1'b1;
            end
        end
    end
end

always @(*) begin
    obj_count_d   = obj_count;
    batch_armed_d = batch_armed;

    if (batch_rearm) begin
        batch_armed_d = 1'b1;
    end else if (accept_sprite) begin
        obj_count_d   = effective_count + 5'd1;
        batch_armed_d = 1'b0;
    end
end

always @(posedge clk or posedge reset) begin
    if (reset) begin
        obj_count   <= 5'd0;
        batch_armed <= 1'b0;
        for (k = 0; k < 20; k = k + 1) begin
            obj_type[k] <= 2'b00;
            obj_x[k]    <= 6'd0;
            obj_y[k]    <= 6'd0;
        end
    end else begin
        obj_count   <= obj_count_d;
        batch_armed <= batch_armed_d;
        if (accept_sprite) begin
            obj_type[effective_count] <= type;
            obj_x[effective_count]    <= X;
            obj_y[effective_count]    <= Y;
        end
    end
end

endmodule


// =============================================================================
// Module 3: SGDE_Sprite_Fetcher (B4-Man Full 13-bit Read Port Exposure)
// Responsibility: Sole owner of ROM address generation, synchronous ROM
//                 sequencing, traversal index (obj_idx), local pixel coordinates,
//                 and full 20x64x13-bit pixel storage (sprite_mem[0:1279]).
//                 Exposes full 13-bit man_rd_data from the existing Man read
//                 port without adding a third sprite_mem read port.
// =============================================================================
module SGDE_Sprite_Fetcher (
    input             clk,
    input             reset,
    input             fetch_start,
    input             reload_man_start,
    input             batch_clr,
    input      [4:0]  obj_count,
    input      [8:0]  man_state_base,
    input      [1:0]  obj_rd_type,
    input      [12:0] SR_Q,
    input      [5:0]  man_rd_pixel_idx,
    input      [4:0]  buf_rd_obj_idx,
    input      [5:0]  buf_rd_pixel_idx,
    output reg [4:0]  obj_idx,
    output reg        SR_CEN,
    output reg [8:0]  SR_A,
    output reg        rom_valid,
    output reg [4:0]  rom_valid_obj_idx,
    output reg [5:0]  rom_valid_pixel_idx,
    output reg [12:0] rom_data_reg,
    output reg [11:0] rom_rgb_reg,
    output reg        rom_mask_reg,
    output     [12:0] man_rd_data,
    output            man_rd_mask,
    output            man_rd_valid,
    output     [12:0] buf_rd_data,
    output            buf_rd_valid,
    output            fetch_busy,
    output reg        fetch_done
);

localparam [1:0] TYPE_MAN    = 2'b00;
localparam [1:0] TYPE_GHOST  = 2'b01;
localparam [1:0] TYPE_CANDY  = 2'b10;
localparam [1:0] TYPE_FLOWER = 2'b11;

localparam [8:0] BASE_MAN_LIVE = 9'd0;
localparam [8:0] BASE_GHOST    = 9'd192;
localparam [8:0] BASE_CANDY    = 9'd256;
localparam [8:0] BASE_FLOWER   = 9'd320;

localparam [2:0] FS_IDLE  = 3'd0;
localparam [2:0] FS_SETUP = 3'd1;
localparam [2:0] FS_REQ   = 3'd2;
localparam [2:0] FS_WAIT  = 3'd3;
localparam [2:0] FS_DONE  = 3'd4;

reg [2:0]  fetch_state_q, fetch_state_d;
reg [4:0]  obj_idx_d;
reg [2:0]  local_x_q, local_x_d;
reg [2:0]  local_y_q, local_y_d;
reg [8:0]  rom_base_q, rom_base_d;
reg        single_obj_mode_q, single_obj_mode_d;

reg [4:0]  inflight_obj_idx_q, inflight_obj_idx_d;
reg [5:0]  inflight_pixel_idx_q, inflight_pixel_idx_d;

reg        rom_valid_d;
reg [4:0]  rom_valid_obj_idx_d;
reg [5:0]  rom_valid_pixel_idx_d;
reg [12:0] rom_data_reg_d;
reg [11:0] rom_rgb_reg_d;
reg        rom_mask_reg_d;
reg        fetch_done_d;
reg        all_fetched_q, all_fetched_d;

reg [12:0] sprite_mem [0:1279];
reg        sprite_buf_we;

wire [5:0]  curr_pixel_idx;
wire        req_active;
wire [8:0]  req_addr_comb;
wire [4:0]  safe_req_obj_idx;
wire [4:0]  safe_cap_obj_idx;
wire [10:0] sprite_wr_addr;
wire [4:0]  safe_rd_obj_idx;
wire [10:0] sprite_rd_addr;
wire [10:0] man_rd_addr;

reg  [8:0]  decoded_rom_base;

assign curr_pixel_idx   = {local_y_q, local_x_q};
assign req_active       = (fetch_state_q == FS_REQ);
assign req_addr_comb    = rom_base_q + {3'b000, local_y_q, local_x_q};
assign safe_req_obj_idx = (obj_idx < 5'd20) ? obj_idx : 5'd0;
assign safe_cap_obj_idx = (inflight_obj_idx_q < 5'd20) ? inflight_obj_idx_q : 5'd0;
assign sprite_wr_addr   = {safe_cap_obj_idx, inflight_pixel_idx_q};

// Port 1: Dedicated Man (Object 0) read port (full 13-bit word + mask + valid)
assign man_rd_addr      = {5'd0, man_rd_pixel_idx};
assign man_rd_data      = sprite_mem[man_rd_addr];
assign man_rd_mask      = man_rd_data[0];
assign man_rd_valid     = all_fetched_q && (obj_count > 5'd0);

// Port 2: General candidate read port
assign safe_rd_obj_idx  = (buf_rd_obj_idx < 5'd20) ? buf_rd_obj_idx : 5'd0;
assign sprite_rd_addr   = {safe_rd_obj_idx, buf_rd_pixel_idx};
assign buf_rd_data      = sprite_mem[sprite_rd_addr];
assign buf_rd_valid     = all_fetched_q && (buf_rd_obj_idx < obj_count);

assign fetch_busy       = (fetch_state_q != FS_IDLE);

// Unified ROM base decode
always @(*) begin
    case (obj_rd_type)
        TYPE_MAN:    decoded_rom_base = man_state_base;
        TYPE_GHOST:  decoded_rom_base = BASE_GHOST;
        TYPE_CANDY:  decoded_rom_base = BASE_CANDY;
        TYPE_FLOWER: decoded_rom_base = BASE_FLOWER;
        default:     decoded_rom_base = BASE_MAN_LIVE;
    endcase
end

always @(*) begin
    fetch_state_d         = fetch_state_q;
    obj_idx_d             = obj_idx;
    local_x_d             = local_x_q;
    local_y_d             = local_y_q;
    rom_base_d            = rom_base_q;
    single_obj_mode_d     = single_obj_mode_q;
    inflight_obj_idx_d    = inflight_obj_idx_q;
    inflight_pixel_idx_d  = inflight_pixel_idx_q;

    rom_valid_d           = 1'b0;
    rom_valid_obj_idx_d   = rom_valid_obj_idx;
    rom_valid_pixel_idx_d = rom_valid_pixel_idx;
    rom_data_reg_d        = rom_data_reg;
    rom_rgb_reg_d         = rom_rgb_reg;
    rom_mask_reg_d        = rom_mask_reg;
    sprite_buf_we         = 1'b0;
    fetch_done_d          = 1'b0;
    all_fetched_d         = all_fetched_q;

    if (batch_clr) begin
        fetch_state_d         = FS_IDLE;
        obj_idx_d             = 5'd0;
        local_x_d             = 3'd0;
        local_y_d             = 3'd0;
        rom_base_d            = BASE_MAN_LIVE;
        single_obj_mode_d     = 1'b0;
        inflight_obj_idx_d    = 5'd0;
        inflight_pixel_idx_d  = 6'd0;
        rom_valid_d           = 1'b0;
        rom_valid_obj_idx_d   = 5'd0;
        rom_valid_pixel_idx_d = 6'd0;
        fetch_done_d          = 1'b0;
        all_fetched_d         = 1'b0;
    end else begin
        case (fetch_state_q)
            FS_IDLE: begin
                fetch_done_d = 1'b0;
                if (fetch_start) begin
                    obj_idx_d         = 5'd0;
                    local_x_d         = 3'd0;
                    local_y_d         = 3'd0;
                    single_obj_mode_d = 1'b0;
                    all_fetched_d     = 1'b0;
                    if (obj_count == 5'd0) begin
                        all_fetched_d = 1'b1;
                        fetch_done_d  = 1'b1;
                        fetch_state_d = FS_DONE;
                    end else begin
                        fetch_state_d = FS_SETUP;
                    end
                end else if (reload_man_start) begin
                    obj_idx_d         = 5'd0;
                    local_x_d         = 3'd0;
                    local_y_d         = 3'd0;
                    single_obj_mode_d = 1'b1;
                    fetch_state_d     = FS_SETUP;
                end
            end

            FS_SETUP: begin
                if (obj_idx >= obj_count) begin
                    all_fetched_d = 1'b1;
                    fetch_done_d  = 1'b1;
                    fetch_state_d = FS_DONE;
                end else begin
                    rom_base_d    = decoded_rom_base;
                    local_x_d     = 3'd0;
                    local_y_d     = 3'd0;
                    fetch_state_d = FS_REQ;
                end
            end

            FS_REQ: begin
                inflight_obj_idx_d   = safe_req_obj_idx;
                inflight_pixel_idx_d = curr_pixel_idx;
                fetch_state_d        = FS_WAIT;
            end

            FS_WAIT: begin
                rom_valid_d           = 1'b1;
                rom_valid_obj_idx_d   = safe_cap_obj_idx;
                rom_valid_pixel_idx_d = inflight_pixel_idx_q;
                rom_data_reg_d        = SR_Q;
                rom_rgb_reg_d         = SR_Q[12:1];
                rom_mask_reg_d        = SR_Q[0];
                sprite_buf_we         = 1'b1;

                if ((local_y_q == 3'd7) && (local_x_q == 3'd7)) begin
                    local_x_d = 3'd0;
                    local_y_d = 3'd0;
                    if (single_obj_mode_q || ((obj_idx + 5'd1) >= obj_count)) begin
                        if (!single_obj_mode_q) begin
                            obj_idx_d = obj_idx + 5'd1;
                        end
                        all_fetched_d = 1'b1;
                        fetch_done_d  = 1'b1;
                        fetch_state_d = FS_DONE;
                    end else begin
                        obj_idx_d     = obj_idx + 5'd1;
                        fetch_state_d = FS_SETUP;
                    end
                end else if (local_x_q == 3'd7) begin
                    local_x_d     = 3'd0;
                    local_y_d     = local_y_q + 3'd1;
                    fetch_state_d = FS_REQ;
                end else begin
                    local_x_d     = local_x_q + 3'd1;
                    fetch_state_d = FS_REQ;
                end
            end

            FS_DONE: begin
                fetch_done_d      = 1'b0;
                single_obj_mode_d = 1'b0;
                fetch_state_d     = FS_IDLE;
            end

            default: begin
                fetch_state_d = FS_IDLE;
            end
        endcase
    end
end

always @(negedge clk or posedge reset) begin
    if (reset) begin
        SR_CEN <= 1'b1;
        SR_A   <= 9'd0;
    end else if (batch_clr) begin
        SR_CEN <= 1'b1;
        SR_A   <= 9'd0;
    end else begin
        SR_CEN <= ~req_active;
        if (req_active) begin
            SR_A <= req_addr_comb;
        end
    end
end

// Control registers with asynchronous active-high reset
always @(posedge clk or posedge reset) begin
    if (reset) begin
        fetch_state_q        <= FS_IDLE;
        obj_idx              <= 5'd0;
        local_x_q            <= 3'd0;
        local_y_q            <= 3'd0;
        rom_base_q           <= BASE_MAN_LIVE;
        single_obj_mode_q    <= 1'b0;
        inflight_obj_idx_q   <= 5'd0;
        inflight_pixel_idx_q <= 6'd0;
        rom_valid            <= 1'b0;
        rom_valid_obj_idx    <= 5'd0;
        rom_valid_pixel_idx  <= 6'd0;
        rom_data_reg         <= 13'd0;
        rom_rgb_reg          <= 12'd0;
        rom_mask_reg         <= 1'b0;
        fetch_done           <= 1'b0;
        all_fetched_q        <= 1'b0;
    end else begin
        fetch_state_q        <= fetch_state_d;
        obj_idx              <= obj_idx_d;
        local_x_q            <= local_x_d;
        local_y_q            <= local_y_d;
        rom_base_q           <= rom_base_d;
        single_obj_mode_q    <= single_obj_mode_d;
        inflight_obj_idx_q   <= inflight_obj_idx_d;
        inflight_pixel_idx_q <= inflight_pixel_idx_d;
        rom_valid            <= rom_valid_d;
        rom_valid_obj_idx    <= rom_valid_obj_idx_d;
        rom_valid_pixel_idx  <= rom_valid_pixel_idx_d;
        rom_data_reg         <= rom_data_reg_d;
        rom_rgb_reg          <= rom_rgb_reg_d;
        rom_mask_reg         <= rom_mask_reg_d;
        fetch_done           <= fetch_done_d;
        all_fetched_q        <= all_fetched_d;
    end
end

// Separate synchronous write block for sprite_mem
always @(posedge clk) begin
    if (!reset && sprite_buf_we) begin
        sprite_mem[sprite_wr_addr] <= SR_Q;
    end
end

endmodule


// =============================================================================
// Module 4: SGDE_Collision_Engine (FROZEN Task 3 Baseline - UNCHANGED)
// Responsibility: Multi-cycle pixel-accurate collision analyzer.
// =============================================================================
module SGDE_Collision_Engine (
    input            clk,
    input            reset,
    input            coll_start,
    input            batch_clr,
    input      [4:0] obj_count,
    input      [5:0] man_x,
    input      [5:0] man_y,
    input            man_rd_mask,
    input            man_rd_valid,
    input      [1:0] cand_type,
    input      [5:0] cand_x,
    input      [5:0] cand_y,
    input            cand_rd_mask,
    input            cand_rd_valid,
    output     [5:0] man_rd_pixel_idx,
    output reg [4:0] obj_idx,
    output     [5:0] target_pixel_idx,
    output reg       candy_hit,
    output reg       ghost_hit,
    output reg [1:0] man_state,
    output reg [8:0] man_state_base,
    output reg       rd_invalid_err,
    output           coll_busy,
    output reg       coll_done
);

localparam [1:0] TYPE_MAN    = 2'b00;
localparam [1:0] TYPE_GHOST  = 2'b01;
localparam [1:0] TYPE_CANDY  = 2'b10;
localparam [1:0] TYPE_FLOWER = 2'b11;

localparam [1:0] MAN_LIVE    = 2'd0;
localparam [1:0] MAN_HAPPY   = 2'd1;
localparam [1:0] MAN_DIE     = 2'd2;

localparam [8:0] BASE_MAN_LIVE  = 9'd0;
localparam [8:0] BASE_MAN_HAPPY = 9'd64;
localparam [8:0] BASE_MAN_DIE   = 9'd128;

localparam [2:0] CS_IDLE     = 3'd0;
localparam [2:0] CS_MAN_PIX  = 3'd1;
localparam [2:0] CS_OBJ_EVAL = 3'd2;
localparam [2:0] CS_DECIDE   = 3'd3;
localparam [2:0] CS_DONE     = 3'd4;

reg [2:0] cs_state_q, cs_state_d;
reg [2:0] mx_q, mx_d;
reg [2:0] my_q, my_d;
reg [4:0] obj_idx_d;

reg       candy_hit_d;
reg       ghost_hit_d;
reg [1:0] man_state_d;
reg [8:0] man_state_base_d;
reg       rd_invalid_err_d;
reg       coll_done_d;

wire [6:0] man_abs_x;
wire [6:0] man_abs_y;
wire [6:0] cand_x_start;
wire [6:0] cand_y_start;
wire [6:0] cand_x_end;
wire [6:0] cand_y_end;
wire       coord_overlap;
wire [6:0] diff_x;
wire [6:0] diff_y;
wire [2:0] ox;
wire [2:0] oy;

assign man_rd_pixel_idx = {my_q, mx_q};

assign man_abs_x        = {1'b0, man_x} + {4'b0000, mx_q};
assign man_abs_y        = {1'b0, man_y} + {4'b0000, my_q};
assign cand_x_start     = {1'b0, cand_x};
assign cand_y_start     = {1'b0, cand_y};
assign cand_x_end       = cand_x_start + 7'd8;
assign cand_y_end       = cand_y_start + 7'd8;

assign coord_overlap    = (man_abs_x >= cand_x_start) && (man_abs_x < cand_x_end) &&
                          (man_abs_y >= cand_y_start) && (man_abs_y < cand_y_end);

assign diff_x           = man_abs_x - cand_x_start;
assign diff_y           = man_abs_y - cand_y_start;
assign ox               = coord_overlap ? diff_x[2:0] : 3'd0;
assign oy               = coord_overlap ? diff_y[2:0] : 3'd0;
assign target_pixel_idx = {oy, ox};

assign coll_busy        = (cs_state_q != CS_IDLE);

always @(*) begin
    cs_state_d       = cs_state_q;
    mx_d             = mx_q;
    my_d             = my_q;
    obj_idx_d        = obj_idx;
    candy_hit_d      = candy_hit;
    ghost_hit_d      = ghost_hit;
    man_state_d      = man_state;
    man_state_base_d = man_state_base;
    rd_invalid_err_d = rd_invalid_err;
    coll_done_d      = 1'b0;

    if (batch_clr) begin
        cs_state_d       = CS_IDLE;
        mx_d             = 3'd0;
        my_d             = 3'd0;
        obj_idx_d        = 5'd1;
        candy_hit_d      = 1'b0;
        ghost_hit_d      = 1'b0;
        man_state_d      = MAN_LIVE;
        man_state_base_d = BASE_MAN_LIVE;
        rd_invalid_err_d = 1'b0;
        coll_done_d      = 1'b0;
    end else begin
        case (cs_state_q)
            CS_IDLE: begin
                coll_done_d = 1'b0;
                if (coll_start) begin
                    mx_d             = 3'd0;
                    my_d             = 3'd0;
                    obj_idx_d        = 5'd1;
                    candy_hit_d      = 1'b0;
                    ghost_hit_d      = 1'b0;
                    man_state_d      = MAN_LIVE;
                    man_state_base_d = BASE_MAN_LIVE;
                    rd_invalid_err_d = 1'b0;
                    if (obj_count <= 5'd1) begin
                        cs_state_d = CS_DECIDE;
                    end else begin
                        cs_state_d = CS_MAN_PIX;
                    end
                end
            end

            CS_MAN_PIX: begin
                if (!man_rd_valid) begin
                    rd_invalid_err_d = 1'b1;
                end

                if (man_rd_valid && (man_rd_mask == 1'b1)) begin
                    obj_idx_d  = 5'd1;
                    cs_state_d = CS_OBJ_EVAL;
                end else begin
                    if ((my_q == 3'd7) && (mx_q == 3'd7)) begin
                        mx_d       = 3'd0;
                        my_d       = 3'd0;
                        cs_state_d = CS_DECIDE;
                    end else if (mx_q == 3'd7) begin
                        mx_d       = 3'd0;
                        my_d       = my_q + 3'd1;
                        cs_state_d = CS_MAN_PIX;
                    end else begin
                        mx_d       = mx_q + 3'd1;
                        cs_state_d = CS_MAN_PIX;
                    end
                end
            end

            CS_OBJ_EVAL: begin
                if (!man_rd_valid || !cand_rd_valid) begin
                    rd_invalid_err_d = 1'b1;
                end

                if (man_rd_valid && cand_rd_valid && coord_overlap && (cand_rd_mask == 1'b1)) begin
                    if (cand_type == TYPE_CANDY) begin
                        candy_hit_d = 1'b1;
                    end else if (cand_type == TYPE_GHOST) begin
                        ghost_hit_d = 1'b1;
                    end
                end

                if ((obj_idx + 5'd1) < obj_count) begin
                    obj_idx_d  = obj_idx + 5'd1;
                    cs_state_d = CS_OBJ_EVAL;
                end else begin
                    obj_idx_d = 5'd1;
                    if ((my_q == 3'd7) && (mx_q == 3'd7)) begin
                        mx_d       = 3'd0;
                        my_d       = 3'd0;
                        cs_state_d = CS_DECIDE;
                    end else if (mx_q == 3'd7) begin
                        mx_d       = 3'd0;
                        my_d       = my_q + 3'd1;
                        cs_state_d = CS_MAN_PIX;
                    end else begin
                        mx_d       = mx_q + 3'd1;
                        cs_state_d = CS_MAN_PIX;
                    end
                end
            end

            CS_DECIDE: begin
                if (candy_hit) begin
                    man_state_d      = MAN_HAPPY;
                    man_state_base_d = BASE_MAN_HAPPY;
                end else if (ghost_hit) begin
                    man_state_d      = MAN_DIE;
                    man_state_base_d = BASE_MAN_DIE;
                end else begin
                    man_state_d      = MAN_LIVE;
                    man_state_base_d = BASE_MAN_LIVE;
                end
                coll_done_d = 1'b1;
                cs_state_d  = CS_DONE;
            end

            CS_DONE: begin
                coll_done_d = 1'b0;
                cs_state_d  = CS_IDLE;
            end

            default: begin
                cs_state_d = CS_IDLE;
            end
        endcase
    end
end

always @(posedge clk or posedge reset) begin
    if (reset) begin
        cs_state_q     <= CS_IDLE;
        mx_q           <= 3'd0;
        my_q           <= 3'd0;
        obj_idx        <= 5'd1;
        candy_hit      <= 1'b0;
        ghost_hit      <= 1'b0;
        man_state      <= MAN_LIVE;
        man_state_base <= BASE_MAN_LIVE;
        rd_invalid_err <= 1'b0;
        coll_done      <= 1'b0;
    end else begin
        cs_state_q     <= cs_state_d;
        mx_q           <= mx_d;
        my_q           <= my_d;
        obj_idx        <= obj_idx_d;
        candy_hit      <= candy_hit_d;
        ghost_hit      <= ghost_hit_d;
        man_state      <= man_state_d;
        man_state_base <= man_state_base_d;
        rd_invalid_err <= rd_invalid_err_d;
        coll_done      <= coll_done_d;
    end
end

endmodule


// =============================================================================
// Module 5: SGDE_Framebuffer_Renderer (Optimized in rev_014: B1+B3+B4-Man+B2c)
// Responsibility: Walks all 4096 screen pixels (screen_y = 0..63, screen_x = 0..63)
//                 in row-major order (FB_A = {screen_y, screen_x} = 0..4095).
//
// Microarchitecture Highlights:
// 1. B1 + B2c Lookahead Pre-Decode:
//    Queries SGDE_Object_Collector with the next pixel coordinate (query_x, query_y)
//    to obtain gc_hit_mask[19:1] and flw_hit_mask[19:1]. A priority encoder
//    pre-selects the highest-priority valid candidate and latches it into
//    (obj_idx, cand_active_q, rem_gc_q, rem_flw_q) BEFORE the pixel evaluation
//    cycle begins.
// 2. Parallel Alternate Priority Pre-Decode:
//    During an active pixel evaluation cycle, a second priority encoder
//    simultaneously pre-decodes the next fallback candidate from the registered
//    queues (rem_gc_q, rem_flw_q) in parallel with the sprite_mem read.
//    Thus, sprite_mem read and Priority Encoding never sit in series!
// 3. B4-Man Same-Cycle Evaluation:
//    On the first cycle of each pixel (pixel_first_cyc_q == 1), evaluates
//    Man (Object 0) via man_rd_data[12:0] in parallel with the top non-Man
//    candidate (obj_idx). If Man is in-box and opaque, Man wins immediately;
//    otherwise, the top non-Man candidate result is used in the same cycle.
// 4. B3 Early Termination:
//    Immediately finishes the pixel in 1 cycle whenever Man hits, the candidate
//    hits, or no remaining candidate exists (rem_gc_q == 0 && rem_flw_q == 0).
// =============================================================================
module SGDE_Framebuffer_Renderer (
    input             clk,
    input             reset,
    input             rend_start,
    input             batch_clr,
    input      [4:0]  obj_count,
    input      [5:0]  man_x,
    input      [5:0]  man_y,
    input      [12:0] man_rd_data,
    input             man_rd_valid,
    input      [5:0]  cand_x,
    input      [5:0]  cand_y,
    input      [12:0] buf_rd_data,
    input             buf_rd_valid,
    input      [19:1] gc_hit_mask,
    input      [19:1] flw_hit_mask,
    output     [5:0]  query_x,
    output     [5:0]  query_y,
    output     [5:0]  man_rd_pixel_idx,
    output reg [4:0]  obj_idx,
    output     [5:0]  pixel_idx,
    output reg        fb_wr_req,
    output reg [11:0] fb_wr_addr,
    output reg [11:0] fb_wr_data,
    output            rend_busy,
    output reg        rend_done
);

localparam [11:0] BG_COLOR = 12'hCF0;

// 4-State Renderer FSM
localparam [1:0] RS_IDLE       = 2'd0;
localparam [1:0] RS_LAYER_EVAL = 2'd1;
localparam [1:0] RS_FINAL_WAIT = 2'd2;
localparam [1:0] RS_DONE       = 2'd3;

reg [1:0]  rs_state_q, rs_state_d;
reg [5:0]  screen_x_q, screen_x_d;
reg [5:0]  screen_y_q, screen_y_d;
reg [4:0]  obj_idx_d;

// Registered candidate queues and status flags for current pixel
reg        cand_active_q, cand_active_d;
reg        pixel_first_cyc_q, pixel_first_cyc_d;
reg [19:1] rem_gc_q, rem_gc_d;
reg [19:1] rem_flw_q, rem_flw_d;

// Next-state signals for pipelined FB write output registers
reg        fb_wr_req_d;
reg [11:0] fb_wr_addr_d;
reg [11:0] fb_wr_data_d;
reg        rend_done_d;

// -----------------------------------------------------------------------------
// Coordinate Arithmetic for Man (Port 1) and Active Candidate (Port 2)
// -----------------------------------------------------------------------------
wire [6:0] scr_x_ext;
wire [6:0] scr_y_ext;

// Man (Object 0) 7-bit unsigned bounding box & local coordinates
wire [6:0] man_x_start;
wire [6:0] man_y_start;
wire [6:0] man_x_end;
wire [6:0] man_y_end;
wire       man_in_box;
wire [6:0] man_diff_x;
wire [6:0] man_diff_y;
wire [2:0] man_local_x;
wire [2:0] man_local_y;

// Active non-Man candidate 7-bit unsigned local coordinates
wire [6:0] cand_x_start;
wire [6:0] cand_y_start;
wire [6:0] cand_x_end;
wire [6:0] cand_y_end;
wire       cand_in_box;
wire [6:0] cand_diff_x;
wire [6:0] cand_diff_y;
wire [2:0] cand_local_x;
wire [2:0] cand_local_y;

// Next screen pixel coordinates (Row-Major order)
wire [5:0] next_screen_x;
wire [5:0] next_screen_y;
wire       is_last_pixel;

// Hit and termination flags
wire       man_opaque_hit;
wire       cand_opaque_hit;
wire       no_more_cands;
wire       pixel_done;

assign scr_x_ext        = {1'b0, screen_x_q};
assign scr_y_ext        = {1'b0, screen_y_q};

// Man coordinate calculations
assign man_x_start      = {1'b0, man_x};
assign man_y_start      = {1'b0, man_y};
assign man_x_end        = man_x_start + 7'd8;
assign man_y_end        = man_y_start + 7'd8;
assign man_in_box       = (scr_x_ext >= man_x_start) && (scr_x_ext < man_x_end) &&
                          (scr_y_ext >= man_y_start) && (scr_y_ext < man_y_end);
assign man_diff_x       = scr_x_ext - man_x_start;
assign man_diff_y       = scr_y_ext - man_y_start;
assign man_local_x      = man_in_box ? man_diff_x[2:0] : 3'd0;
assign man_local_y      = man_in_box ? man_diff_y[2:0] : 3'd0;
assign man_rd_pixel_idx = {man_local_y, man_local_x};

// Candidate coordinate calculations
assign cand_x_start     = {1'b0, cand_x};
assign cand_y_start     = {1'b0, cand_y};
assign cand_x_end       = cand_x_start + 7'd8;
assign cand_y_end       = cand_y_start + 7'd8;
assign cand_in_box      = (scr_x_ext >= cand_x_start) && (scr_x_ext < cand_x_end) &&
                          (scr_y_ext >= cand_y_start) && (scr_y_ext < cand_y_end);
assign cand_diff_x      = scr_x_ext - cand_x_start;
assign cand_diff_y      = scr_y_ext - cand_y_start;
assign cand_local_x     = cand_in_box ? cand_diff_x[2:0] : 3'd0;
assign cand_local_y     = cand_in_box ? cand_diff_y[2:0] : 3'd0;
assign pixel_idx        = {cand_local_y, cand_local_x};

// Next pixel coordinates and Collector Lookahead Query Coordinates
assign next_screen_x    = (screen_x_q == 6'd63) ? 6'd0 : (screen_x_q + 6'd1);
assign next_screen_y    = (screen_x_q == 6'd63) ? (screen_y_q + 6'd1) : screen_y_q;
assign is_last_pixel    = (screen_y_q == 6'd63) && (screen_x_q == 6'd63);

// In RS_IDLE, query pixel (0,0); in RS_LAYER_EVAL, lookahead-query (next_screen_x, next_screen_y)
assign query_x          = (rs_state_q == RS_IDLE) ? 6'd0 : next_screen_x;
assign query_y          = (rs_state_q == RS_IDLE) ? 6'd0 : next_screen_y;

// B4-Man & Candidate Opaque Hit Qualification
assign man_opaque_hit   = pixel_first_cyc_q && man_rd_valid && man_in_box && (man_rd_data[0] == 1'b1);
assign cand_opaque_hit  = cand_active_q && buf_rd_valid && cand_in_box && (buf_rd_data[0] == 1'b1);
assign no_more_cands    = (rem_gc_q == 19'd0) && (rem_flw_q == 19'd0);
assign pixel_done       = man_opaque_hit || cand_opaque_hit || no_more_cands;

assign rend_busy        = (rs_state_q != RS_IDLE);

// -----------------------------------------------------------------------------
// Priority Encoder 1: Pre-Decode First Candidate for Next Pixel (from Collector masks)
// Priority Order: Ghost/Candy (19 down to 1) > Flower (19 down to 1)
// -----------------------------------------------------------------------------
reg        init_cand_active;
reg [4:0]  init_obj_idx;
reg [19:1] init_rem_gc;
reg [19:1] init_rem_flw;
integer i1;

always @(*) begin
    init_cand_active = 1'b0;
    init_obj_idx     = 5'd1;
    init_rem_gc      = gc_hit_mask;
    init_rem_flw     = flw_hit_mask;

    if (|gc_hit_mask) begin
        // Ascending loop where later assignments override earlier ones -> highest index wins
        for (i1 = 1; i1 < 20; i1 = i1 + 1) begin
            if (gc_hit_mask[i1]) begin
                init_cand_active = 1'b1;
                init_obj_idx     = i1[4:0];
            end
        end
        init_rem_gc[init_obj_idx] = 1'b0;
    end else if (|flw_hit_mask) begin
        for (i1 = 1; i1 < 20; i1 = i1 + 1) begin
            if (flw_hit_mask[i1]) begin
                init_cand_active = 1'b1;
                init_obj_idx     = i1[4:0];
            end
        end
        init_rem_flw[init_obj_idx] = 1'b0;
    end
end

// -----------------------------------------------------------------------------
// Priority Encoder 2: Pre-Decode Next Fallback Candidate for Current Pixel
// Operates in parallel with sprite_mem read directly from registered rem_gc_q/rem_flw_q
// -----------------------------------------------------------------------------
reg        step_cand_active;
reg [4:0]  step_obj_idx;
reg [19:1] step_rem_gc;
reg [19:1] step_rem_flw;
integer i2;

always @(*) begin
    step_cand_active = 1'b0;
    step_obj_idx     = 5'd1;
    step_rem_gc      = rem_gc_q;
    step_rem_flw     = rem_flw_q;

    if (|rem_gc_q) begin
        for (i2 = 1; i2 < 20; i2 = i2 + 1) begin
            if (rem_gc_q[i2]) begin
                step_cand_active = 1'b1;
                step_obj_idx     = i2[4:0];
            end
        end
        step_rem_gc[step_obj_idx] = 1'b0;
    end else if (|rem_flw_q) begin
        for (i2 = 1; i2 < 20; i2 = i2 + 1) begin
            if (rem_flw_q[i2]) begin
                step_cand_active = 1'b1;
                step_obj_idx     = i2[4:0];
            end
        end
        step_rem_flw[step_obj_idx] = 1'b0;
    end
end

// -----------------------------------------------------------------------------
// Renderer FSM Next-State and Output Control Logic
// -----------------------------------------------------------------------------
always @(*) begin
    rs_state_d        = rs_state_q;
    screen_x_d        = screen_x_q;
    screen_y_d        = screen_y_q;
    obj_idx_d         = obj_idx;
    cand_active_d     = cand_active_q;
    pixel_first_cyc_d = pixel_first_cyc_q;
    rem_gc_d          = rem_gc_q;
    rem_flw_d         = rem_flw_q;

    fb_wr_req_d       = 1'b0;
    fb_wr_addr_d      = fb_wr_addr;
    fb_wr_data_d      = fb_wr_data;
    rend_done_d       = 1'b0;

    if (batch_clr) begin
        rs_state_d        = RS_IDLE;
        screen_x_d        = 6'd0;
        screen_y_d        = 6'd0;
        obj_idx_d         = 5'd1;
        cand_active_d     = 1'b0;
        pixel_first_cyc_d = 1'b0;
        rem_gc_d          = 19'd0;
        rem_flw_d         = 19'd0;
        fb_wr_req_d       = 1'b0;
        fb_wr_addr_d      = 12'd0;
        fb_wr_data_d      = BG_COLOR;
        rend_done_d       = 1'b0;
    end else begin
        case (rs_state_q)
            RS_IDLE: begin
                rend_done_d = 1'b0;
                fb_wr_req_d = 1'b0;
                if (rend_start) begin
                    // Initialize pixel (0,0) and latch its pre-decoded first candidate
                    screen_x_d        = 6'd0;
                    screen_y_d        = 6'd0;
                    obj_idx_d         = init_obj_idx;
                    cand_active_d     = init_cand_active;
                    pixel_first_cyc_d = 1'b1;
                    rem_gc_d          = init_rem_gc;
                    rem_flw_d         = init_rem_flw;
                    rs_state_d        = RS_LAYER_EVAL;
                end
            end

            RS_LAYER_EVAL: begin
                if (pixel_done) begin
                    // Commit current pixel write to registered FB output stage
                    fb_wr_req_d  = 1'b1;
                    fb_wr_addr_d = {screen_y_q, screen_x_q};

                    // Priority: Man (Object 0) > Active Non-Man Candidate > Background
                    if (man_opaque_hit) begin
                        fb_wr_data_d = man_rd_data[12:1];
                    end else if (cand_opaque_hit) begin
                        fb_wr_data_d = buf_rd_data[12:1];
                    end else begin
                        fb_wr_data_d = BG_COLOR;
                    end

                    if (is_last_pixel) begin
                        cand_active_d     = 1'b0;
                        pixel_first_cyc_d = 1'b0;
                        rs_state_d        = RS_FINAL_WAIT;
                    end else begin
                        // Advance to next pixel and latch its pre-decoded first candidate
                        screen_x_d        = next_screen_x;
                        screen_y_d        = next_screen_y;
                        obj_idx_d         = init_obj_idx;
                        cand_active_d     = init_cand_active;
                        pixel_first_cyc_d = 1'b1;
                        rem_gc_d          = init_rem_gc;
                        rem_flw_d         = init_rem_flw;
                        rs_state_d        = RS_LAYER_EVAL;
                    end
                end else begin
                    // Current candidate was transparent and more candidates remain for this pixel:
                    // Latch the pre-decoded fallback candidate from (rem_gc_q, rem_flw_q)
                    fb_wr_req_d       = 1'b0;
                    obj_idx_d         = step_obj_idx;
                    cand_active_d     = step_cand_active;
                    pixel_first_cyc_d = 1'b0;
                    rem_gc_d          = step_rem_gc;
                    rem_flw_d         = step_rem_flw;
                    rs_state_d        = RS_LAYER_EVAL;
                end
            end

            // Drain final write (address 4095) through negedge FB registers and posedge FB.v sampling
            RS_FINAL_WAIT: begin
                fb_wr_req_d = 1'b0;
                rend_done_d = 1'b1;
                rs_state_d  = RS_DONE;
            end

            RS_DONE: begin
                fb_wr_req_d = 1'b0;
                rend_done_d = 1'b0;
                rs_state_d  = RS_IDLE;
            end

            default: begin
                rs_state_d = RS_IDLE;
            end
        endcase
    end
end

always @(posedge clk or posedge reset) begin
    if (reset) begin
        rs_state_q        <= RS_IDLE;
        screen_x_q        <= 6'd0;
        screen_y_q        <= 6'd0;
        obj_idx           <= 5'd1;
        cand_active_q     <= 1'b0;
        pixel_first_cyc_q <= 1'b0;
        rem_gc_q          <= 19'd0;
        rem_flw_q         <= 19'd0;
        fb_wr_req         <= 1'b0;
        fb_wr_addr        <= 12'd0;
        fb_wr_data        <= BG_COLOR;
        rend_done         <= 1'b0;
    end else begin
        rs_state_q        <= rs_state_d;
        screen_x_q        <= screen_x_d;
        screen_y_q        <= screen_y_d;
        obj_idx           <= obj_idx_d;
        cand_active_q     <= cand_active_d;
        pixel_first_cyc_q <= pixel_first_cyc_d;
        rem_gc_q          <= rem_gc_d;
        rem_flw_q         <= rem_flw_d;
        fb_wr_req         <= fb_wr_req_d;
        fb_wr_addr        <= fb_wr_addr_d;
        fb_wr_data        <= fb_wr_data_d;
        rend_done         <= rend_done_d;
    end
end

endmodule
=======
module SGDE ( ready, done, clk, reset, sprite, start, type, X, Y, SR_CEN, SR_A, SR_Q, FB_CEN, FB_WEN, FB_A, FB_D, FB_Q);

input clk, reset, sprite, start;
input [1:0] type;
input [5:0] X, Y;
input [12:0] SR_Q;
input [11:0] FB_Q;

output ready, done;
output SR_CEN, FB_CEN, FB_WEN;
output [8:0] SR_A;
output [11:0] FB_A, FB_D;

endmodule
>>>>>>> Stashed changes
