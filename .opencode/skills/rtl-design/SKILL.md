---

name: rtl-design
description: Design and modify synthesizable Verilog and SystemVerilog RTL while preserving interface, timing, reset, and protocol requirements.
------------------------------------------------------------------------------------------------------------------------------------------------

# RTL Design

## Objective

Implement hardware behavior that conforms to approved specifications and can be verified through appropriate RTL and implementation flows.

## 1. Inspect Before Coding

Read the relevant:

* Functional and interface specifications
* Existing RTL modules and their dependencies
* Clock and reset definitions
* Parameter and macro definitions
* Timing and latency requirements
* Protocol specifications
* Testbenches and verification plans
* Synthesis constraints and tool settings, when available

Identify the intended behavior before changing the implementation.

## 2. Preserve the Module Contract

Do not change the following without approval:

* Module names and port definitions
* Signal widths, signedness, and parameter defaults
* Clock and reset polarity or behavior
* Protocol handshakes
* Pipeline depth and observable latency
* Throughput and buffering requirements
* Error and backpressure semantics

If an interface change is necessary, document the reason and identify every affected instantiation, testbench, and downstream component.

## 3. Sequential and Combinational Logic

For sequential logic:

* Use nonblocking assignments for conventional clocked state updates.
* Define reset behavior consistently with the approved specification.
* Ensure state transitions and register updates match the intended cycle-level behavior.
* Consider initialization, recovery, and illegal-state behavior where relevant.

For combinational logic:

* Ensure all outputs are assigned on every applicable path.
* Avoid unintended latch inference.
* Define default values and handle all relevant cases.
* Avoid accidental combinational feedback.

Follow the existing project coding standard when it is consistent with the design requirements.

## 4. Widths and Data Semantics

Explicitly consider:

* Operand widths and signedness
* Sign extension and zero extension
* Truncation and overflow
* Unsized literals and implicit conversions
* Parameterized widths and boundary configurations
* Array bounds and indexing
* Arithmetic behavior at minimum and maximum values

Do not assume that an expression has the intended width merely because it compiles.

## 5. Clock Domains and Reset

Identify each clock domain and the signals crossing between domains.

Where applicable:

* Use an approved clock-domain crossing strategy.
* Synchronize asynchronous control signals appropriately.
* Use suitable transfer mechanisms for multi-bit data.
* Consider reset assertion and deassertion requirements.
* Check reset release behavior across related clock domains.
* Avoid introducing unsynchronized crossings or combinational clock gating.

Do not treat a two-flop synchronizer as a universal solution for multi-bit buses or arbitrary protocols.

## 6. Pipeline, Handshake, and Protocol Behavior

For streaming or transaction-based designs, inspect:

* Valid/ready or equivalent handshakes
* Backpressure propagation
* FIFO full and empty handling
* Simultaneous enqueue and dequeue
* Packet boundaries and metadata
* Stall behavior and state retention
* Throughput, latency, and transaction ordering
* Error, abort, and recovery behavior

Check that data and associated metadata remain aligned across pipeline stages.

Do not change latency or throughput assumptions without updating the specification and verification plan.

## 7. Synthesis and Implementation Awareness

Consider:

* Whether the code is synthesizable in the intended toolchain
* Inferred registers, memories, and logic structures
* Reset and enable inference
* Resource and fan-out implications
* Timing constraints and critical paths
* Differences between RTL simulation and synthesized hardware

Do not claim that RTL is synthesis-clean or timing-closed unless the corresponding tools were actually run and their results inspected.

## 8. Verification Expectations

Before completing a meaningful RTL change:

* Identify the requirements affected.
* Review boundary conditions and protocol corner cases.
* Ensure relevant tests cover the changed behavior.
* Run lint, simulation, synthesis, or timing checks when appropriate and available.
* Report unexecuted checks explicitly.

## 9. Deliverables

Report:

* Behavioral changes
* Interfaces and invariants preserved
* Files changed
* Potential synthesis and timing implications
* Verification evidence
* Known limitations and unresolved issues

## Rules

* Never silently alter the approved cycle-level contract.
* Do not use successful compilation as proof of functional correctness.
* Do not suppress warnings without understanding their significance.
* Prefer small, reviewable changes that preserve existing behavior outside the requested scope.
