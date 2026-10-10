---
description: Assess engineering deliverables before release or handoff
agent: orchestrator
---

Perform a release-readiness assessment for:

$ARGUMENTS

Review the available project requirements, approved architecture decisions, implementation changes, review findings, and verification evidence.

Check the following:

1. Requirements and acceptance criteria
2. Implementation completeness
3. Interface and architecture consistency
4. Build, test, simulation, and analysis evidence
5. Review findings and unresolved defects
6. Known limitations and outstanding work
7. Documentation and reproducibility
8. Compatibility, dependencies, and configuration
9. Risk of regressions or integration failures

Classify each item as:

- Pass: supported by sufficient evidence
- Fail: a known requirement or acceptance criterion is not satisfied
- Blocked: required information, tools, or dependencies are unavailable
- Not Verified: insufficient evidence to determine the result
- Not Applicable: demonstrably irrelevant to this deliverable

Rules:

- Do not infer that a check passed merely because no failure was reported.
- Do not treat proposed tests as executed tests.
- Do not close unresolved review findings without evidence.
- Do not modify code, create a release, publish artifacts, or deploy anything as part of this assessment unless explicitly authorized.
- Distinguish release blockers from non-blocking risks.
- Treat this command as an assessment-only workflow. Inspect existing files, change summaries, reports, logs, and verification evidence using permitted operations. Do not execute builds, tests, simulations, packaging, deployment, or release commands unless explicitly authorized. When additional execution is required, identify the required command and obtain authorization before running it.

Final report:

1. Readiness assessment
2. Blocking issues
3. Failed or incomplete acceptance criteria
4. Verification evidence reviewed
5. Remaining risks and limitations
6. Recommended actions before handoff

A readiness assessment is not a guarantee of correctness or a substitute for required hardware validation.