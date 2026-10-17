---

name: embedded-development
description: Develop and integrate embedded systems across hardware, firmware, drivers, communication interfaces, build systems, and system-level verification.
---------------------------------------------------------------------------------------------------------------------------------------------------------------

# Embedded Development

## Objective

Provide a system-level workflow for embedded projects that combine hardware, firmware, software, and external interfaces.

Use this skill when a task spans multiple components or when the correct behavior depends on hardware-software interaction.

## 1. Establish the System Context

Identify:

* Target hardware and processor or FPGA
* Relevant schematics, block diagrams, and hardware manuals
* Operating system or bare-metal environment
* Compiler, SDK, build system, and toolchain versions
* Communication interfaces and protocols
* Memory layout and resource constraints
* Boot, initialization, shutdown, and recovery requirements
* Available simulators, debuggers, and hardware test equipment

Do not assume that a toolchain, board, peripheral, or debugging interface is available without evidence.

## 2. Define Component Boundaries

Describe responsibilities and contracts among:

* Hardware logic and peripherals
* Bootloader and firmware
* Device drivers and operating-system services
* Application software
* Host-side utilities and diagnostic tools
* External devices and communication links

For each interface, identify applicable data formats, ownership, timing, error behavior, initialization order, and compatibility constraints.

Use explicit specifications instead of relying on implicit assumptions.

## 3. Analyze Hardware-Software Interaction

Where relevant, inspect:

* Register maps and register access semantics
* Interrupt routing and interrupt-service behavior
* DMA descriptors, buffer ownership, and completion handling
* Cache coherency and memory ordering
* Address translation, alignment, and memory protection
* Reset sequencing and peripheral initialization
* Clock and power management
* Timeout, retry, recovery, and fault handling

Only apply checks relevant to the actual platform and design.

Do not assume that CPU-visible memory and device-visible memory are automatically coherent.

## 4. Plan Implementation and Integration

Prefer an incremental sequence:

1. Confirm requirements and interface contracts.
2. Validate the smallest relevant hardware or software component.
3. Implement one component at a time.
4. Test the component independently where possible.
5. Integrate components using explicit interface assumptions.
6. Test normal operation, failures, and recovery.
7. Record the final configuration and remaining limitations.

Keep integration changes separate from unrelated refactoring.

## 5. Debug System-Level Failures

When a failure crosses component boundaries:

* Identify the earliest observable point where behavior diverges from expectations.
* Collect logs, register values, traces, status codes, and reproducible conditions.
* Separate symptoms from root-cause hypotheses.
* Check whether the issue originates in hardware, firmware, drivers, protocol assumptions, or configuration.
* Change one meaningful variable at a time when practical.
* Preserve enough evidence to compare behavior before and after a fix.

Do not attribute a failure to hardware or software without supporting evidence.

## 6. Verification Strategy

Select a layered strategy as appropriate:

* Static analysis and code review
* Component-level tests
* Protocol and interface tests
* Integration tests
* Simulation or emulation
* Hardware-in-the-loop testing
* Actual target-system validation
* Stress, endurance, and fault-recovery testing

Document which layers were executed and which remain outstanding.

A successful software build does not prove correct hardware interaction.

## 7. Deliverables

For a meaningful embedded-system change, report:

* System assumptions and hardware dependencies
* Affected components and interface contracts
* Configuration and toolchain requirements
* Implementation and integration sequence
* Verification performed and observed results
* Known limitations, risks, and unresolved issues

## Rules

* Respect approved hardware and software interfaces.
* Do not silently change register maps, protocols, memory layouts, or initialization sequences.
* Avoid assuming behavior that has not been confirmed by the hardware specification or measurement.
* Distinguish simulation results from actual target-hardware results.
* Follow the project's change-impact and verification rules.
