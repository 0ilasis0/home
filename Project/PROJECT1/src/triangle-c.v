`timescale 100ps/10ps

module triangle (
    input  wire       clk,
    input  wire       reset,
    input  wire       nt,
    input  wire [2:0] xi,
    input  wire [2:0] yi,
    output reg        busy,
    output reg        po,
    output reg  [2:0] xo,
    output reg  [2:0] yo
);

    // =========================================================================
    // FSM State Definitions
    // =========================================================================
    localparam [3:0]
        IDLE        = 4'd0,
        CAPTURE_P2  = 4'd1,
        CAPTURE_P3  = 4'd2,
        INIT_COLUMN = 4'd3,
        LOWER_INIT  = 4'd4,
        LOWER_TRACE = 4'd5,
        LOWER_CLAMP = 4'd6,
        UPPER_INIT  = 4'd7,
        UPPER_TRACE = 4'd8,
        UPPER_CLAMP = 4'd9,
        OUTPUT_SCAN = 4'd10;

    reg [3:0] state, next_state;

    // =========================================================================
    // Datapath & Storage Registers
    // =========================================================================
    // Input coordinates (Candidate D: Redundant x3 register removed)
    reg [2:0] x1, y1, x2, y2, y3;

    // Column storage (8-entry x-indexed)
    reg [2:0] ylow [0:7];
    reg [2:0] yup  [0:7];

    // Geometry parameters
    reg signed [3:0] dx, dy12, dy32, s;

    // Shared tracer registers
    reg signed [3:0] x_trace, y_trace;

    // Candidate B: E_trace width reduced to minimal safe width (4-bit signed)
    reg signed [3:0] E_trace;

    // Output scanner registers
    reg [2:0] x_scan, y_scan, x_left, x_right;
    reg scan_done;

    // Candidate C: 4-bit signed geometry operands for edge accumulators
    wire signed [3:0] s_dy12 = (s == 4'sd1) ? dy12 : -dy12;
    wire signed [3:0] s_dy32 = (s == 4'sd1) ? dy32 : -dy32;

    // Normalized inside condition: sE >= 0
    wire trace_inside = (s == 4'sd1) ? (E_trace >= 4'sd0) : (E_trace <= 4'sd0);

    // Output scan candidate evaluation
    wire current_inside = (ylow[x_scan] <= y_scan) && (y_scan <= yup[x_scan]);

    // Candidate A: Row-start dynamic search comb-logic
    // Used to skip leading outside candidates when x2 < x1 (s == -1)
    wire [2:0] target_y = (state == OUTPUT_SCAN) ? (y_scan + 3'd1) : y1;
    wire [7:0] valid_x_mask;

    genvar gi;
    generate
        for (gi = 0; gi < 8; gi = gi + 1) begin : gen_valid_mask
            assign valid_x_mask[gi] = (gi >= x_left) && (gi <= x_right) &&
                                      (ylow[gi] <= target_y) && (target_y <= yup[gi]);
        end
    endgenerate

    // 8-to-3 Priority Encoder to find the first valid x column for target_y
    wire [2:0] first_valid_x =
        valid_x_mask[0] ? 3'd0 : valid_x_mask[1] ? 3'd1 :
        valid_x_mask[2] ? 3'd2 : valid_x_mask[3] ? 3'd3 :
        valid_x_mask[4] ? 3'd4 : valid_x_mask[5] ? 3'd5 :
        valid_x_mask[6] ? 3'd6 : 3'd7;

    wire [2:0] next_x_start = (s == -4'sd1) ? first_valid_x : x_left;

    // =========================================================================
    // Combinational Next-State Logic
    // =========================================================================
    always @(*) begin
        next_state = state;

        case (state)
            IDLE: begin
                if (nt) next_state = CAPTURE_P2;
            end
            CAPTURE_P2: begin
                next_state = CAPTURE_P3;
            end
            CAPTURE_P3: begin
                next_state = INIT_COLUMN;
            end
            INIT_COLUMN: begin
                next_state = LOWER_INIT;
            end

            // --- LOWER TRACER ---
            LOWER_INIT: begin
                next_state = LOWER_TRACE;
            end
            LOWER_TRACE: begin
                if (trace_inside) begin
                    if (x_trace + s == $signed({1'b0, x2}))
                        next_state = UPPER_INIT;
                end else begin
                    if (y_trace + 4'sd1 == $signed({1'b0, y2}))
                        next_state = LOWER_CLAMP;
                end
            end
            LOWER_CLAMP: begin
                // Fixed in Task 1 Bugfix: Exact x2 termination matching
                if (x_trace == $signed({1'b0, x2}))
                    next_state = UPPER_INIT;
            end

            // --- UPPER TRACER ---
            UPPER_INIT: begin
                next_state = UPPER_TRACE;
            end
            UPPER_TRACE: begin
                if (trace_inside) begin
                    if (x_trace + s == $signed({1'b0, x2}))
                        next_state = OUTPUT_SCAN;
                end else begin
                    if (y_trace - 4'sd1 == $signed({1'b0, y2}))
                        next_state = UPPER_CLAMP;
                end
            end
            UPPER_CLAMP: begin
                // Fixed in Task 1 Bugfix: Exact x2 termination matching
                if (x_trace == $signed({1'b0, x2}))
                    next_state = OUTPUT_SCAN;
            end

            // --- SCANNER ---
            OUTPUT_SCAN: begin
                if (scan_done)
                    next_state = IDLE;
            end
            default: next_state = IDLE;
        endcase
    end

    // =========================================================================
    // Sequential Datapath & Register Updates
    // =========================================================================
    integer i;
    always @(posedge clk or posedge reset) begin
        if (reset) begin
            state <= IDLE;
            busy <= 1'b0;
            po <= 1'b0;
            xo <= 3'd0;
            yo <= 3'd0;

            x1 <= 3'd0; y1 <= 3'd0;
            x2 <= 3'd0; y2 <= 3'd0;
            y3 <= 3'd0; // Candidate D: x3 removed

            dx <= 4'sd0; dy12 <= 4'sd0; dy32 <= 4'sd0; s <= 4'sd0;
            x_trace <= 4'sd0; y_trace <= 4'sd0; E_trace <= 4'sd0;
            x_scan <= 3'd0; y_scan <= 3'd0; x_left <= 3'd0; x_right <= 3'd0;
            scan_done <= 1'b0;

            for (i=0; i<8; i=i+1) begin
                ylow[i] <= 3'd0;
                yup[i]  <= 3'd0;
            end
        end else begin
            state <= next_state;

            case (state)
                IDLE: begin
                    busy <= 1'b0;
                    po <= 1'b0;
                    if (nt) begin
                        x1 <= xi;
                        y1 <= yi;
                    end
                end

                CAPTURE_P2: begin
                    x2 <= xi;
                    y2 <= yi;
                    busy <= 1'b1;
                end

                CAPTURE_P3: begin
                    y3 <= yi; // Candidate D: x3 capture removed
                end

                INIT_COLUMN: begin
                    dx   <= $signed({1'b0, x2}) - $signed({1'b0, x1});
                    dy12 <= $signed({1'b0, y2}) - $signed({1'b0, y1});
                    dy32 <= $signed({1'b0, y2}) - $signed({1'b0, y3});
                    s    <= (x2 > x1) ? 4'sd1 : -4'sd1;

                    x_left  <= (x1 < x2) ? x1 : x2;
                    x_right <= (x1 > x2) ? x1 : x2;

                    ylow[x1] <= y1;
                    yup[x1]  <= y3;
                    ylow[x2] <= y2;
                    yup[x2]  <= y2;
                end

                // --- LOWER TRACER ---
                LOWER_INIT: begin
                    x_trace <= $signed({1'b0, x1}) + s;
                    y_trace <= $signed({1'b0, y1});
                    E_trace <= -s_dy12;
                end
                LOWER_TRACE: begin
                    if (trace_inside) begin
                        ylow[x_trace[2:0]] <= y_trace[2:0];
                        if (x_trace + s != $signed({1'b0, x2})) begin
                            x_trace <= x_trace + s;
                            E_trace <= E_trace - s_dy12;
                        end
                    end else begin
                        if (y_trace + 4'sd1 == $signed({1'b0, y2})) begin
                            ylow[x_trace[2:0]] <= y2;
                            x_trace <= x_trace + s;
                        end else begin
                            y_trace <= y_trace + 4'sd1;
                            E_trace <= E_trace + dx;
                        end
                    end
                end
                LOWER_CLAMP: begin
                    if (x_trace != $signed({1'b0, x2})) begin
                        ylow[x_trace[2:0]] <= y2;
                        x_trace <= x_trace + s;
                    end
                end

                // --- UPPER TRACER ---
                UPPER_INIT: begin
                    x_trace <= $signed({1'b0, x1}) + s;
                    y_trace <= $signed({1'b0, y3});
                    E_trace <= s_dy32;
                end
                UPPER_TRACE: begin
                    if (trace_inside) begin
                        yup[x_trace[2:0]] <= y_trace[2:0];
                        if (x_trace + s != $signed({1'b0, x2})) begin
                            x_trace <= x_trace + s;
                            E_trace <= E_trace + s_dy32;
                        end else begin
                            x_scan <= next_x_start; // Candidate A integration
                            y_scan <= y1;
                            scan_done <= 1'b0;
                        end
                    end else begin
                        if (y_trace - 4'sd1 == $signed({1'b0, y2})) begin
                            yup[x_trace[2:0]] <= y2;
                            x_trace <= x_trace + s;
                        end else begin
                            y_trace <= y_trace - 4'sd1;
                            E_trace <= E_trace + dx;
                        end
                    end
                end
                UPPER_CLAMP: begin
                    if (x_trace != $signed({1'b0, x2})) begin
                        yup[x_trace[2:0]] <= y2;
                        x_trace <= x_trace + s;
                    end else begin
                        x_scan <= next_x_start; // Candidate A integration
                        y_scan <= y1;
                        scan_done <= 1'b0;
                    end
                end

                // --- OUTPUT SCANNER ---
                OUTPUT_SCAN: begin
                    if (scan_done) begin
                        po <= 1'b0;
                    end else begin
                        po <= current_inside;
                        xo <= x_scan;
                        yo <= y_scan;

                        if (!current_inside && (s == 4'sd1)) begin
                            // OUTPUT-SCAN-EARLY-EXIT: Fast-forward trailing zeros when x2 > x1
                            if (y_scan == y3) begin
                                scan_done <= 1'b1;
                            end else begin
                                x_scan <= next_x_start;
                                y_scan <= y_scan + 3'd1;
                            end
                        end else begin
                            // Normal progression or Candidate A (leading zero skip when x2 < x1)
                            if (x_scan == x_right) begin
                                if (y_scan == y3) begin
                                    scan_done <= 1'b1;
                                end else begin
                                    x_scan <= next_x_start;
                                    y_scan <= y_scan + 3'd1;
                                end
                            end else begin
                                x_scan <= x_scan + 3'd1;
                            end
                        end
                    end
                end
            endcase
        end
    end

endmodule