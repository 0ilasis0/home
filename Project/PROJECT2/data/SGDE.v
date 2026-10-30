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
reg done;

localparam MAX_SPRITES = 20;
localparam [11:0] BACKGROUND = 12'hCF0;

localparam [4:0] S_IDLE            = 5'd0;
localparam [4:0] S_FILL            = 5'd1;
localparam [4:0] S_FIND_MAN        = 5'd2;
localparam [4:0] S_FIND_PAIR       = 5'd3;
localparam [4:0] S_READ_MAN        = 5'd5;
localparam [4:0] S_WAIT_MAN        = 5'd6;
localparam [4:0] S_WAIT_OTHER      = 5'd8;
localparam [4:0] S_SET_MAN_STATE   = 5'd10;
localparam [4:0] S_SELECT_DRAW     = 5'd11;
localparam [4:0] S_DRAW_READ       = 5'd12;
localparam [4:0] S_DRAW_WAIT       = 5'd13;
localparam [4:0] S_DONE            = 5'd16;

reg [4:0] state;
reg [5:0] sprite_count;
reg [11:0] fill_address;
reg [4:0] man_index, other_index, draw_index;
reg [3:0] scan_x, scan_y;
reg [3:0] scan_start_x, scan_start_y;
reg [3:0] scan_end_x, scan_end_y;
reg [5:0] pixel_index;
reg [1:0] draw_pass;
reg [5:0] sprite_x [0:MAX_SPRITES-1];
reg [5:0] sprite_y [0:MAX_SPRITES-1];
reg [1:0] sprite_type [0:MAX_SPRITES-1];
reg [1:0] man_state [0:MAX_SPRITES-1];
reg man_hit_candy, man_hit_ghost, man_mask_pixel;

wire [6:0] man_pixel_x;
wire [6:0] man_pixel_y;
wire [6:0] overlap_left;
wire [6:0] overlap_top;
wire [6:0] overlap_right;
wire [6:0] overlap_bottom;
wire [6:0] man_right;
wire [6:0] man_bottom;
wire [6:0] other_right;
wire [6:0] other_bottom;
wire [6:0] other_local_x;
wire [6:0] other_local_y;
wire [6:0] draw_pixel_x;
wire [6:0] draw_pixel_y;
wire [8:0] draw_rom_base;

assign ready = (state == S_IDLE) && !reset;

assign man_pixel_x = {1'b0, sprite_x[man_index]} + {3'b0, scan_x};
assign man_pixel_y = {1'b0, sprite_y[man_index]} + {3'b0, scan_y};
assign man_right = {1'b0, sprite_x[man_index]} + 7'd8;
assign man_bottom = {1'b0, sprite_y[man_index]} + 7'd8;
assign other_right = {1'b0, sprite_x[other_index]} + 7'd8;
assign other_bottom = {1'b0, sprite_y[other_index]} + 7'd8;
assign overlap_left = ({1'b0, sprite_x[man_index]} > {1'b0, sprite_x[other_index]})
                    ? {1'b0, sprite_x[man_index]} : {1'b0, sprite_x[other_index]};
assign overlap_top = ({1'b0, sprite_y[man_index]} > {1'b0, sprite_y[other_index]})
                   ? {1'b0, sprite_y[man_index]} : {1'b0, sprite_y[other_index]};
assign overlap_right = (man_right < other_right) ? man_right : other_right;
assign overlap_bottom = (man_bottom < other_bottom) ? man_bottom : other_bottom;
assign other_local_x = man_pixel_x - {1'b0, sprite_x[other_index]};
assign other_local_y = man_pixel_y - {1'b0, sprite_y[other_index]};
assign draw_pixel_x = {1'b0, sprite_x[draw_index]} + {4'b0, pixel_index[2:0]};
assign draw_pixel_y = {1'b0, sprite_y[draw_index]} + {4'b0, pixel_index[5:3]};
assign draw_rom_base = (sprite_type[draw_index] == 2'b00)
                     ? ((man_state[draw_index] == 2'd1) ? 9'd64
                     : (man_state[draw_index] == 2'd2) ? 9'd128 : 9'd0)
                     : sprite_base(sprite_type[draw_index]);

function [8:0] sprite_base;
    input [1:0] sprite_type_in;
    begin
        case (sprite_type_in)
            2'b00: sprite_base = 9'd0;
            2'b01: sprite_base = 9'd192;
            2'b10: sprite_base = 9'd256;
            default: sprite_base = 9'd320;
        endcase
    end
endfunction

always @(negedge clk or posedge reset) begin
    if (reset) begin
        SR_CEN <= 1'b1;
        SR_A <= 9'd0;
        FB_CEN <= 1'b1;
        FB_WEN <= 1'b1;
        FB_A <= 12'd0;
        FB_D <= 12'd0;
        man_mask_pixel <= 1'b0;
    end else begin
        SR_CEN <= 1'b1;
        SR_A <= 9'd0;
        FB_CEN <= 1'b1;
        FB_WEN <= 1'b1;
        FB_A <= 12'd0;
        FB_D <= 12'd0;

        if (state == S_WAIT_MAN)
            man_mask_pixel <= SR_Q[0];

        case (state)
            S_FILL: begin
                FB_CEN <= 1'b0;
                FB_WEN <= 1'b0;
                FB_A <= fill_address;
                FB_D <= BACKGROUND;
            end
            S_READ_MAN: begin
                SR_CEN <= 1'b0;
                SR_A <= {3'b000, scan_y[2:0], scan_x[2:0]};
            end
            S_WAIT_MAN: begin
                if (SR_Q[0]) begin
                    SR_CEN <= 1'b0;
                    SR_A <= sprite_base(sprite_type[other_index])
                          + {3'b000, other_local_y[2:0], other_local_x[2:0]};
                end
            end
            S_DRAW_READ: begin
                SR_CEN <= 1'b0;
                SR_A <= draw_rom_base + {3'b0, pixel_index};
            end
            S_DRAW_WAIT: begin
                if (pixel_index != 6'd63) begin
                    SR_CEN <= 1'b0;
                    SR_A <= draw_rom_base + {3'b0, pixel_index} + 9'd1;
                end
                if (SR_Q[0] && (draw_pixel_x < 7'd64)
                            && (draw_pixel_y < 7'd64)) begin
                    FB_CEN <= 1'b0;
                    FB_WEN <= 1'b0;
                    FB_A <= {draw_pixel_y[5:0], draw_pixel_x[5:0]};
                    FB_D <= SR_Q[12:1];
                end
            end
            default: begin
            end
        endcase
    end
end

always @(posedge clk or posedge reset) begin
    if (reset) begin
        state <= S_IDLE;
        done <= 1'b0;
        sprite_count <= 6'd0;
        fill_address <= 12'd0;
        man_index <= 5'd0;
        other_index <= 5'd0;
        draw_index <= 5'd0;
        scan_x <= 4'd0;
        scan_y <= 4'd0;
        scan_start_x <= 4'd0;
        scan_start_y <= 4'd0;
        scan_end_x <= 4'd0;
        scan_end_y <= 4'd0;
        pixel_index <= 6'd0;
        draw_pass <= 2'd0;
        man_hit_candy <= 1'b0;
        man_hit_ghost <= 1'b0;
    end else begin
        case (state)
            S_IDLE: begin
                if (sprite) begin
                    done <= 1'b0;
                    if (sprite_count < MAX_SPRITES) begin
                        sprite_x[sprite_count] <= X;
                        sprite_y[sprite_count] <= Y;
                        sprite_type[sprite_count] <= type;
                        man_state[sprite_count] <= 2'd0;
                        sprite_count <= sprite_count + 1'b1;
                    end
                end else if (start) begin
                    done <= 1'b0;
                    fill_address <= 12'd0;
                    state <= S_FILL;
                end
            end

            S_FILL: begin
                if (fill_address == 12'd4095) begin
                    man_index <= 5'd0;
                    state <= S_FIND_MAN;
                end else begin
                    fill_address <= fill_address + 1'b1;
                end
            end

            S_FIND_MAN: begin
                if (man_index >= sprite_count) begin
                    draw_pass <= 2'd0;
                    draw_index <= 5'd0;
                    state <= S_SELECT_DRAW;
                end else if (sprite_type[man_index] != 2'b00) begin
                    man_index <= man_index + 1'b1;
                end else begin
                    other_index <= 5'd0;
                    man_hit_candy <= 1'b0;
                    man_hit_ghost <= 1'b0;
                    state <= S_FIND_PAIR;
                end
            end

            S_FIND_PAIR: begin
                if (other_index >= sprite_count) begin
                    state <= S_SET_MAN_STATE;
                end else if ((other_index == man_index)
                          || (sprite_type[other_index] == 2'b00)
                          || (sprite_type[other_index] == 2'b11)) begin
                    other_index <= other_index + 1'b1;
                end else if (({1'b0, sprite_x[man_index]} >= other_right)
                          || ({1'b0, sprite_x[other_index]} >= man_right)
                          || ({1'b0, sprite_y[man_index]} >= other_bottom)
                          || ({1'b0, sprite_y[other_index]} >= man_bottom)) begin
                    other_index <= other_index + 1'b1;
                end else begin
                    scan_x <= overlap_left - {1'b0, sprite_x[man_index]};
                    scan_y <= overlap_top - {1'b0, sprite_y[man_index]};
                    scan_start_x <= overlap_left - {1'b0, sprite_x[man_index]};
                    scan_start_y <= overlap_top - {1'b0, sprite_y[man_index]};
                    scan_end_x <= overlap_right - {1'b0, sprite_x[man_index]};
                    scan_end_y <= overlap_bottom - {1'b0, sprite_y[man_index]};
                    state <= S_READ_MAN;
                end
            end

            S_READ_MAN: begin
                state <= S_WAIT_MAN;
            end

            S_WAIT_MAN: begin
                if (man_mask_pixel) begin
                    state <= S_WAIT_OTHER;
                end else if ({1'b0, scan_x} + 4'd1 < scan_end_x) begin
                    scan_x <= scan_x + 1'b1;
                    state <= S_READ_MAN;
                end else if ({1'b0, scan_y} + 4'd1 < scan_end_y) begin
                    scan_x <= scan_start_x;
                    scan_y <= scan_y + 1'b1;
                    state <= S_READ_MAN;
                end else begin
                    other_index <= other_index + 1'b1;
                    state <= S_FIND_PAIR;
                end
            end

            S_WAIT_OTHER: begin
                if (SR_Q[0]) begin
                    if (sprite_type[other_index] == 2'b10)
                        man_hit_candy <= 1'b1;
                    else
                        man_hit_ghost <= 1'b1;
                    other_index <= other_index + 1'b1;
                    state <= S_FIND_PAIR;
                end else begin
                    if ({1'b0, scan_x} + 4'd1 < scan_end_x) begin
                        scan_x <= scan_x + 1'b1;
                        state <= S_READ_MAN;
                    end else if ({1'b0, scan_y} + 4'd1 < scan_end_y) begin
                        scan_x <= scan_start_x;
                        scan_y <= scan_y + 1'b1;
                        state <= S_READ_MAN;
                    end else begin
                        other_index <= other_index + 1'b1;
                        state <= S_FIND_PAIR;
                    end
                end
            end

            S_SET_MAN_STATE: begin
                if (man_hit_candy)
                    man_state[man_index] <= 2'd1;
                else if (man_hit_ghost)
                    man_state[man_index] <= 2'd2;
                else
                    man_state[man_index] <= 2'd0;
                man_index <= man_index + 1'b1;
                state <= S_FIND_MAN;
            end

            S_SELECT_DRAW: begin
                if (draw_index >= sprite_count) begin
                    if (draw_pass == 2'd2) begin
                        state <= S_DONE;
                    end else begin
                        draw_pass <= draw_pass + 1'b1;
                        draw_index <= 5'd0;
                    end
                end else if (((draw_pass == 2'd0)
                           && (sprite_type[draw_index] != 2'b11))
                          || ((draw_pass == 2'd1)
                           && (sprite_type[draw_index] == 2'b00
                            || sprite_type[draw_index] == 2'b11))
                          || ((draw_pass == 2'd2)
                           && (sprite_type[draw_index] != 2'b00))) begin
                    draw_index <= draw_index + 1'b1;
                end else begin
                    pixel_index <= 6'd0;
                    state <= S_DRAW_READ;
                end
            end

            S_DRAW_READ: begin
                state <= S_DRAW_WAIT;
            end

            S_DRAW_WAIT: begin
                if (pixel_index == 6'd63) begin
                    draw_index <= draw_index + 1'b1;
                    state <= S_SELECT_DRAW;
                end else begin
                    pixel_index <= pixel_index + 1'b1;
                end
            end

            S_DONE: begin
                done <= 1'b1;
                sprite_count <= 6'd0;
                state <= S_IDLE;
            end

            default: begin
                state <= S_IDLE;
            end
        endcase
    end
end

endmodule
