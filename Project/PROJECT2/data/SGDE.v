`timescale 100ps/1ps

module SGDE (ready, done, clk, reset, sprite, start, type, X, Y,
             SR_CEN, SR_A, SR_Q, FB_CEN, FB_WEN, FB_A, FB_D, FB_Q);

input clk, reset, sprite, start;
input [1:0] type;
input [5:0] X, Y;
input [12:0] SR_Q;
input [11:0] FB_Q;

output ready, done;
output reg SR_CEN, FB_CEN, FB_WEN;
output reg [8:0] SR_A;
output reg [11:0] FB_A, FB_D;
reg    done;

localparam [11:0] BACKGROUND = 12'hCF0;
localparam [4:0]  NSPR       = 5'd20;

// ---------------- state encoding -------------------------------------------
localparam [3:0] S_IDLE  = 4'd0,
                 S_FILL  = 4'd1,
                 S_PAIR  = 4'd2,
                 S_SCAN  = 4'd3,
                 S_WSCAN = 4'd4,
                 S_WOTH  = 4'd5,
                 S_MST   = 4'd6,
                 S_DSEL  = 4'd7,
                 S_DREAD = 4'd8,
                 S_DWAIT = 4'd9,
                 S_DONE  = 4'd10;

reg [3:0]  state;
reg [13:0] spr [0:19];           // {X[5:0], Y[5:0], type[1:0]}
reg [4:0]  n;                    // number of stored sprites
reg [1:0]  man_state;            // 0 live, 1 happy, 2 die
reg [11:0] fill_addr;
reg [4:0]  pair_i, di;
reg [2:0]  sxs, sys;             // scan counters (window relative)
reg [2:0]  sx0, sy0;             // window origin in Man-local coords
reg [3:0]  sw, sh;               // window size (1..8)
reg [2:0]  oth0x, oth0y;         // window origin in other-local coords
reg [2:0]  q_oth;                // ROM base (in 64-word units) of the other
reg [5:0]  pix;
reg [1:0]  pass;
reg [5:0]  cur_x, cur_y;
reg [2:0]  cur_q;
reg        hit_c, hit_g;

reg        srmask;               // SR_Q[0] sampled on the falling edge

assign ready = (state == S_IDLE) && !reset;

// ---------------- sprite field extraction ----------------------------------
wire [5:0] mx = spr[0][13:8];
wire [5:0] my = spr[0][7:2];
wire [5:0] ox = spr[pair_i][13:8];
wire [5:0] oy = spr[pair_i][7:2];
wire [1:0] ot = spr[pair_i][1:0];

wire [6:0] mx8 = {1'b0, mx} + 7'd8;
wire [6:0] my8 = {1'b0, my} + 7'd8;
wire [6:0] ox8 = {1'b0, ox} + 7'd8;
wire [6:0] oy8 = {1'b0, oy} + 7'd8;

wire [6:0] x0 = ({1'b0, mx} > {1'b0, ox}) ? {1'b0, mx} : {1'b0, ox};
wire [6:0] y0 = ({1'b0, my} > {1'b0, oy}) ? {1'b0, my} : {1'b0, oy};
wire [6:0] x1 = (mx8 < ox8) ? mx8 : ox8;
wire [6:0] y1 = (my8 < oy8) ? my8 : oy8;

wire ovl = ({1'b0, mx} < ox8) && ({1'b0, ox} < mx8) &&
           ({1'b0, my} < oy8) && ({1'b0, oy} < my8);

wire [6:0] x0_d = x0 - {1'b0, mx};      // window origin, Man local
wire [6:0] y0_d = y0 - {1'b0, my};
wire [6:0] x0_o = x0 - {1'b0, ox};      // window origin, other local
wire [6:0] y0_o = y0 - {1'b0, oy};
wire [6:0] x1_d = x1 - x0;              // window width
wire [6:0] y1_d = y1 - y0;

// ---------------- collision engine addresses -------------------------------
wire [2:0] mr = sy0 + sys;
wire [2:0] mc = sx0 + sxs;
wire [2:0] or_ = oth0y + sys;
wire [2:0] oc = oth0x + sxs;

wire [8:0] man_addr  = {3'b000, mr, mc};
wire [8:0] oth_addr  = {q_oth, or_, oc};
wire [8:0] draw_addr = {cur_q, pix[5:3], pix[2:0]};
wire [8:0] draw_nxt  = {cur_q, (pix + 6'd1)};

// ---------------- draw engine addresses ------------------------------------
wire [5:0]  dbx = cur_x + {3'b000, pix[2:0]};
wire [5:0]  dby = cur_y + {3'b000, pix[5:3]};

wire [1:0]  dtype = spr[di][1:0];
wire        pass_sel = (pass == 2'd0) ? (dtype == 2'b11) :
                       (pass == 2'd1) ? (dtype == 2'b01) || (dtype == 2'b10) :
                                        (dtype == 2'b00);

// ---------------- memory interface -----------------------------------------
// The SRAM / frame-buffer macros have a 1 ns setup time with respect to the
// rising clock edge, therefore every memory control signal is launched on the
// falling edge (half a cycle of margin, exactly as required by the data sheet).
always @(negedge clk or posedge reset) begin
    if (reset) begin
        SR_CEN <= 1'b1;
        SR_A   <= 9'd0;
        FB_CEN <= 1'b1;
        FB_WEN <= 1'b1;
        FB_A   <= 12'd0;
        FB_D   <= 12'd0;
        srmask <= 1'b0;
    end else begin
        SR_CEN <= 1'b1;
        SR_A   <= 9'd0;
        FB_CEN <= 1'b1;
        FB_WEN <= 1'b1;
        FB_A   <= 12'd0;
        FB_D   <= 12'd0;
        srmask <= SR_Q[0];

        case (state)
        S_FILL: begin
            FB_CEN <= 1'b0;
            FB_WEN <= 1'b0;
            FB_A   <= fill_addr;
            FB_D   <= BACKGROUND;
        end
        S_SCAN: begin
            SR_CEN <= 1'b0;
            SR_A   <= man_addr;
        end
        S_WSCAN: begin
            if (SR_Q[0]) begin
                SR_CEN <= 1'b0;
                SR_A   <= oth_addr;
            end
        end
        S_DREAD: begin
            SR_CEN <= 1'b0;
            SR_A   <= draw_addr;
        end
        S_DWAIT: begin
            SR_CEN <= 1'b0;
            SR_A   <= draw_nxt;
            if (SR_Q[0]) begin
                FB_CEN <= 1'b0;
                FB_WEN <= 1'b0;
                FB_A   <= {dby, dbx};
                FB_D   <= SR_Q[12:1];
            end
        end
        default: ;
        endcase
    end
end

// ---------------- control --------------------------------------------------
always @(posedge clk or posedge reset) begin
    if (reset) begin
        state      <= S_IDLE;
        done       <= 1'b0;
        n          <= 5'd0;
        fill_addr  <= 12'd0;
        pair_i     <= 5'd0;
        di         <= 5'd0;
        sxs        <= 3'd0;
        sys        <= 3'd0;
        sx0        <= 3'd0;
        sy0        <= 3'd0;
        sw         <= 4'd0;
        sh         <= 4'd0;
        oth0x      <= 3'd0;
        oth0y      <= 3'd0;
        q_oth      <= 3'd0;
        pix        <= 6'd0;
        pass       <= 2'd0;
        cur_x      <= 6'd0;
        cur_y      <= 6'd0;
        cur_q      <= 3'd0;
        hit_c      <= 1'b0;
        hit_g      <= 1'b0;
        man_state  <= 2'd0;
    end else begin
        done <= 1'b0;
        case (state)

        S_IDLE: begin
            if (sprite) begin
                if (n < NSPR) begin
                    spr[n] <= {X, Y, type};
                    n      <= n + 5'd1;
                end
            end else if (start) begin
                fill_addr <= 12'd0;
                pair_i    <= 5'd1;
                hit_c     <= 1'b0;
                hit_g     <= 1'b0;
                pass      <= 2'd0;
                di        <= 5'd0;
                state     <= S_FILL;
            end
        end

        S_FILL: begin
            if (fill_addr == 12'd4095) begin
                pair_i <= 5'd1;
                state  <= S_PAIR;
            end else begin
                fill_addr <= fill_addr + 12'd1;
            end
        end

        S_PAIR: begin
            if (pair_i >= n) begin
                state <= S_MST;
            end else if ((ot == 2'b00) || (ot == 2'b11) || !ovl) begin
                pair_i <= pair_i + 5'd1;
            end else begin
                sx0   <= x0_d[2:0];
                sy0   <= y0_d[2:0];
                sw    <= x1_d[3:0];
                sh    <= y1_d[3:0];
                oth0x <= x0_o[2:0];
                oth0y <= y0_o[2:0];
                q_oth <= {1'b0, ot} + 3'd2;
                sxs   <= 3'd0;
                sys   <= 3'd0;
                state <= S_SCAN;
            end
        end

        S_SCAN: begin
            state <= S_WSCAN;
        end

        S_WSCAN: begin
            if (srmask) begin
                state <= S_WOTH;
            end else if ({1'b0, sxs} + 4'd1 < sw) begin
                sxs   <= sxs + 3'd1;
                state <= S_SCAN;
            end else if ({1'b0, sys} + 4'd1 < sh) begin
                sxs   <= 3'd0;
                sys   <= sys + 3'd1;
                state <= S_SCAN;
            end else begin
                pair_i <= pair_i + 5'd1;
                state  <= S_PAIR;
            end
        end

        S_WOTH: begin
            if (srmask) begin
                if (q_oth == 3'd4) hit_c <= 1'b1;
                else               hit_g <= 1'b1;
                pair_i <= pair_i + 5'd1;
                state  <= S_PAIR;
            end else if ({1'b0, sxs} + 4'd1 < sw) begin
                sxs   <= sxs + 3'd1;
                state <= S_SCAN;
            end else if ({1'b0, sys} + 4'd1 < sh) begin
                sxs   <= 3'd0;
                sys   <= sys + 3'd1;
                state <= S_SCAN;
            end else begin
                pair_i <= pair_i + 5'd1;
                state  <= S_PAIR;
            end
        end

        S_MST: begin
            man_state <= hit_c ? 2'd1 : (hit_g ? 2'd2 : 2'd0);
            pass      <= 2'd0;
            di        <= 5'd0;
            state     <= S_DSEL;
        end

        S_DSEL: begin
            if (di >= n) begin
                if (pass == 2'd2) begin
                    state <= S_DONE;
                end else begin
                    pass <= pass + 2'd1;
                    di   <= 5'd0;
                end
            end else if (!pass_sel) begin
                di <= di + 5'd1;
            end else begin
                cur_x <= spr[di][13:8];
                cur_y <= spr[di][7:2];
                cur_q <= (dtype == 2'b00) ? {1'b0, man_state}
                                          : ({1'b0, dtype} + 3'd2);
                pix   <= 6'd0;
                state <= S_DREAD;
            end
        end

        S_DREAD: begin
            state <= S_DWAIT;
        end

        S_DWAIT: begin
            if (pix == 6'd63) begin
                di    <= di + 5'd1;
                state <= S_DSEL;
            end else begin
                pix <= pix + 6'd1;
            end
        end

        S_DONE: begin
            done  <= 1'b1;
            n     <= 5'd0;
            state <= S_IDLE;
        end

        default: state <= S_IDLE;
        endcase
    end
end

endmodule
