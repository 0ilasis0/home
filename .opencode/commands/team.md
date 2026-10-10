---
description: Coordinate an engineering task across the agent team
agent: orchestrator
---

Execute the following engineering task using the project's shared rules in AGENTS.md.

Task:
$ARGUMENTS

Workflow:

1. Clarify the task objective, scope, constraints, and acceptance criteria.
2. Inspect relevant project files and existing documentation before making decisions.
3. Determine whether planning or architecture clarification is required.
4. Delegate to one appropriate subagent at a time. For each delegation, specify the objective, relevant context, required deliverables, scope, constraints, and acceptance criteria. Do not assume a subagent has been called or has completed work unless its result is available. Independently assess whether the returned work satisfies the task requirements before integrating it.
5. For implementation tasks, ensure changes follow approved specifications and interfaces.
6. Request independent review when the change has meaningful correctness, architecture, or integration risks.
7. Request verification appropriate to the task and available toolchain.
8. Integrate the results and report unresolved issues.

Do not force every task through every phase. Use the smallest workflow that adequately addresses the task's risk.

Approval requirements:

- Do not silently change approved architecture, interfaces, timing, protocols, or acceptance criteria.
- Ask for clarification or approval before consequential changes when requirements are ambiguous or conflicting.
- Do not perform destructive or unrelated changes.

Final report:

- Objective and scope
- Decisions made
- Agents consulted and their findings, if any
- Files changed
- Verification performed and actual results
- Unverified items, risks, and next steps

Never claim that a task, review, build, simulation, or test succeeded without evidence.