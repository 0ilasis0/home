---

name: rtl-verification
description: Create and evaluate RTL testbenches, simulation scenarios, assertions, functional coverage, and requirement-based hardware verification.
-----------------------------------------------------------------------------------------------------------------------------------------------------

# RTL Verification

## Objective

Verify RTL behavior against explicit requirements using reproducible tests and observable evidence.

## 1. Establish the Verification Basis

Identify:

* Design specification and interface contract
* Clock and reset behavior
* Expected latency and throughput
* Protocol rules and error semantics
* Parameters and supported configurations
* Existing testbench structure and simulation toolchain
* Required acceptance criteria

Do not derive expected behavior solely from the implementation being tested. Prefer an independent specification or approved reference model.

## 2. Derive Test Scenarios

Cover applicable categories:

* Reset assertion and release
* Normal transactions
* Minimum and maximum supported values
* Boundary conditions and illegal inputs
* Back-to-back transactions
* Stall and backpressure
* FIFO full and empty conditions
* Simultaneous read and write operations
* Error reporting and recovery
* Parameter and configuration variants
* Long-duration or randomized sequences where useful

Prioritize tests based on requirements and risk rather than maximizing test count.

## 3. Testbench Design

A testbench should, where appropriate:

* Generate clocks and reset sequences
* Drive inputs according to protocol timing
* Monitor observable outputs
* Check expected behavior using assertions or scoreboards
* Track outstanding transactions and transaction ordering
* Detect timeouts, deadlocks, and unexpected inactivity
* Produce clear failure messages and reproducible results

Avoid testbench assumptions that duplicate the same implementation error as the design.

Use deterministic seeds or record random seeds when randomized testing is used.

## 4. Assertions and Scoreboards

Use assertions to check important invariants, such as:

* Output stability during a stall, when required by the protocol
* Legal state transitions
* Correct handshake behavior
* No unintended transaction loss or duplication
* Correct ordering of responses
* Reset-related constraints
* Bounds and consistency of counters or indices

Use scoreboards or reference models for transaction-level comparisons when appropriate.

Assertions must reflect the approved specification, not merely the current RTL behavior.

## 5. Simulation Execution

When running simulation:

1. Record the simulator and relevant configuration.
2. Build or compile the testbench and design.
3. Execute the selected tests.
4. Inspect exit status, logs, assertion failures, and relevant waveforms.
5. Investigate failures before modifying either the design or the testbench.
6. Re-run affected tests after a correction.
7. Preserve meaningful evidence.

Distinguish a compile failure from a simulation failure and a passing simulation.

## 6. Coverage and Completeness

Use available functional or code coverage where appropriate.

Coverage can help identify untested behavior, but coverage percentage alone does not prove correctness.

Report:

* Requirements mapped to tests
* Scenarios executed
* Scenarios not covered
* Relevant coverage results
* Assertions and scoreboards used
* Known limitations of the testbench

Do not invent coverage results when the simulator does not provide them.

## 7. Failure Triage

For each failure, record:

* Failing test and scenario
* Expected behavior
* Observed behavior
* Relevant signal or transaction trace
* Earliest known divergence
* Whether the failure is reproducible
* Suspected cause and supporting evidence
* Follow-up test required

Do not change expected values merely to make a failing test pass unless the specification itself is corrected through the approved process.

## 8. Verification Boundaries

Keep these activities distinct:

* RTL compilation
* RTL simulation
* Lint and static checks
* Formal verification
* Synthesis
* Static timing analysis
* Gate-level simulation
* Physical hardware validation

One successful stage does not automatically establish success in another.

## 9. Required Report

Summarize:

1. Requirements and acceptance criteria
2. Test scenarios and expected behavior
3. Testbench or assertion changes
4. Commands and tools actually used
5. Pass/fail results and supporting evidence
6. Uncovered scenarios and limitations
7. Recommended next verification step

## Rules

* Prefer requirement-based checks over implementation-dependent expectations.
* Do not claim unexecuted tests passed.
* Do not conceal failures by removing assertions or reducing test coverage.
* Keep verification changes reviewable and reproducible.
