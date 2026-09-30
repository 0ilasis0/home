`timescale 100ps/10ps
`default_nettype none

module triangle (
    input  wire       clk,
    input  wire       reset,
    input  wire       nt,
    input  wire [2:0] xi,
    input  wire [2:0] yi,
    output wire       busy,
    output wire       po,
    output wire [2:0] xo,
    output wire [2:0] yo
);

    // =========================================================================
    // FSM State Encoding (localparam)
    // =========================================================================
    localparam STATE_IDLE       = 2'b00;
    localparam STATE_CAPTURE_P2 = 2'b01;
    localparam STATE_CAPTURE_P3 = 2'b10;
    localparam STATE_SCAN       = 2'b11;

    // =========================================================================
    // Internal Registers and Next-State Logic
    // =========================================================================
    reg [1:0] state_q, state_d;

    // Coordinate Registers (x3 is intentionally omitted as x1=x3 per architecture)
    reg [2:0] x1_q, x1_d;
    reg [2:0] y1_q, y1_d;
    reg [2:0] x2_q, x2_d;
    reg [2:0] y2_q, y2_d;
    reg [2:0] y3_q, y3_d;

    // Control and Output Registers
    reg       busy_q, busy_d;
    reg       po_q,   po_d;
    reg [2:0] xo_q,   xo_d;
    reg [2:0] yo_q,   yo_d;

    // =========================================================================
    // Sequential Logic (Non-blocking assignments)
    // =========================================================================
    always @(posedge clk or posedge reset) begin
        if (reset) begin
            state_q <= STATE_IDLE;
            busy_q  <= 1'b0;
            po_q    <= 1'b0;
            x1_q    <= 3'd0;
            y1_q    <= 3'd0;
            x2_q    <= 3'd0;
            y2_q    <= 3'd0;
            y3_q    <= 3'd0;
            xo_q    <= 3'd0;
            yo_q    <= 3'd0;
        end else begin
            state_q <= state_d;
            busy_q  <= busy_d;
            po_q    <= po_d;
            x1_q    <= x1_d;
            y1_q    <= y1_d;
            x2_q    <= x2_d;
            y2_q    <= y2_d;
            y3_q    <= y3_d;
            xo_q    <= xo_d;
            yo_q    <= yo_d;
        end
    end

    // =========================================================================
    // Combinational Logic (Blocking assignments)
    // =========================================================================
    always @(*) begin
        // ---------------------------------------------------------------------
        // Default Assignments (Prevents inferred latches)
        // ---------------------------------------------------------------------
        state_d = state_q;
        busy_d  = busy_q;
        po_d    = 1'b0;      // 'po' is 0 during this increment
        xo_d    = xo_q;      // 'xo/yo' hold their state (Don't Care when po=0)
        yo_d    = yo_q;
        
        x1_d    = x1_q;
        y1_d    = y1_q;
        x2_d    = x2_q;
        y2_d    = y2_q;
        y3_d    = y3_q;

        // ---------------------------------------------------------------------
        // Next-State & Datapath Routing
        // ---------------------------------------------------------------------
        case (state_q)
            STATE_IDLE: begin
                busy_d = 1'b0;
                // Cycle N: nt=1 captures P1 and triggers transition
                if (nt == 1'b1) begin
                    x1_d    = xi;
                    y1_d    = yi;
                    state_d = STATE_CAPTURE_P2;
                end
            end

            STATE_CAPTURE_P2: begin
                // Transition occurring at Edge N+1: capture P2 and assert busy
                x2_d    = xi;
                y2_d    = yi;
                busy_d  = 1'b1;
                state_d = STATE_CAPTURE_P3;
            end

            STATE_CAPTURE_P3: begin
                // Transition occurring at Edge N+2: capture P3(y) and enter SCAN
                y3_d    = yi;
                busy_d  = 1'b1; // Maintain busy status
                state_d = STATE_SCAN;
            end

            STATE_SCAN: begin
                // Geometry scan is OUT OF SCOPE for this increment.
                // Architecture baseline requires final scan -> IDLE transition without RELEASE state.
                busy_d  = 1'b0;
                state_d = STATE_IDLE;
            end

            default: begin
                state_d = STATE_IDLE;
                busy_d  = 1'b0;
            end
        endcase
    end

    // =========================================================================
    // Continuous Assignments
    // =========================================================================
    assign busy = busy_q;
    assign po   = po_q;
    assign xo   = xo_q;
    assign yo   = yo_q;

endmodule

`default_nettype wire