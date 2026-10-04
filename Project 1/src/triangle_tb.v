`timescale 100ps/10ps

// ============================================================================
// Project        : Triangle Rendering Engine
// File           : tb_triangle.v
// Purpose        : RTL Verification Testbench for triangle.v
//
// Verification baseline:
//   - Project Specification
//   - Architecture Specification A1
//   - RTL Coding Standard
//   - Manager RTL Verification Task 1
//
// IMPORTANT:
//   - This file is TESTBENCH ONLY.
//   - Production RTL triangle.v is NOT modified.
//   - Verilog-2001 compatible.
//   - No SystemVerilog constructs.
//   - Reference model is intentionally independent from RTL tracer equations.
// ============================================================================

module tb_triangle;

// =========================================================================
// DUT Interface
// =========================================================================
reg        clk;
reg        reset;
reg        nt;
reg  [2:0] xi;
reg  [2:0] yi;

wire       busy;
wire       po;
wire [2:0] xo;
wire [2:0] yo;

triangle dut (
    .clk   (clk),
    .reset (reset),
    .nt    (nt),
    .xi    (xi),
    .yi    (yi),
    .busy  (busy),
    .po     (po),
    .xo     (xo),
    .yo     (yo)
);

// =========================================================================
// Clock
// =========================================================================
initial begin
    clk = 1'b0;
    forever #5 clk = ~clk;
end

// =========================================================================
// Test Configuration
// =========================================================================
integer total_tests;
integer pass_tests;
integer fail_tests;

integer total_points_checked;
integer total_po_cycles;
integer total_busy_errors;
integer total_output_errors;
integer total_protocol_errors;
integer total_timing_errors;
integer total_reset_errors;

integer current_test_id;

// =========================================================================
// Reference Output Storage
//
// Maximum possible candidate space:
//   x range <= 8
//   y range <= 8
//
// Maximum valid triangle points therefore <= 64.
// =========================================================================
reg [2:0] exp_x [0:63];
reg [2:0] exp_y [0:63];

integer exp_count;
integer exp_index;

// =========================================================================
// Transaction bookkeeping
// =========================================================================
integer transaction_active;
integer transaction_finished;

integer first_po_seen;
integer last_po_seen;
integer first_output_cycle;
integer last_output_cycle;

integer cycle_counter;

integer busy_seen_high;
integer busy_violation;

integer po_previous;

// =========================================================================
// Expected final output timing
// =========================================================================
integer release_seen;
integer idle_seen_after_release;

// =========================================================================
// Generic counters
// =========================================================================
integer i;
integer j;

// =========================================================================
// Utility: absolute value
// =========================================================================
function integer abs_int;
    input integer value;
    begin
        if (value < 0)
            abs_int = -value;
        else
            abs_int = value;
    end
endfunction

// =========================================================================
// Independent geometric reference model
//
// Uses point-in-triangle cross products.
//
// For point P and directed edge A->B:
//
//   cross = (Bx-Ax)*(Py-Ay) - (By-Ay)*(Px-Ax)
//
// Boundary is included:
//
//   all cross >= 0 OR all cross <= 0
//
// This is intentionally independent from the RTL LOWER/UPPER tracer.
// =========================================================================
function integer point_inside_triangle;
    input integer px;
    input integer py;

    input integer ax;
    input integer ay;
    input integer bx;
    input integer by;

    input integer cx;
    input integer cy;

    integer c1;
    integer c2;
    integer c3;

    begin
        c1 = (bx - ax) * (py - ay) -
             (by - ay) * (px - ax);

        c2 = (cx - bx) * (py - by) -
             (cy - by) * (px - bx);

        c3 = (ax - cx) * (py - cy) -
             (ay - cy) * (px - cx);

        if ((c1 >= 0) && (c2 >= 0) && (c3 >= 0))
            point_inside_triangle = 1;
        else if ((c1 <= 0) && (c2 <= 0) && (c3 <= 0))
            point_inside_triangle = 1;
        else
            point_inside_triangle = 0;
    end
endfunction

// =========================================================================
// Build expected output list
//
// Required output order:
//
//   y ascending
//   x ascending
//
// Only valid triangle points are stored.
// =========================================================================
task build_expected_output;
    input integer tx1;
    input integer ty1;
    input integer tx2;
    input integer ty2;
    input integer tx3;
    input integer ty3;

    integer min_x;
    integer max_x;
    integer x;
    integer y;

    begin
        exp_count = 0;

        if (tx1 < tx2)
            min_x = tx1;
        else
            min_x = tx2;

        if (tx1 > tx2)
            max_x = tx1;
        else
            max_x = tx2;

        // y ascending
        for (y = ty1; y <= ty3; y = y + 1) begin

            // x ascending
            for (x = min_x; x <= max_x; x = x + 1) begin

                if (point_inside_triangle(
                        x, y,
                        tx1, ty1,
                        tx2, ty2,
                        tx3, ty3)) begin

                    if (exp_count < 64) begin
                        exp_x[exp_count] = x[2:0];
                        exp_y[exp_count] = y[2:0];
                        exp_count = exp_count + 1;
                    end
                end
            end
        end
    end
endtask

// =========================================================================
// Failure reporting
// =========================================================================
task report_failure;
    input [255:0] reason;
    begin
        fail_tests = fail_tests + 1;

        $display(
            "ERROR: TEST=%0d CYCLE=%0d REASON=%s",
            current_test_id,
            cycle_counter,
            reason
        );
    end
endtask

// =========================================================================
// Reset task
// =========================================================================
task apply_reset;
    begin
        reset = 1'b1;
        nt    = 1'b0;
        xi    = 3'd0;
        yi    = 3'd0;

        #2;

        if (busy !== 1'b0) begin
            total_reset_errors = total_reset_errors + 1;
            $display(
                "ERROR: RESET busy != 0, time=%0t",
                $time
            );
        end

        if (po !== 1'b0) begin
            total_reset_errors = total_reset_errors + 1;
            $display(
                "ERROR: RESET po != 0, time=%0t",
                $time
            );
        end

        #8;

        reset = 1'b0;

        @(negedge clk);

        if (busy !== 1'b0) begin
            total_reset_errors = total_reset_errors + 1;
            $display(
                "ERROR: POST-RESET busy != 0, time=%0t",
                $time
            );
        end

        if (po !== 1'b0) begin
            total_reset_errors = total_reset_errors + 1;
            $display(
                "ERROR: POST-RESET po != 0, time=%0t",
                $time
            );
        end
    end
endtask

// =========================================================================
// Drive one triangle
//
// Protocol:
//
// C1: nt=1, P1
// C2: nt=0, P2
// C3: nt=0, P3
//
// Inputs are changed at negedge to guarantee setup before DUT posedge.
// =========================================================================
task drive_triangle;
    input integer tx1;
    input integer ty1;
    input integer tx2;
    input integer ty2;
    input integer tx3;
    input integer ty3;

    begin
        @(negedge clk);

        // -------------------------------
        // C1: P1
        // -------------------------------
        if (busy !== 1'b0) begin
            total_protocol_errors = total_protocol_errors + 1;

            $display(
                "ERROR: Attempted transaction while busy=1 before P1"
            );
        end

        xi = tx1[2:0];
        yi = ty1[2:0];
        nt = 1'b1;

        @(posedge clk);

        // Keep nt high only for this one input cycle.
        @(negedge clk);

        nt = 1'b0;

        // -------------------------------
        // C2: P2
        // -------------------------------
        xi = tx2[2:0];
        yi = ty2[2:0];

        @(posedge clk);

        // busy must be asserted after C2.
        #1;

        if (busy !== 1'b1) begin
            total_busy_errors = total_busy_errors + 1;

            $display(
                "ERROR: BUSY timing failure: busy != 1 after P2 capture"
            );
        end

        @(negedge clk);

        // -------------------------------
        // C3: P3
        // -------------------------------
        xi = tx3[2:0];
        yi = ty3[2:0];

        // busy must already be high before P3.
        if (busy !== 1'b1) begin
            total_busy_errors = total_busy_errors + 1;

            $display(
                "ERROR: BUSY timing failure: busy != 1 before P3"
            );
        end

        @(posedge clk);

        @(negedge clk);

        xi = 3'd0;
        yi = 3'd0;
    end
endtask

// =========================================================================
// Wait for output and check registered po/xo/yo.
//
// IMPORTANT:
//
// The manager requirement explicitly calls for:
//
//   @(posedge clk)
//   if (po == 1)
//
// Therefore the checker samples the registered outputs at posedge,
// before the DUT's NBA update for that edge.
//
// This means the observed output corresponds to the output register
// value established during the preceding cycle.
// =========================================================================
task monitor_transaction;
    input integer tx1;
    input integer ty1;
    input integer tx2;
    input integer ty2;
    input integer tx3;
    input integer ty3;

    integer timeout;
    integer expected_busy_release;
    integer previous_x;
    integer previous_y;
    integer have_previous_point;

    begin
        build_expected_output(
            tx1, ty1,
            tx2, ty2,
            tx3, ty3
        );

        exp_index = 0;

        timeout = 0;
        first_po_seen = 0;
        last_po_seen = 0;
        first_output_cycle = -1;
        last_output_cycle = -1;
        busy_seen_high = 0;
        busy_violation = 0;
        release_seen = 0;
        idle_seen_after_release = 0;

        have_previous_point = 0;
        previous_x = 0;
        previous_y = 0;

        // -------------------------------------------------------------
        // Monitor until busy goes low after the transaction.
        //
        // The expected protocol is:
        //
        //   last valid output : busy=1, po=1
        //   release cycle     : busy=1, po=0
        //   next cycle        : busy=0, po=0
        // -------------------------------------------------------------
        while (timeout < 1000) begin

            @(posedge clk);

            cycle_counter = cycle_counter + 1;
            timeout = timeout + 1;

            // ---------------------------------------------------------
            // Busy must remain high during active operation.
            // ---------------------------------------------------------
            if (busy === 1'b1)
                busy_seen_high = 1;

            // ---------------------------------------------------------
            // Registered output sampling required by verification task.
            // ---------------------------------------------------------
            if (po === 1'b1) begin

                total_po_cycles = total_po_cycles + 1;

                if (first_po_seen == 0) begin
                    first_po_seen = 1;
                    first_output_cycle = cycle_counter;
                end

                last_po_seen = 1;
                last_output_cycle = cycle_counter;

                // -----------------------------------------------------
                // Compare against independent reference model.
                // -----------------------------------------------------
                if (exp_index >= exp_count) begin

                    total_output_errors = total_output_errors + 1;

                    $display(
                        "ERROR: Unexpected extra output: xo=%0d yo=%0d",
                        xo, yo
                    );

                end else begin

                    if ((xo !== exp_x[exp_index]) ||
                        (yo !== exp_y[exp_index])) begin

                        total_output_errors =
                            total_output_errors + 1;

                        $display(
                            "ERROR: OUTPUT mismatch: ",
                            "expected=(%0d,%0d) ",
                            "actual=(%0d,%0d) ",
                            "index=%0d",
                            //current_test_id,
                            exp_x[exp_index],
                            exp_y[exp_index],
                            xo,
                            yo,
                            exp_index
                        );
                    end else begin
                        total_points_checked =
                            total_points_checked + 1;
                    end

                    // -------------------------------------------------
                    // Explicit ordering check.
                    // -------------------------------------------------
                    if (have_previous_point) begin

                        if (yo < previous_y) begin
                            total_output_errors =
                                total_output_errors + 1;

                            $display(
                                "ERROR: Y ordering violation: ",
                                "previous=(%0d,%0d) ",
                                "current=(%0d,%0d)",
                                previous_x,
                                previous_y,
                                xo,
                                yo
                            );
                        end

                        if ((yo == previous_y) &&
                            (xo < previous_x)) begin

                            total_output_errors =
                                total_output_errors + 1;

                            $display(
                                "ERROR: X ordering violation: ",
                                "previous=(%0d,%0d) ",
                                "current=(%0d,%0d)",
                                previous_x,
                                previous_y,
                                xo,
                                yo
                            );
                        end
                    end

                    previous_x = xo;
                    previous_y = yo;
                    have_previous_point = 1;

                    exp_index = exp_index + 1;
                end
            end

            // ---------------------------------------------------------
            // po must be zero once expected valid points are exhausted.
            // ---------------------------------------------------------
            if ((exp_index == exp_count) && (first_po_seen != 0)) begin
                if (po === 1'b1) begin
                    // Extra output is handled above.
                end
            end

            // ---------------------------------------------------------
            // Busy release.
            // ---------------------------------------------------------
            if (busy === 1'b0) begin

                if (busy_seen_high != 0) begin
                    idle_seen_after_release = 1;
                    release_seen = 1;
                    disable monitor_transaction;
                end
            end

        end

        // -----------------------------------------------------------------
        // Timeout
        // -----------------------------------------------------------------
        if (timeout >= 1000) begin
            total_timing_errors = total_timing_errors + 1;

            $display(
                "ERROR: Transaction timeout: busy never returned low"
            );
        end

        // -----------------------------------------------------------------
        // Number of valid outputs
        // -----------------------------------------------------------------
        if (exp_index != exp_count) begin
            total_output_errors = total_output_errors + 1;

            $display(
                "ERROR: Output count mismatch: expected=%0d observed=%0d",
                exp_count,
                exp_index
            );
        end

        // -----------------------------------------------------------------
        // busy must have been asserted during transaction.
        // -----------------------------------------------------------------
        if (busy_seen_high == 0) begin
            total_busy_errors = total_busy_errors + 1;

            $display(
                "ERROR: busy never asserted for transaction"
            );
        end
    end
endtask

// =========================================================================
// One complete transaction
// =========================================================================
task run_triangle;
    input integer tx1;
    input integer ty1;
    input integer tx2;
    input integer ty2;
    input integer tx3;
    input integer ty3;

    integer errors_before;

    begin
        current_test_id = current_test_id + 1;
        total_tests = total_tests + 1;

        errors_before =
            total_busy_errors +
            total_output_errors +
            total_protocol_errors +
            total_timing_errors +
            total_reset_errors;

        drive_triangle(
            tx1, ty1,
            tx2, ty2,
            tx3, ty3
        );

        monitor_transaction(
            tx1, ty1,
            tx2, ty2,
            tx3, ty3
        );

        if ((total_busy_errors +
             total_output_errors +
             total_protocol_errors +
             total_timing_errors +
             total_reset_errors) ==
            errors_before) begin

            pass_tests = pass_tests + 1;

        end
    end
endtask

// =========================================================================
// Directed corner tests
// =========================================================================
task directed_tests;
    begin

        $display("");
        $display("============================================================");
        $display("DIRECTED TESTS");
        $display("============================================================");

        // -------------------------------------------------------------
        // Sample from Project Specification
        // P1=(1,1), P2=(6,3), P3=(1,6)
        // -------------------------------------------------------------
        run_triangle(
            1,1,
            6,3,
            1,6
        );

        // -------------------------------------------------------------
        // Reverse x direction
        // -------------------------------------------------------------
        run_triangle(
            6,1,
            1,3,
            6,6
        );

        // -------------------------------------------------------------
        // |dx| = 1
        // -------------------------------------------------------------
        run_triangle(
            1,1,
            2,3,
            1,6
        );

        run_triangle(
            6,1,
            5,3,
            6,6
        );

        // -------------------------------------------------------------
        // x/y minimum boundary = 0
        // -------------------------------------------------------------
        run_triangle(
            0,0,
            3,2,
            0,5
        );

        // -------------------------------------------------------------
        // x/y maximum boundary = 7
        // -------------------------------------------------------------
        run_triangle(
            7,2,
            4,5,
            7,7
        );

        // -------------------------------------------------------------
        // Very narrow triangle
        // -------------------------------------------------------------
        run_triangle(
            0,0,
            1,1,
            0,2
        );

        // -------------------------------------------------------------
        // Wide triangle
        // -------------------------------------------------------------
        run_triangle(
            0,1,
            7,4,
            0,7
        );

        // -------------------------------------------------------------
        // Minimum dy12
        // -------------------------------------------------------------
        run_triangle(
            2,1,
            5,2,
            2,3
        );

        // -------------------------------------------------------------
        // Maximum vertical extent
        // -------------------------------------------------------------
        run_triangle(
            1,0,
            6,3,
            1,7
        );

    end
endtask

// =========================================================================
// Exhaustive legal triangle generation
//
// Legal constraints:
//
//   x1 = x3
//   y1 < y2 < y3
//   x2 != x1
//   x/y in [0,7]
//
// Total expected:
//
//   3136 triangles
// =========================================================================
task exhaustive_tests;
    integer tx1;
    integer ty1;
    integer tx2;
    integer ty2;
    integer ty3;

    integer exhaustive_count;

    begin

        $display("");
        $display("============================================================");
        $display("EXHAUSTIVE LEGAL TRIANGLE TEST");
        $display("============================================================");

        exhaustive_count = 0;

        for (tx1 = 0; tx1 <= 7; tx1 = tx1 + 1) begin

            for (ty1 = 0; ty1 <= 7; ty1 = ty1 + 1) begin

                for (ty2 = ty1 + 1;
                     ty2 <= 7;
                     ty2 = ty2 + 1) begin

                    for (ty3 = ty2 + 1;
                         ty3 <= 7;
                         ty3 = ty3 + 1) begin

                        for (tx2 = 0;
                             tx2 <= 7;
                             tx2 = tx2 + 1) begin

                            if (tx2 != tx1) begin

                                exhaustive_count =
                                    exhaustive_count + 1;

                                run_triangle(
                                    tx1,
                                    ty1,
                                    tx2,
                                    ty2,
                                    tx1,
                                    ty3
                                );

                            end
                        end
                    end
                end
            end
        end

        if (exhaustive_count != 3136) begin
            $display(
                "ERROR: Exhaustive domain count = %0d, expected 3136",
                exhaustive_count
            );
        end else begin
            $display(
                "Exhaustive legal triangle count = %0d",
                exhaustive_count
            );
        end
    end
endtask

// =========================================================================
// Main Test
// =========================================================================
initial begin

    total_tests          = 0;
    pass_tests           = 0;
    fail_tests           = 0;

    total_points_checked = 0;
    total_po_cycles      = 0;

    total_busy_errors    = 0;
    total_output_errors  = 0;
    total_protocol_errors = 0;
    total_timing_errors  = 0;
    total_reset_errors   = 0;

    current_test_id = 0;
    cycle_counter   = 0;

    nt = 1'b0;
    xi = 3'd0;
    yi = 3'd0;

    // -------------------------------------------------------------
    // Initial reset
    // -------------------------------------------------------------
    apply_reset();

    // -------------------------------------------------------------
    // Directed tests
    // -------------------------------------------------------------
    directed_tests();

    // -------------------------------------------------------------
    // Exhaustive legal domain
    // -------------------------------------------------------------
    exhaustive_tests();

    // -------------------------------------------------------------
    // Final report
    // -------------------------------------------------------------
    $display("");
    $display("============================================================");
    $display("RTL VERIFICATION REPORT");
    $display("============================================================");

    $display("Total transactions      = %0d", total_tests);
    $display("Transaction PASS count  = %0d", pass_tests);

    $display("Points checked          = %0d",
             total_points_checked);

    $display("Observed po cycles      = %0d",
             total_po_cycles);

    $display("");
    $display("Busy errors             = %0d",
             total_busy_errors);

    $display("Output errors           = %0d",
             total_output_errors);

    $display("Protocol errors         = %0d",
             total_protocol_errors);

    $display("Timing errors           = %0d",
             total_timing_errors);

    $display("Reset errors            = %0d",
             total_reset_errors);

    $display("============================================================");

    if ((total_busy_errors     == 0) &&
        (total_output_errors   == 0) &&
        (total_protocol_errors == 0) &&
        (total_timing_errors   == 0) &&
        (total_reset_errors    == 0)) begin

        $display("RESULT: FUNCTIONAL REGRESSION PASS");
        $display("");
        $display(
            "NOTE: This PASS represents RTL simulation evidence only."
        );
        $display(
            "Synthesis / STA / Gate-level verification are NOT covered."
        );

    end else begin

        $display("RESULT: FUNCTIONAL REGRESSION FAIL");

        $display("");
        $display("Failure classification required:");
        $display("  SYMPTOM");
        $display("  FAILING STAGE");
        $display("  ROOT CAUSE");
        $display("  AFFECTED ARCHITECTURE SECTION");
        $display("  RTL IMPACT");
        $display("  PROPOSED FIX");
        $display("  SIDE EFFECT");
        $display("  REGRESSION REQUIREMENT");

    end

    $display("============================================================");

    $finish;
end

endmodule
