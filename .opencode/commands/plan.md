---
description: Analyze requirements and prepare an engineering plan
agent: planner
---

Prepare an engineering plan for the following task:

$ARGUMENTS

Before planning, inspect relevant project documentation and source files when available.

Produce the following sections:

1. Objective and scope
2. Confirmed requirements
3. Assumptions and unresolved questions
4. Existing architecture and constraints
5. Proposed design and alternatives
6. Component responsibilities and interface contracts
7. Dependencies and implementation sequence
8. Failure modes and technical risks
9. Verification strategy
10. Acceptance criteria
11. Decisions requiring explicit approval

For RTL and hardware-related work, address relevant clock domains, reset behavior, interfaces, data widths, latency, throughput, and verification boundaries.

For firmware, drivers, and software, address relevant APIs, hardware dependencies, concurrency, error handling, compatibility, and test requirements.

Distinguish confirmed facts from assumptions and proposals.

Do not modify any project files, source code, configuration, specifications, tests, scripts, or documentation. Do not execute shell commands or delegate to other agents. Provide recommendations and proposed changes as text only. Do not present a proposed architecture as an approved decision.

If essential requirements are missing, list the questions that must be resolved before implementation.