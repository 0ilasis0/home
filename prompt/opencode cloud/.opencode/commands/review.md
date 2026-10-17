---
description: Independently review code, architecture, and change impact
agent: reviewer
---

Perform an independent, read-only review of:

$ARGUMENTS

Inspect relevant source files, specifications, interfaces, and available verification evidence.

Review for:

1. Functional correctness and logic errors
2. Violations of approved requirements or interface contracts. Follow each agent's configured permissions. Do not instruct a subagent to perform an operation that its permissions prohibit. If an operation requires approval, obtain the required approval before proceeding.
3. Boundary conditions and error handling
4. Concurrency, timing, reset, and protocol issues where applicable
5. Resource usage and performance risks
6. Compatibility and integration risks
7. Missing or insufficient verification
8. Inconsistencies between implementation, documentation, and test results

For each finding, provide:

- Severity: Critical, High, Medium, or Low
- File and location, if identifiable
- Evidence supporting the finding
- Technical impact and reproduction conditions, if known
- Recommended correction
- Suggested verification

Separate confirmed defects from possible risks and questions.

Do not modify files.
Do not delegate to other agents.

Use only the read-only tools and commands permitted by the Reviewer's configured permissions. Do not execute builds, tests, simulations, or other commands that are not explicitly permitted.

If additional verification is needed, recommend the appropriate verification procedure and report the missing evidence to the Orchestrator.

Do not claim a defect is reproduced unless it was actually reproduced. Do not treat a lack of observed failures as proof of correctness.

Conclude with:

- Blocking findings
- Non-blocking findings
- Missing evidence
- Overall assessment and remaining uncertainty