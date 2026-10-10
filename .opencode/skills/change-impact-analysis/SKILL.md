---

name: change-impact-analysis
description: Analyze change scope, dependencies, interface contracts, regression risks, and required verification before modifying an engineering project.
----------------------------------------------------------------------------------------------------------------------------------------------------------

# Change Impact Analysis

## Objective

Determine what a proposed change may affect before implementation. Identify dependencies, constraints, risks, and required verification.

This skill applies to RTL, FPGA projects, embedded systems, firmware, PC drivers, and general software.

## Workflow

### 1. Understand the Change

Identify:

* The requested objective and expected behavior
* Explicit requirements and acceptance criteria
* Files, modules, interfaces, and components directly involved
* Constraints that must remain unchanged
* Information that is missing or ambiguous

Separate confirmed requirements from assumptions and proposals.

### 2. Inspect Existing Implementation

Before recommending changes:

* Inspect relevant source files and directory structure.
* Read applicable specifications, interface definitions, and architecture decisions.
* Identify callers, callees, dependencies, shared resources, and integration boundaries.
* Locate related tests, build scripts, simulation files, and configuration.
* Use available source-search and dependency-analysis tools when useful.

Do not infer complete system behavior from a single source file when related components may change the result.

### 3. Build the Impact Map

Classify potential impacts into:

* Directly modified components
* Upstream dependencies and input producers
* Downstream consumers
* Public interfaces and data formats
* Timing, concurrency, or synchronization behavior
* Build, deployment, and toolchain configuration
* Tests, simulation environments, and documentation
* Compatibility and regression risks

For hardware-related changes, also consider clock domains, reset behavior, pipeline latency, throughput, buffering, resource usage, and protocol handshakes where applicable.

For firmware and drivers, consider registers, interrupts, DMA, memory ownership, operating-system interfaces, ABI compatibility, and error recovery where applicable.

### 4. Assess Risk

For each meaningful risk, record:

* Description
* Affected component or interface
* Likelihood, if reasonably assessable
* Impact or severity
* Evidence and uncertainty
* Mitigation
* Verification required

Do not assign precise probabilities without supporting data.

Prioritize risks that could cause interface incompatibility, data corruption, deadlock, timing violations, hardware failure, or loss of required functionality.

### 5. Define the Change Boundary

Explicitly state:

* What may be modified
* What must remain unchanged
* Which decisions require approval
* Which adjacent components require inspection
* Whether the requested scope is sufficient

Do not silently expand the scope to include unrelated refactoring.

If the change requires altering an approved interface, protocol, timing contract, or architecture decision, request approval before implementing that alteration.

### 6. Derive Verification Requirements

Map each affected requirement or behavior to one or more appropriate checks:

* Static inspection or lint
* Unit or integration tests
* RTL simulation
* CDC or reset-related analysis
* Synthesis and timing analysis
* Gate-level simulation
* Firmware or driver tests
* Target hardware testing

Select methods appropriate to the actual risk. Do not prescribe every verification type for every change.

### 7. Report Findings

Produce the following sections:

1. Change objective
2. Confirmed facts and assumptions
3. Directly affected components
4. Indirect dependencies and interfaces
5. Invariants that must be preserved
6. Risk assessment
7. Proposed change boundary
8. Required verification
9. Questions and approvals needed
10. Recommended next step

## Rules

* This skill analyzes impact; it does not automatically authorize implementation.
* Do not claim that an interface or dependency is unaffected without sufficient evidence.
* Distinguish observed dependencies from suspected dependencies.
* If repository information is incomplete, identify the limitation.
* Prefer the smallest change that satisfies the approved requirements.
