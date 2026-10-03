`timescale 1ns/1ps

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
    // FSM State Definitions (Architecture Section 26 & 27)
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
        OUTPUT_SCAN = 4'd10,
        RELEASE     = 4'd11;

    reg [3:0] state, next_state;

    // =========================================================================
    // Datapath & Storage Registers
    // =========================================================================
    // Input coordinates
    reg [2:0] x1, y1, x2, y2, x3, y3;

    // Column storage (8-entry x-indexed, Section 10)
    reg [2:0] ylow [0:7];
    reg [2:0] yup  [0:7];

    // Geometry parameters (Section 9)
    reg signed [3:0] dx, dy12, dy32, s;

    // Shared tracer registers (Section 25)
    reg signed [3:0] x_trace, y_trace;
    reg signed [8:0] E_trace;

    // Output scanner registers (Section 28)
    reg [2:0] x_scan, y_scan, x_left, x_right;
    reg scan_done;

    // =========================================================================
    // Combinational Helpers & Math Extensions
    // =========================================================================
    // |dx| shortcut evaluation (Section 13)
    wire [2:0] abs_dx = (x2 > x1) ? (x2 - x1) : (x1 - x2);

    // Explicit sign extensions to 9-bit for E_trace arithmetic (Section 9)
    wire signed [8:0] dx_ext   = {{5{dx[3]}}, dx};
    wire signed [8:0] dy12_ext = {{5{dy12[3]}}, dy12};
    wire signed [8:0] dy32_ext = {{5{dy32[3]}}, dy32};

    // Normalized inside condition: sE >= 0 (Section 14)
    wire trace_inside = (s == 4'sd1) ? (E_trace >= 9'sd0) : (E_trace <= 9'sd0);

    // =========================================================================
    // Combinational Next-State Logic
    // =========================================================================
    always @(*) begin
        next_state = state; // Default hold state

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
                if (abs_dx == 3'd1)
                    next_state = OUTPUT_SCAN; // |dx|=1 shortcut
                else
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
                    // Outside Y2 Clamp Check
                    if (y_trace + 4'sd1 == $signed({1'b0, y2}))
                        next_state = LOWER_CLAMP;
                end
            end
            LOWER_CLAMP: begin
                if (x_trace + s == $signed({1'b0, x2}))
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
                    // Outside Y2 Clamp Check
                    if (y_trace - 4'sd1 == $signed({1'b0, y2}))
                        next_state = UPPER_CLAMP;
                end
            end
            UPPER_CLAMP: begin
                if (x_trace + s == $signed({1'b0, x2}))
                    next_state = OUTPUT_SCAN;
            end

            // --- SCANNER & RELEASE ---
            OUTPUT_SCAN: begin
                if (scan_done)
                    next_state = RELEASE;
            end
            RELEASE: begin
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
            // Architecture Section 34: Asynchronous active-high reset
            state <= IDLE;
            busy <= 1'b0;
            po <= 1'b0;
            xo <= 3'd0;
            yo <= 3'd0;

            x1 <= 3'd0; y1 <= 3'd0;
            x2 <= 3'd0; y2 <= 3'd0;
            x3 <= 3'd0; y3 <= 3'd0;

            dx <= 4'sd0; dy12 <= 4'sd0; dy32 <= 4'sd0; s <= 4'sd0;
            x_trace <= 4'sd0; y_trace <= 4'sd0; E_trace <= 9'sd0;
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
                    busy <= 1'b1; // Sec 6: busy is 1 during P3 input cycle
                end

                CAPTURE_P3: begin
                    x3 <= xi;
                    y3 <= yi;
                end

                INIT_COLUMN: begin
                    // Signed 4-bit subtraction
                    dx   <= $signed({1'b0, x2}) - $signed({1'b0, x1});
                    dy12 <= $signed({1'b0, y2}) - $signed({1'b0, y1});
                    dy32 <= $signed({1'b0, y2}) - $signed({1'b0, y3});
                    s    <= (x2 > x1) ? 4'sd1 : -4'sd1;

                    x_left  <= (x1 < x2) ? x1 : x2;
                    x_right <= (x1 > x2) ? x1 : x2;

                    // Endpoint initialization (Section 11)
                    ylow[x1] <= y1;
                    yup[x1]  <= y3;
                    ylow[x2] <= y2;
                    yup[x2]  <= y2;

                    // Prepare scanner if taking shortcut
                    if (abs_dx == 3'd1) begin
                        x_scan <= (x1 < x2) ? x1 : x2;
                        y_scan <= y1;
                        scan_done <= 1'b0;
                    end
                end

                // --- LOWER TRACER ---
                LOWER_INIT: begin
                    x_trace <= $signed({1'b0, x1}) + s;
                    y_trace <= $signed({1'b0, y1});
                    E_trace <= (s == 4'sd1) ? -dy12_ext : dy12_ext;
                end
                LOWER_TRACE: begin
                    if (trace_inside) begin
                        ylow[x_trace[2:0]] <= y_trace[2:0];
                        if (x_trace + s != $signed({1'b0, x2})) begin
                            x_trace <= x_trace + s;
                            E_trace <= E_trace - ((s == 4'sd1) ? dy12_ext : -dy12_ext);
                        end
                    end else begin
                        if (y_trace + 4'sd1 == $signed({1'b0, y2})) begin
                            ylow[x_trace[2:0]] <= y2;
                            x_trace <= x_trace + s; // Enter Clamp for next x
                        end else begin
                            y_trace <= y_trace + 4'sd1;
                            E_trace <= E_trace + dx_ext;
                        end
                    end
                end
                LOWER_CLAMP: begin
                    ylow[x_trace[2:0]] <= y2;
                    if (x_trace + s != $signed({1'b0, x2})) begin
                        x_trace <= x_trace + s;
                    end
                end

                // --- UPPER TRACER ---
                UPPER_INIT: begin
                    x_trace <= $signed({1'b0, x1}) + s;
                    y_trace <= $signed({1'b0, y3});
                    E_trace <= (s == 4'sd1) ? dy32_ext : -dy32_ext;
                end
                UPPER_TRACE: begin
                    if (trace_inside) begin
                        yup[x_trace[2:0]] <= y_trace[2:0];
                        if (x_trace + s != $signed({1'b0, x2})) begin
                            x_trace <= x_trace + s;
                            E_trace <= E_trace + ((s == 4'sd1) ? dy32_ext : -dy32_ext);
                        end else begin
                            // Prepare scanner
                            x_scan <= x_left;
                            y_scan <= y1;
                            scan_done <= 1'b0;
                        end
                    end else begin
                        if (y_trace - 4'sd1 == $signed({1'b0, y2})) begin
                            yup[x_trace[2:0]] <= y2;
                            x_trace <= x_trace + s; // Enter Clamp for next x
                        end else begin
                            y_trace <= y_trace - 4'sd1;
                            E_trace <= E_trace + dx_ext;
                        end
                    end
                end
                UPPER_CLAMP: begin
                    yup[x_trace[2:0]] <= y2;
                    if (x_trace + s != $signed({1'b0, x2})) begin
                        x_trace <= x_trace + s;
                    end else begin
                        // Prepare scanner
                        x_scan <= x_left;
                        y_scan <= y1;
                        scan_done <= 1'b0;
                    end
                end

                // --- OUTPUT SCANNER ---
                OUTPUT_SCAN: begin
                    if (scan_done) begin
                        // Last valid point was evaluated and output in previous cycle
                        po <= 1'b0;
                    end else begin
                        // Evaluate condition and update registered outputs
                        po <= (ylow[x_scan] <= y_scan) && (y_scan <= yup[x_scan]);
                        xo <= x_scan;
                        yo <= y_scan;

                        // Scanner iteration logic
                        if (x_scan == x_right) begin
                            if (y_scan == y3) begin
                                scan_done <= 1'b1; // Triggers RELEASE transition
                            end else begin
                                x_scan <= x_left;
                                y_scan <= y_scan + 3'd1;
                            end
                        end else begin
                            x_scan <= x_scan + 3'd1;
                        end
                    end
                end

                // --- RELEASE ---
                RELEASE: begin
                    po <= 1'b0; // busy remains 1 here, po is 0 (Sec 32 & 33)
                end
            endcase
        end
    end

endmodule