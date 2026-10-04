先閱讀與記憶並不動作，直到我讓你開始行動

# GLOBAL_RULE.md

# 1. Purpose

This document defines the global rules that apply to every role and every development stage of the Python project.

These rules have higher priority than role-specific instructions.

All agents must follow this document unless an explicit project-level decision overrides a rule.

---

# 2. Role Separation

The project uses role-based development.

The primary roles are:

* Python Manager
* Python Coding Engineer
* Python Test Engineer
* Python Reviewer

Each role has clearly defined responsibilities.

An agent must not silently perform responsibilities belonging to another role when doing so would change project architecture, specification, or verification requirements.

---

# 3. Source of Truth

The following priority order shall be used when resolving conflicts:

1. GLOBAL_RULE.md
2. SPECIFICATION.md
3. ARCHITECTURE.md
4. Role-specific instructions
5. Implementation details
6. Temporary suggestions or assumptions

Lower-level documents must not silently contradict higher-level documents.

If a conflict is discovered, the agent must report the conflict instead of silently choosing one interpretation.

---

# 4. No Silent Requirement Changes

Agents shall not silently:

* change requirements
* remove required functionality
* change public interfaces
* change data formats
* change API behavior
* change architecture
* weaken validation
* modify expected test behavior

If a requirement appears incorrect, incomplete, or impractical, report the issue to the Manager.

Do not solve requirement problems by silently modifying the requirement.

---

# 5. Architecture Protection

The architecture must be treated as a controlled artifact.

Coding Engineers may implement the approved architecture but shall not independently redefine the architecture.

If implementation reveals an architectural problem:

1. identify the problem
2. explain the impact
3. report it to the Manager
4. wait for an architectural decision

Do not bypass the architecture by introducing undocumented mechanisms.

---

# 6. Evidence-Based Development

Agents shall distinguish clearly between:

* VERIFIED
* IMPLEMENTED
* TESTED
* NOT VERIFIED
* ASSUMED
* UNKNOWN

Never claim that something works merely because the code appears logically correct.

Implementation is not equivalent to verification.

---

# 7. Change Control

Every significant change should have a clear reason.

A significant change includes:

* public API modification
* architecture modification
* dependency change
* data model change
* behavior change
* test expectation change
* configuration change

Changes must be traceable to a requirement, bug, review finding, or approved engineering decision.

---

# 8. Python Engineering Principles

The project should prioritize:

* readability
* maintainability
* testability
* deterministic behavior where practical
* explicit interfaces
* clear error handling
* type safety where practical
* modular design
* minimal coupling
* minimal global state
* reproducible execution

Avoid unnecessary complexity.

Do not introduce abstractions merely for the sake of abstraction.

---

# 9. Dependency Control

External dependencies must not be introduced casually.

Before adding a dependency, evaluate:

* necessity
* maintenance status
* compatibility
* security implications
* project complexity
* whether the standard library is sufficient

The dependency should be documented when introduced.

---

# 10. Error Handling

Errors must not be silently ignored.

Do not use broad exception handling merely to prevent crashes.

Avoid patterns such as:

```python
try:
    ...
except Exception:
    pass
```

unless there is a documented reason.

Errors should provide sufficient information for debugging.

---

# 11. Testing

New functionality should have corresponding tests when practical.

Bug fixes should preferably include regression tests.

Tests must validate behavior rather than implementation details whenever possible.

Do not modify tests merely to make incorrect implementation pass.

---

# 12. Code Review

Code should be reviewable by another engineer.

Important decisions should be documented.

Complex logic should include appropriate comments explaining WHY rather than merely repeating WHAT the code does.

---

# 13. Minimal Change Principle

When fixing a bug or implementing a requirement:

* change only what is necessary
* avoid unrelated refactoring
* avoid unnecessary formatting changes
* preserve existing behavior unless a change is required

This reduces regression risk.

---

# 14. Communication Rules

When reporting work, agents should clearly state:

* what was changed
* why it was changed
* affected files
* verification performed
* remaining risks
* unresolved issues

Do not report unverified assumptions as facts.

---

# 15. Stop Conditions

An agent should stop and escalate when:

* requirements conflict
* architecture is insufficient
* required information is missing
* implementation would require changing a higher-level artifact
* expected behavior is ambiguous
* a potentially breaking change is required

The agent should report the problem and proposed options rather than making an undocumented decision.

---

# 16. Definition of Done

A task is not considered complete merely because code has been written.

A task should be considered complete only when applicable:

* implementation is complete
* interfaces are consistent
* tests are added or updated
* relevant tests pass
* behavior has been verified
* documentation is updated when necessary
* known limitations are reported

---

# 17. General Principle

The project follows the principle:

Requirements define WHAT.
Architecture defines HOW at the system level.
Implementation defines HOW at the code level.
Tests provide EVIDENCE.

No role should silently override another level.
