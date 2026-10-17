---

name: firmware-development
description: Implement and verify MCU firmware, bare-metal software, RTOS tasks, peripheral drivers, interrupts, and hardware-dependent behavior.
-------------------------------------------------------------------------------------------------------------------------------------------------

# Firmware Development

## Objective

Implement maintainable firmware that respects the target hardware specification, execution environment, and approved interfaces.

## 1. Inspect the Target

Identify:

* MCU or processor model and relevant silicon revision
* Reference manual, datasheet, and errata
* Compiler, SDK, startup code, linker script, and build system
* Bare-metal or RTOS environment
* Clock configuration, memory layout, and peripheral configuration
* Existing driver abstractions and coding conventions
* Available debugger, simulator, and target hardware

Do not invent register addresses, bit fields, interrupt numbers, or peripheral behavior.

If required hardware documentation is missing, identify the missing information before making hardware-specific assumptions.

## 2. Preserve Initialization and Resource Contracts

Inspect the dependencies among:

* Reset and startup code
* Clock and power initialization
* Pin multiplexing and peripheral setup
* Interrupt controller configuration
* Memory and linker configuration
* RTOS initialization and task startup
* Peripheral and driver initialization order

Do not reorder initialization or change memory configuration without understanding the consequences.

## 3. Implement Peripheral and Driver Logic

Where applicable:

* Respect register access width and read/write semantics.
* Distinguish read-only, write-only, write-one-to-clear, and reserved fields.
* Consider alignment, volatile access, memory barriers, and cache maintenance where required by the platform.
* Validate buffer sizes and input parameters.
* Handle peripheral busy states, timeouts, error flags, and recovery.
* Preserve existing interfaces unless a change has been approved.

Use vendor-provided definitions or verified project headers when available.

`volatile` alone does not guarantee atomicity, synchronization, or cache coherency.

## 4. Interrupts, DMA, and Concurrency

For interrupt-driven or concurrent code, analyze:

* Interrupt priority and execution constraints
* Shared state and race conditions
* Critical sections and synchronization
* ISR execution time and deferred processing
* DMA buffer ownership and transfer completion
* Cache coherency and memory ordering
* Buffer lifetime and alignment
* Timeout and cancellation behavior

Avoid lengthy blocking operations in interrupt context unless the platform and requirements explicitly permit them.

Do not assume that a shared variable is safe merely because it is declared volatile.

## 5. RTOS-Specific Considerations

When using an RTOS, inspect:

* Task priorities and scheduling assumptions
* Stack size and stack overflow protection
* Mutexes, semaphores, queues, and event mechanisms
* Deadlock and priority inversion risks
* Task and interrupt context restrictions
* Watchdog interaction
* Timing and deadline requirements

Do not introduce new tasks or synchronization mechanisms without considering resource usage and scheduling effects.

## 6. Build and Test

Use the project's prescribed toolchain and build settings.

Where available, perform relevant checks such as:

* Compilation and linker checks
* Static analysis and warnings review
* Unit tests and mocked peripheral tests
* Host-based tests for platform-independent logic
* Hardware-in-the-loop tests
* On-target debugging and functional tests
* Fault injection and recovery tests

Compilation success is not evidence that firmware behaves correctly on the target.

## 7. Required Report

For meaningful changes, report:

* Target hardware and toolchain assumptions
* Files and interfaces changed
* Register, interrupt, DMA, or RTOS implications
* Build and tests actually performed
* Observed failures and warnings
* Hardware validation status
* Remaining risks and unverified behavior

## Rules

* Do not guess hardware register definitions.
* Do not hide compiler warnings or weaken checks merely to obtain a successful build.
* Do not claim target-hardware validation without actual target-hardware evidence.
* Preserve approved memory maps, calling conventions, and external interfaces.
