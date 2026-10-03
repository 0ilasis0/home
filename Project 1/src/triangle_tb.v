`timescale 1ns/1ps

// ============================================================================
// Digital Circuit Design and Analysis 115-1 Project I
// Triangle Rendering Engine
//
// RTL Verification Engineer — AI-3
// Task 1 Verification Baseline
//
// IMPORTANT:
//   - This is a verification-only testbench.
//   - Production RTL is NOT modified by this file.
//   - Expected geometry is generated independently from the RTL architecture.
//   - Do NOT change expected behavior to accommodate DUT behavior.
//   - RTL version / baseline ID is intentionally NOT defined here.
//
// Verification scope:
//   1. Interface
//   2. Asynchronous active-high reset
//   3. Input capture protocol
//   4. Busy protocol
//   5. Triangle geometry
//   6. x2 > x1 / x2 < x1
//   7. |dx| = 1 / |dx| > 1
//   8. Output inclusion
//   9. Output ordering
//  10. Registered output timing
//  11. Release timing
//  12. Corner cases
//  13. Exhaustive legal input space: 3136 triangles
//
// Reference model:
//   Independent point-in-triangle mathematical model.
//   It does NOT copy the DUT's column/tracer implementation.
//
// ============================================================================

module triangle_tb;

    // ========================================================================
    // DUT interface
    // ========================================================================

    reg         clk;
    reg         reset;
    reg         nt;
    reg  [2:0]  xi;
    reg  [2:0]  yi;

    wire        busy;
    wire        po;
    wire [2:0]  xo;
    wire [2:0]  yo;


    // ========================================================================
    // DUT
    // ========================================================================

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


    // ========================================================================
    // Clock
    // ========================================================================

    parameter CLK_PERIOD = 10;

    initial begin
        clk = 1'b0;

        forever begin
            #(CLK_PERIOD / 2);
            clk = ~clk;
        end
    end


    // ========================================================================
    // Global verification bookkeeping
    // ========================================================================

    integer cycle_count;

    integer total_tests;
    integer pass_tests;
    integer fail_tests;

    integer error_count;

    integer current_test_id;

    reg test_failed;

    always @(posedge clk) begin
        cycle_count = cycle_count + 1;
    end


    // ========================================================================
    // Independent Reference Model
    //
    // Maximum possible number of points for the legal triangle domain is well
    // below 64, therefore 64 entries are reserved.
    //
    // The expected output ordering is generated as:
    //
    //     for y = y1 -> y3
    //         for x = x_left -> x_right
    //
    // A mathematical point-in-triangle test determines whether the point
    // belongs to the triangle.
    //
    // This does NOT use:
    //     ylow[]
    //     yup[]
    //     E_trace
    //     LOWER tracer
    //     UPPER tracer
    //     DUT FSM
    //
    // ========================================================================

    reg [2:0] expected_x [0:63];
    reg [2:0] expected_y [0:63];

    integer expected_count;
    integer expected_index;


    // ========================================================================
    // Mathematical reference model
    //
    // Cross product:
    //
    // cross(A,B,P)
    //   = (Bx-Ax)*(Py-Ay)
    //     - (By-Ay)*(Px-Ax)
    //
    // A point is inside or on the boundary when all three edge cross products
    // are non-negative OR all three are non-positive.
    //
    // integer is intentionally used only in the verification environment.
    // It is NOT production RTL.
    // ========================================================================

    task point_inside_triangle;
        input  integer px;
        input  integer py;

        integer c1;
        integer c2;
        integer c3;

        integer ax;
        integer ay;
        integer bx;
        integer by;

        begin
            // Edge P1 -> P2
            ax = ref_x1;
            ay = ref_y1;
            bx = ref_x2;
            by = ref_y2;

            c1 = (bx - ax) * (py - ay)
               - (by - ay) * (px - ax);


            // Edge P2 -> P3
            ax = ref_x2;
            ay = ref_y2;
            bx = ref_x3;
            by = ref_y3;

            c2 = (bx - ax) * (py - ay)
               - (by - ay) * (px - ax);


            // Edge P3 -> P1
            ax = ref_x3;
            ay = ref_y3;
            bx = ref_x1;
            by = ref_y1;

            c3 = (bx - ax) * (py - ay)
               - (by - ay) * (px - ax);


            if (((c1 >= 0) && (c2 >= 0) && (c3 >= 0)) ||
                ((c1 <= 0) && (c2 <= 0) && (c3 <= 0))) begin
                point_inside_triangle = 1;
            end
            else begin
                point_inside_triangle = 0;
            end
        end
    endtask


    // ========================================================================
    // Reference triangle coordinates
    // ========================================================================

    integer ref_x1;
    integer ref_y1;
    integer ref_x2;
    integer ref_y2;
    integer ref_x3;
    integer ref_y3;

    integer ref_x_left;
    integer ref_x_right;

    integer ref_dx;


    // ========================================================================
    // Build independent expected output list
    //
    // Ordering:
    //     y ascending
    //     x ascending within same y
    //
    // ========================================================================

    task build_expected;
        integer x;
        integer y;
        integer inside;

        begin
            expected_count = 0;

            ref_x_left  = (ref_x1 < ref_x2) ? ref_x1 : ref_x2;
            ref_x_right = (ref_x1 > ref_x2) ? ref_x1 : ref_x2;

            ref_dx = ref_x2 - ref_x1;

            for (y = ref_y1; y <= ref_y3; y = y + 1) begin

                for (x = ref_x_left; x <= ref_x_right; x = x + 1) begin

                    point_inside_triangle(x, y);

                    if (point_inside_triangle) begin
                        expected_x[expected_count] = x[2:0];
                        expected_y[expected_count] = y[2:0];

                        expected_count = expected_count + 1;
                    end
                end

            end
        end
    endtask


    // ========================================================================
    // Utility: verification failure
    // ========================================================================

    task report_error;
        input [255:0] message;

        begin
            error_count = error_count + 1;
            test_failed = 1'b1;

            $display("");
            $display("============================================================");
            $display("VERIFICATION ERROR");
            $display("Test ID       : %0d", current_test_id);
            $display("Cycle         : %0d", cycle_count);
            $display("Triangle      : P1=(%0d,%0d) P2=(%0d,%0d) P3=(%0d,%0d)",
                     ref_x1, ref_y1,
                     ref_x2, ref_y2,
                     ref_x3, ref_y3);
            $display("Message       : %s", message);
            $display("DUT busy      : %b", busy);
            $display("DUT po        : %b", po);
            $display("DUT xo        : %0d", xo);
            $display("DUT yo        : %0d", yo);
            $display("============================================================");
            $display("");
        end
    endtask


    // ========================================================================
    // Utility: wait for a bounded number of clock cycles
    //
    // This prevents a DUT stuck in a state from hanging the whole regression.
    // ========================================================================

    task wait_one_cycle;
        begin
            @(posedge clk);
            #1;
        end
    endtask


    // ========================================================================
    // Reset verification
    //
    // Active-high asynchronous reset.
    //
    // Expected:
    //     busy = 0
    //     po   = 0
    //
    // Internal FSM/data-path state will be checked after actual RTL is provided
    // and its internal state names are known.
    // ========================================================================

    task verify_reset;
        begin
            $display("");
            $display("------------------------------------------------------------");
            $display("RESET VERIFICATION");
            $display("------------------------------------------------------------");

            reset = 1'b1;
            nt    = 1'b0;
            xi    = 3'd0;
            yi    = 3'd0;

            #2;

            if (busy !== 1'b0) begin
                report_error("Reset assertion: busy is not 0");
            end

            if (po !== 1'b0) begin
                report_error("Reset assertion: po is not 0");
            end

            // Release reset on a clock-safe boundary.
            @(negedge clk);
            reset = 1'b0;

            #1;

            if (busy !== 1'b0) begin
                report_error("After reset release: busy is not 0");
            end

            if (po !== 1'b0) begin
                report_error("After reset release: po is not 0");
            end

            if (!test_failed) begin
                $display("RESET CHECK: PASS (simulation evidence required)");
            end
        end
    endtask


    // ========================================================================
    // Reset during processing
    //
    // This is used to verify asynchronous reset behavior does not leave the
    // external interface in a stale/phantom-output state.
    //
    // Architecture requires reset -> IDLE, busy=0, po=0.
    // ========================================================================

    task verify_reset_during_processing;

        integer timeout;

        begin
            $display("");
            $display("------------------------------------------------------------");
            $display("RESET-DURING-PROCESSING VERIFICATION");
            $display("------------------------------------------------------------");

            // Wait for idle.
            timeout = 0;

            while (busy !== 1'b0) begin
                wait_one_cycle;
                timeout = timeout + 1;

                if (timeout > 20) begin
                    report_error("Cannot reach IDLE before reset-during-processing test");
                    disable verify_reset_during_processing;
                end
            end

            // Apply a legal triangle.
            @(negedge clk);
            nt = 1'b1;
            xi = 3'd0;
            yi = 3'd0;

            @(negedge clk);
            nt = 1'b0;
            xi = 3'd7;
            yi = 3'd3;

            @(negedge clk);
            nt = 1'b0;
            xi = 3'd0;
            yi = 3'd7;

            // Allow DUT to enter processing.
            @(posedge clk);
            #1;

            // Assert asynchronous reset while processing.
            #1;
            reset = 1'b1;

            #1;

            if (busy !== 1'b0) begin
                report_error("Asynchronous reset during processing: busy is not 0");
            end

            if (po !== 1'b0) begin
                report_error("Asynchronous reset during processing: po is not 0");
            end

            @(negedge clk);
            reset = 1'b0;

            #1;

            if (busy !== 1'b0) begin
                report_error("After reset release from processing: busy is not 0");
            end

            if (po !== 1'b0) begin
                report_error("After reset release from processing: po is not 0");
            end

            if (!test_failed) begin
                $display("RESET-DURING-PROCESSING CHECK: PASS (simulation evidence required)");
            end
        end
    endtask


    // ========================================================================
    // Run one legal triangle
    //
    // Input protocol:
    //
    //     C1: nt=1 -> P1
    //     C2: nt=0 -> P2
    //     C3: nt=0 -> P3
    //
    // Inputs are changed at negedge so that the DUT samples stable values at
    // posedge.
    //
    // Output is sampled #1 after posedge because po/xo/yo are registered.
    // ========================================================================

    task run_triangle;

        input integer in_x1;
        input integer in_y1;
        input integer in_x2;
        input integer in_y2;
        input integer in_x3;
        input integer in_y3;

        integer timeout;
        integer output_count;
        integer release_phase;

        reg saw_last_output;
        reg saw_busy_high;

        begin
            test_failed = 1'b0;

            ref_x1 = in_x1;
            ref_y1 = in_y1;

            ref_x2 = in_x2;
            ref_y2 = in_y2;

            ref_x3 = in_x3;
            ref_y3 = in_y3;

            build_expected;

            output_count = 0;
            release_phase = 0;
            saw_last_output = 1'b0;
            saw_busy_high = 1'b0;

            // ------------------------------------------------------------
            // Wait until IDLE
            // ------------------------------------------------------------

            timeout = 0;

            while (busy !== 1'b0) begin
                wait_one_cycle;
                timeout = timeout + 1;

                if (timeout > 1000) begin
                    report_error("Timeout waiting for IDLE");
                    disable run_triangle;
                end
            end

            // ------------------------------------------------------------
            // C1: P1
            // ------------------------------------------------------------

            @(negedge clk);

            nt = 1'b1;
            xi = in_x1[2:0];
            yi = in_y1[2:0];

            @(posedge clk);
            #1;

            // nt is valid only for one clock.
            @(negedge clk);

            nt = 1'b0;
            xi = in_x2[2:0];
            yi = in_y2[2:0];

            // ------------------------------------------------------------
            // C2: P2
            // ------------------------------------------------------------

            @(posedge clk);
            #1;

            // Architecture requires busy to become active after C2.
            if (busy !== 1'b1) begin
                report_error("After P2 capture: busy is not 1");
            end

            saw_busy_high = 1'b1;

            // ------------------------------------------------------------
            // C3: P3
            // ------------------------------------------------------------

            @(negedge clk);

            nt = 1'b0;
            xi = in_x3[2:0];
            yi = in_y3[2:0];

            @(posedge clk);
            #1;

            // nt must not remain asserted.
            if (nt !== 1'b0) begin
                report_error("Input protocol: nt is not 0 during P3");
            end

            // busy must remain asserted during processing.
            if (busy !== 1'b1) begin
                report_error("After P3 capture: busy is not 1");
            end

            // Architecture says first output is not produced until the
            // output-scan registered-output timing point.
            //
            // This check is deliberately explicit because the output is
            // registered.
            if (po !== 1'b0) begin
                report_error("Unexpected output valid on P3 capture edge");
            end

            // ------------------------------------------------------------
            // Output / release monitoring
            // ------------------------------------------------------------

            timeout = 0;

            forever begin

                @(posedge clk);
                #1;

                timeout = timeout + 1;

                if (timeout > 5000) begin
                    report_error("Timeout waiting for triangle completion");
                    disable run_triangle;
                end

                // --------------------------------------------------------
                // Valid output
                // --------------------------------------------------------

                if (po === 1'b1) begin

                    if (output_count >= expected_count) begin
                        report_error("Extra output point after all expected points");
                    end
                    else begin

                        if (xo !== expected_x[output_count]) begin
                            report_error("Output X does not match independent reference ordering");
                        end

                        if (yo !== expected_y[output_count]) begin
                            report_error("Output Y does not match independent reference ordering");
                        end
                    end

                    output_count = output_count + 1;

                    // Last expected point.
                    if (output_count == expected_count) begin
                        saw_last_output = 1'b1;

                        // Last valid point must have busy=1.
                        if (busy !== 1'b1) begin
                            report_error("Last valid point does not have busy=1");
                        end
                    end
                end

                // --------------------------------------------------------
                // After last output:
                //
                // N:
                //     po=1 busy=1
                //
                // N+1:
                //     po=0 busy=1
                //
                // N+2:
                //     po=0 busy=0
                // --------------------------------------------------------

                if (saw_last_output) begin

                    if (release_phase == 0) begin

                        // This branch executes on the cycle immediately
                        // following the last valid point.
                        //
                        // The last point was already observed in the previous
                        // iteration, so po must now be 0 and busy must remain 1.

                        if (po !== 1'b0) begin
                            report_error("Cycle after last output: po is not 0");
                        end

                        if (busy !== 1'b1) begin
                            report_error("Cycle after last output: busy is not 1");
                        end

                        release_phase = 1;
                    end
                    else begin

                        // Release-complete cycle.
                        if (po !== 1'b0) begin
                            report_error("Release complete: po is not 0");
                        end

                        if (busy !== 1'b0) begin
                            report_error("Release complete: busy is not 0");
                        end

                        break;
                    end
                end

            end

            // ------------------------------------------------------------
            // Final per-triangle checks
            // ------------------------------------------------------------

            if (!saw_busy_high) begin
                report_error("Triangle never entered busy state");
            end

            if (output_count != expected_count) begin
                report_error("Output point count does not match reference model");
            end

            // ------------------------------------------------------------
            // Result
            // ------------------------------------------------------------

            if (test_failed) begin
                fail_tests = fail_tests + 1;

                $display("TEST %0d: FAIL", current_test_id);
                $display("  P1=(%0d,%0d) P2=(%0d,%0d) P3=(%0d,%0d)",
                         ref_x1, ref_y1,
                         ref_x2, ref_y2,
                         ref_x3, ref_y3);
                $display("  Expected points = %0d", expected_count);
                $display("  Actual outputs  = %0d", output_count);
            end
            else begin
                pass_tests = pass_tests + 1;

                $display("TEST %0d: PASS  P1=(%0d,%0d) P2=(%0d,%0d) P3=(%0d,%0d) points=%0d",
                         current_test_id,
                         ref_x1, ref_y1,
                         ref_x2, ref_y2,
                         ref_x3, ref_y3,
                         expected_count);
            end

            total_tests = total_tests + 1;

        end
    endtask


    // ========================================================================
    // Directed example from Architecture Specification
    //
    // P1=(1,1)
    // P2=(6,3)
    // P3=(1,6)
    //
    // Expected:
    //
    // (1,1)
    // (1,2) (2,2) (3,2)
    // (1,3) (2,3) (3,3) (4,3) (5,3) (6,3)
    // (1,4) (2,4) (3,4) (4,4)
    // (1,5) (2,5)
    // (1,6)
    //
    // The independent model generates this list rather than hard-coding
    // expected points, but this specific triangle is explicitly exercised.
    // ========================================================================

    task directed_architecture_example;
        begin
            current_test_id = 1;

            run_triangle(
                1, 1,
                6, 3,
                1, 6
            );
        end
    endtask


    // ========================================================================
    // Directed geometry classes
    // ========================================================================

    task directed_dx_plus_one;
        begin
            current_test_id = current_test_id + 1;

            // x2 > x1, |dx| = 1
            run_triangle(
                1, 1,
                2, 4,
                1, 7
            );
        end
    endtask


    task directed_dx_minus_one;
        begin
            current_test_id = current_test_id + 1;

            // x2 < x1, |dx| = 1
            run_triangle(
                6, 0,
                5, 3,
                6, 7
            );
        end
    endtask


    task directed_dx_plus_many;
        begin
            current_test_id = current_test_id + 1;

            // x2 > x1, |dx| > 1
            run_triangle(
                0, 0,
                7, 3,
                0, 7
            );
        end
    endtask


    task directed_dx_minus_many;
        begin
            current_test_id = current_test_id + 1;

            // x2 < x1, |dx| > 1
            run_triangle(
                7, 0,
                0, 3,
                7, 7
            );
        end
    endtask


    // ========================================================================
    // Boundary cases
    // ========================================================================

    task directed_boundary_cases;
        begin
            // x1 = 0, x2 = 7
            current_test_id = current_test_id + 1;

            run_triangle(
                0, 0,
                7, 1,
                0, 7
            );


            // x1 = 7, x2 = 0
            current_test_id = current_test_id + 1;

            run_triangle(
                7, 0,
                0, 1,
                7, 7
            );


            // y1 = 0
            current_test_id = current_test_id + 1;

            run_triangle(
                3, 0,
                7, 4,
                3, 7
            );


            // y3 = 7
            current_test_id = current_test_id + 1;

            run_triangle(
                0, 0,
                4, 3,
                0, 7
            );


            // x boundary and y boundary combination
            current_test_id = current_test_id + 1;

            run_triangle(
                0, 0,
                7, 6,
                0, 7
            );
        end
    endtask


    // ========================================================================
    // Exhaustive legal triangle verification
    //
    // Legal domain:
    //
    //   x1 = x3
    //   y1 < y2 < y3
    //   x2 != x1
    //
    // Number:
    //
    //   8 * C(8,3) * 7
    // = 8 * 56 * 7
    // = 3136
    //
    // This is independent exhaustive functional geometry verification.
    // ========================================================================

    task exhaustive_legal_triangles;

        integer x1_loop;
        integer x2_loop;
        integer y1_loop;
        integer y2_loop;
        integer y3_loop;

        begin
            $display("");
            $display("============================================================");
            $display("EXHAUSTIVE LEGAL TRIANGLE VERIFICATION");
            $display("Expected legal triangle count = 3136");
            $display("============================================================");
            $display("");

            for (x1_loop = 0; x1_loop <= 7; x1_loop = x1_loop + 1) begin

                for (x2_loop = 0; x2_loop <= 7; x2_loop = x2_loop + 1) begin

                    if (x2_loop != x1_loop) begin

                        for (y1_loop = 0; y1_loop <= 7; y1_loop = y1_loop + 1) begin

                            for (y2_loop = y1_loop + 1;
                                 y2_loop <= 7;
                                 y2_loop = y2_loop + 1) begin

                                for (y3_loop = y2_loop + 1;
                                     y3_loop <= 7;
                                     y3_loop = y3_loop + 1) begin

                                    current_test_id = current_test_id + 1;

                                    run_triangle(
                                        x1_loop,
                                        y1_loop,
                                        x2_loop,
                                        y2_loop,
                                        x1_loop,
                                        y3_loop
                                    );

                                end
                            end
                        end
                    end
                end
            end
        end
    endtask


    // ========================================================================
    // Main verification sequence
    // ========================================================================

    initial begin

        // ------------------------------------------------------------
        // Initial values
        // ------------------------------------------------------------

        reset = 1'b1;
        nt    = 1'b0;
        xi    = 3'd0;
        yi    = 3'd0;

        cycle_count = 0;

        total_tests = 0;
        pass_tests  = 0;
        fail_tests  = 0;

        error_count = 0;

        current_test_id = 0;

        test_failed = 1'b0;


        // ------------------------------------------------------------
        // Reset verification
        // ------------------------------------------------------------

        verify_reset;


        // ------------------------------------------------------------
        // Reset during processing
        // ------------------------------------------------------------

        current_test_id = current_test_id + 1;

        verify_reset_during_processing;


        // ------------------------------------------------------------
        // Restore clean reset state before functional verification
        // ------------------------------------------------------------

        reset = 1'b1;
        nt    = 1'b0;
        xi    = 3'd0;
        yi    = 3'd0;

        #2;

        @(negedge clk);
        reset = 1'b0;

        #1;


        // ------------------------------------------------------------
        // Directed architecture example
        // ------------------------------------------------------------

        directed_architecture_example;


        // ------------------------------------------------------------
        // Geometry direction / dx classes
        // ------------------------------------------------------------

        directed_dx_plus_one;
        directed_dx_minus_one;
        directed_dx_plus_many;
        directed_dx_minus_many;


        // ------------------------------------------------------------
        // Coordinate boundary cases
        // ------------------------------------------------------------

        directed_boundary_cases;


        // ------------------------------------------------------------
        // Exhaustive legal input space
        // ------------------------------------------------------------

        exhaustive_legal_triangles;


        // ------------------------------------------------------------
        // Final report
        // ------------------------------------------------------------

        $display("");
        $display("============================================================");
        $display("RTL VERIFICATION TASK 1 SUMMARY");
        $display("============================================================");
        $display("Total tests      : %0d", total_tests);
        $display("Passed tests     : %0d", pass_tests);
        $display("Failed tests     : %0d", fail_tests);
        $display("Error count      : %0d", error_count);
        $display("Cycle count      : %0d", cycle_count);
        $display("============================================================");

        if (fail_tests != 0 || error_count != 0) begin
            $display("FINAL STATUS: FAIL");
        end
        else begin
            $display("FINAL STATUS: PASS");
        end

        $display("NOTE: Final verification status requires retained simulation evidence.");
        $display("============================================================");
        $display("");

        $finish;
    end

endmodule