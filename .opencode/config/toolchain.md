# Toolchain Configuration and Execution Policy

## 1. Purpose and Scope

This document defines the development toolchains, local executable paths, applicable engineering tasks, execution constraints, and verification evidence requirements for this project.

The purpose is to ensure that all Agents:

- Identify the actual project structure and required toolchain before execution.
- Use the correct tools for the target platform.
- Obtain the required authorization before operations that may modify project files, configuration, or hardware state.
- Distinguish environment availability from successful execution.
- Report results based on actual, inspectable evidence.

This document describes the current Windows development environment. Paths are machine-specific and must not be assumed to exist on other computers.

## 2. General Toolchain Policy

### 2.1 Pre-execution Checks

Before invoking a development tool, the responsible Agent must:

1. Identify the current working directory and project root.
2. Confirm that the required executable exists and can be invoked.
3. Determine the installed tool version where applicable.
4. Inspect the repository to identify the actual project structure and build system.
5. Identify required dependencies, configuration files, SDKs, libraries, licenses, and hardware connections.
6. Review the applicable Agent permissions and obtain user approval when required.
7. Determine whether the planned operation may modify files, regenerate source code, change configuration, or affect connected hardware.

Do not assume that installing an IDE or FPGA development suite means every compiler, simulator, debugger, license, or dependency is available.

### 2.2 Authorization and Change Control

All tool operations must comply with the Agent permission configuration and the project-wide `AGENTS.md` rules.

The following operations require the applicable authorization before execution:

- Modifying an existing project configuration.
- Regenerating source code or IP cores.
- Overwriting or deleting existing files.
- Changing MCU clock settings, pin assignments, peripheral configuration, FPGA constraints, or IP configuration.
- Downloading firmware, programming hardware, or performing operations that may affect connected devices.
- Generating a bitstream or performing other explicitly restricted release operations.

An Agent must not bypass permission restrictions by invoking a shell command, script, IDE, or another tool.

Read-only inspection should be preferred when the task can be completed without making changes.

### 2.3 Verification and Evidence

Every build or verification claim must be supported by actual execution evidence.

Where applicable, record:

- Tool name and version.
- Working directory and target project.
- Exact command or execution procedure.
- Exit status.
- Relevant log paths and diagnostic output.
- Files generated or modified.
- Verification scope and acceptance criteria.
- Known limitations and unverified items.

Use the following result statuses consistently:

| Status | Meaning |
|---|---|
| PASS | The specified check was executed and its acceptance criteria were satisfied. |
| FAIL | The check was executed and one or more acceptance criteria were not satisfied. |
| BLOCKED | Execution could not proceed because of a missing dependency, authorization, environment, or other blocking condition. |
| NOT RUN | The check was not executed. |
| INCONCLUSIVE | Execution occurred, but the available evidence is insufficient to determine the result. |
| NOT APPLICABLE | The check does not apply to the current task, with justification provided. |

A successful tool launch alone does not establish that a build, simulation, or test passed.

Host unit tests, target hardware tests, RTL simulation, synthesis, implementation, timing analysis, and hardware programming must be reported as separate activities.

## 3. STM32 Development Toolchain

### 3.1 Target and Environment

- Operating system: Windows
- Target MCU: STM32G431RB

### 3.2 STM32CubeMX

**Executable**

`C:\Users\user\AppData\Local\Programs\STM32CubeMX\STM32CubeMX.exe`

**Purpose**

- MCU peripheral configuration.
- Clock tree configuration.
- Pinout configuration.
- STM32 project configuration.
- Source-code generation.

**Execution constraints**

1. Verify that the executable exists before invoking it.
2. Inspect the repository to identify existing `.ioc` files and generated source files.
3. Do not modify an existing `.ioc` project without the required user approval.
4. Do not regenerate source code in a way that overwrites existing files without the required approval.
5. Do not change the target MCU, clock configuration, pin assignments, peripheral settings, or other project configuration without authorization.
6. After an authorized configuration change, inspect the resulting diff and generated files to identify unintended changes.

Opening CubeMX or inspecting a project does not imply authorization to modify or regenerate it.

### 3.3 STM32CubeIDE

**Installation directory**

`C:\ST\STM32CubeIDE_2.2.0`

**Executable**

`C:\ST\STM32CubeIDE_2.2.0\STM32CubeIDE\stm32cubeide.exe`

**Purpose**

- C/C++ development.
- Project compilation and building.
- Debugging.
- Firmware development and target-board workflows.

**Pre-execution requirements**

1. Verify that the installation directory and executable exist.
2. Inspect the repository to determine whether the project is an STM32CubeIDE project or uses another build system.
3. Identify relevant project files and build configuration, such as `.project`, `.cproject`, Makefiles, CMake files, linker scripts, and startup files.
4. Determine whether the required compiler, build tools, device support, and dependencies are available.
5. Confirm the actual build command and output location before building.

**Execution constraints**

- Building, debugging, downloading firmware, programming the target, and other hardware-affecting operations must follow the configured permissions and required approval process.
- Do not assume that installing STM32CubeIDE guarantees that ARM GCC, `make`, CMake, debugger support, or other tools are correctly configured.
- Do not change project configuration or regenerate source files merely to make a build succeed without the required authorization.

### 3.4 STM32 Verification Requirements

Record Host and Target verification separately.

**Host unit testing**

- Identify the host compiler and test framework.
- Confirm whether the driver can be compiled and tested without MCU-specific hardware dependencies.
- Record the exact commands, exit status, test results, and relevant logs.
- Clearly identify mocked hardware registers, stubs, or simulated peripherals.

**Target hardware testing**

- Confirm the target board and MCU identity.
- Confirm that the required debugger, programming tools, hardware connections, and firmware configuration are available.
- Obtain the required authorization before programming or performing hardware-affecting operations.
- Record the actual test procedure, hardware configuration, observed behavior, and logs where available.

A Host unit test does not establish that the firmware works on the target MCU. A successful compilation does not establish that the driver operates correctly on physical hardware.

## 4. AMD Xilinx Vivado Toolchain

### 4.1 Installation

- Vendor: AMD Xilinx
- Version: Vivado 2024.2
- Operating system: Windows

**Batch executable**

`C:\Users\user\Desktop\local\vivado\2024.2\bin\vivado.bat`

### 4.2 Purpose

Vivado may be used for:

- FPGA RTL synthesis.
- FPGA implementation and timing analysis.
- FPGA project and block design management.
- IP integration and dependency inspection.
- Simulation integration, when the required simulator is installed and configured.
- Bitstream generation, when explicitly authorized.

The installed Vivado version and available features must be confirmed from the actual environment before execution.

### 4.3 Applicable Agents

| Agent | Responsibilities |
|---|---|
| Planner | Evaluates FPGA architecture, constraints, clocking, dependencies, and verification strategy. |
| Coder | Implements RTL and related FPGA source files within the assigned scope. |
| Reviewer | Independently reviews RTL, constraints, clock/reset design, interfaces, and implementation changes. |
| Verification Engineer | Executes authorized RTL simulation and FPGA verification and records evidence. |
| Integration Engineer | Checks build configuration, IP dependencies, timing reports, and hardware/software interfaces. |
| Researcher | Researches official documentation, tool behavior, IP requirements, and technical constraints. |
| Orchestrator | Coordinates the workflow, manages approvals and dependencies, and consolidates evidence-based results. |

Agent responsibilities do not override individual Agent permissions or user approval requirements.

### 4.4 Pre-execution Checks

Before using Vivado:

1. Verify that the configured batch executable exists.
2. Confirm the actual Vivado version.
3. Identify the project root and inspect the repository.
4. Determine whether the project uses an existing `.xpr` project, Tcl-based flow, or another supported workflow.
5. Inspect relevant files, including `.xpr`, `.tcl`, `.xdc`, RTL source files, IP configuration, and block design files where applicable.
6. Identify required simulators, IP cores, board files, licenses, device support, and external dependencies.
7. Determine whether the intended command is read-only, generates files, modifies the project, or affects hardware.

Do not assume that the presence of Vivado guarantees that the simulator, IP licenses, board files, or required IP cores are available.

### 4.5 Execution Constraints

- Do not overwrite an existing Vivado project without the required authorization.
- Do not regenerate IP cores or block designs without the required authorization.
- Do not modify pin constraints, timing constraints, clock definitions, reset configuration, or target-device settings without authorization.
- Do not generate a bitstream or program FPGA hardware unless explicitly authorized.
- Prefer inspecting existing project files and logs before initiating operations that generate or modify files.
- Review generated changes and preserve unrelated user modifications.

### 4.6 FPGA Verification Requirements

Report each verification activity separately.

**RTL simulation**

Record the simulator and version, testbench, execution command, logs, assertions, and pass/fail criteria. If no simulator is available, report `BLOCKED` or `NOT RUN` as appropriate.

**Synthesis**

Record the target FPGA device, source set, constraints, tool version, execution status, relevant warnings, and resource reports.

**Implementation and timing analysis**

Record implementation status, applicable timing constraints, timing reports, critical paths, and unresolved timing violations.

**Bitstream generation**

Record whether generation was authorized and executed, the target device, tool output, and generated artifact location.

**Hardware verification**

Record the physical device, programming procedure, test conditions, observed behavior, and available logs. Do not claim hardware validation based solely on simulation or implementation reports.

### 4.7 Execution Evidence

For each executed Vivado task, record where available:

- Exact command and working directory.
- Tool version.
- Exit status.
- Relevant log and report paths.
- Generated or modified files.
- Warnings and errors.
- Verification activity and acceptance criteria.
- Remaining risks and unverified items.

Use the common status definitions in Section 2.3.

## 5. Cross-Toolchain Rules

When a task spans firmware, FPGA, drivers, or PC software:

1. Identify the interface contracts and dependencies across components before implementation.
2. Verify that each toolchain targets the intended device and project.
3. Keep configuration changes within the approved scope.
4. Distinguish source-level review, Host testing, RTL simulation, target hardware testing, synthesis, implementation, and timing analysis.
5. Do not infer end-to-end correctness from a successful result in only one layer.
6. Require independent review and appropriate verification evidence before declaring the task complete.
7. Preserve logs, reports, and other reproducible evidence needed for handoff.

## 6. Standard Tool Execution Report

For each tool operation, report the following fields:

| Field | Required information |
|---|---|
| Task | Objective of the operation. |
| Tool | Tool name and version, if available. |
| Project | Actual project root and target project. |
| Command | Exact command or documented execution procedure. |
| Authorization | Whether approval was required and obtained. |
| Execution | Whether the operation was actually executed. |
| Exit status | Actual process exit status, when available. |
| Evidence | Relevant output, logs, reports, and artifact paths. |
| Result | PASS, FAIL, BLOCKED, NOT RUN, INCONCLUSIVE, or NOT APPLICABLE. |
| Limitations | Missing dependencies, incomplete coverage, or unverified claims. |

Do not fabricate command output, exit codes, logs, reports, generated files, test results, hardware observations, or CI results.

## 7. Maintenance

- Update this document when the development environment or tool versions change.
- Treat executable paths as local configuration, not portable project requirements.
- When working on another machine, rediscover the tool locations and verify the environment instead of reusing these paths blindly.
- Keep toolchain documentation separate from task-specific acceptance criteria and implementation plans.
- Any change to an existing project configuration remains subject to the applicable authorization and change-control rules.
