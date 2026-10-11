---
description: Plan and perform evidence-based engineering verification
agent: verification-engineer
---

Verify the following implementation or requirement:

$ARGUMENTS

First inspect the relevant requirements, implementation, existing tests, and available toolchain.

Determine the appropriate verification level:

- Static inspection or lint
- Unit or component tests
- Software build and automated tests
- RTL simulation and testbench execution
- Gate-level simulation
- Synthesis checks
- Timing analysis
- Emulation or hardware testing

Only select verification methods relevant to the task.

Workflow:

1. Identify the requirements and expected observable behavior.
2. Define pass/fail criteria before interpreting results.
3. Inspect existing tests and identify coverage gaps.
4. Select the smallest sufficient verification sequence.
5a. Before executing a command that requires approval, obtain approval through the configured permission mechanism. If approval is denied or unavailable, mark the check as Blocked or Not Run, as appropriate.
5b. Do not delegate tasks to other agents.
5c. Do not modify production implementation code to make verification pass. Report suspected implementation defects to the Orchestrator and recommend a separate corrective task.
5d. If creating or modifying testbenches, test code, scripts, or verification configuration requires approval, obtain that approval before making the change.
6. Inspect actual outputs, logs, exit status, and failure details.
7. Report failures without hiding them or weakening acceptance criteria to obtain a pass.

Report:

- Requirement being verified
- Verification method and test conditions
- Commands or procedures actually executed
- Expected versus observed results
- Pass, Fail, Blocked, or Not Run status for each check
- Relevant logs or evidence
- Uncovered cases and remaining risks

Do not claim that a test passed if it was only proposed, inspected, or reasoned about.

If execution is unavailable or not authorized, provide a concrete verification procedure and clearly mark it as Not Run.