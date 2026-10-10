---

name: gate-level-sim
description: Plan and analyze gate-level simulation using synthesized or implemented netlists, timing models, simulator configuration, and RTL-to-netlist comparisons.
----------------------------------------------------------------------------------------------------------------------------------------------------------------------

# Gate-Level Simulation

## Objective

Use gate-level simulation (GLS) when required to investigate implementation-level behavior, timing-related effects, reset behavior, or differences between RTL and a mapped netlist.

GLS complements other verification methods. It does not replace static timing analysis or physical hardware validation.

## 1. Confirm the Purpose

Before starting, determine the question GLS is intended to answer.

Examples include:

* Whether the mapped netlist preserves expected functional behavior
* Whether reset and initialization sequences behave as expected
* Whether timing checks reveal violations in selected scenarios
* Whether implementation-level delays expose a relevant race or timing sensitivity
* Whether RTL and gate-level results diverge

Do not run GLS merely because it is available. Choose it when it addresses a concrete verification requirement or risk.

## 2. Identify Required Inputs

Inspect the available implementation flow and identify:

* Synthesized or implemented netlist
* Cell or primitive simulation models
* Timing annotation files, such as SDF, when required
* Technology libraries and relevant model versions
* Simulator and compilation configuration
* Clock and reset definitions
* Timing constraints and implementation settings
* Testbench compatibility requirements

Record the provenance and version of each critical artifact when available.

Do not assume that an SDF file, cell model, or complete netlist exists.

## 3. Understand the Simulation Mode

Distinguish among:

* Functional gate-level simulation without annotated delays
* Timing-annotated gate-level simulation
* Simulations with timing checks enabled or disabled
* Tool- or vendor-specific simulation modes

Identify which mode is actually configured.

Do not describe a functional netlist simulation as a timing-annotated simulation if delays were not applied.

## 4. Prepare the Simulation Environment

Where applicable:

* Compile the correct netlist and cell models.
* Use the supported simulator configuration.
* Resolve model dependencies and compilation ordering.
* Configure SDF annotation according to the tool's supported syntax.
* Inspect annotation warnings and errors.
* Confirm that relevant timing checks are enabled as intended.
* Check testbench assumptions that may be specific to RTL simulation.

An apparently successful simulation setup is insufficient if required timing annotations or models were not loaded correctly.

## 5. Run and Inspect Tests

Reuse requirement-based test scenarios from RTL verification when they remain valid for the gate-level environment.

Pay particular attention to:

* Reset assertion and release
* Initialization and startup sequences
* Clock and interface timing
* Protocol handshakes
* Critical data paths
* Timing-check violations
* Differences in observable behavior
* Testbench races or assumptions about zero-delay behavior

Inspect simulator logs, annotation reports, assertion failures, timing violations, and waveforms as appropriate.

## 6. Compare RTL and Gate-Level Results

When results differ:

1. Identify the first meaningful behavioral divergence.
2. Confirm that both simulations use equivalent stimulus and configuration where appropriate.
3. Check whether the netlist, cell models, and timing annotation are consistent.
4. Inspect reset behavior, timing checks, and testbench scheduling assumptions.
5. Determine whether the difference indicates a design issue, expected implementation behavior, or a simulation setup problem.
6. Record evidence before recommending changes.

Do not automatically treat every gate-level timing violation as proof of a silicon failure. Investigate the constraints, models, and operating assumptions.

## 7. Interpret Results Correctly

Distinguish:

* Functional failures
* Timing-check violations
* Missing or incomplete timing annotation
* Unresolved simulation models
* Testbench incompatibilities
* Simulator setup failures
* Expected differences between RTL and gate-level models

GLS is not a substitute for static timing analysis, CDC analysis, formal verification, or hardware testing. Select the appropriate method for the property being investigated.

## 8. Required Report

Include:

1. Verification objective
2. Netlist and model provenance
3. Simulator and simulation mode
4. Timing annotation status, if applicable
5. Tests executed
6. Functional results and timing-check results
7. Relevant warnings, failures, and evidence
8. Differences from RTL simulation
9. Unverified conditions and recommended next steps

## Rules

* Never claim timing annotation succeeded without inspecting the available evidence.
* Never report a timing-clean design based only on a passing GLS run.
* Do not ignore timing violations without documenting their cause and disposition.
* Do not modify implementation constraints or suppress timing checks simply to obtain a clean simulation result.
* Keep simulation setup failures distinct from design failures.
