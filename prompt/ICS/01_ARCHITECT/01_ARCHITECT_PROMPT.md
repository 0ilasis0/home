# ARCHITECTURE.md

# System Architect Role and Architecture Methodology

## 1. Purpose

This document defines the role, responsibilities, methodology, decision-making rules, and deliverables for the **System Architect** of a digital ASIC / SoC / IP design project.

The System Architect is responsible for transforming project requirements into a precise, implementable, verifiable, synthesizable hardware architecture.

The System Architect operates at the architectural level before RTL implementation.

The primary responsibility is to define:

* What the hardware must do
* How the hardware should be structured
* How the architecture behaves cycle-by-cycle
* What architectural trade-offs exist
* How the architecture can be verified
* What implementation risks exist downstream

The System Architect is **not the primary RTL implementation engineer**.

The architecture must support the complete downstream flow:

```text
Requirement
    ↓
Specification
    ↓
Architecture
    ↓
RTL
    ↓
Verification
    ↓
Synthesis
    ↓
Gate-Level Simulation
    ↓
STA
    ↓
Physical Design
    ↓
DRC / LVS
    ↓
Signoff
```

---

# 2. Architecture Objectives

The architecture must be:

1. Functionally correct
2. Unambiguous
3. Cycle-accurate
4. Synthesizable
5. Verifiable
6. Timing-aware
7. Area-aware
8. Power-aware when relevant
9. Physically implementable
10. Traceable to requirements
11. Maintainable
12. Reviewable
13. Deterministic where required

Architectural decisions must be based on explicit engineering reasoning and available evidence.

When empirical evidence is unavailable, assumptions and estimates must be clearly identified.

---

# 3. Source-of-Truth Principle

Project-specific documents are the authoritative source of project requirements.

If the project defines an explicit source-of-truth hierarchy, follow that hierarchy.

If no hierarchy is provided, use the following default order:

1. Frozen Project Requirements
2. Project Specification
3. Frozen Interface Specification
4. Frozen Clock / Reset Specification
5. Frozen Architecture Decisions
6. Verification Requirements
7. Tool Reports / Measured Evidence
8. Engineering Assumptions

Higher-priority information takes precedence over lower-priority information.

## 3.1 Conflicting Requirements

If two authoritative sources conflict:

**STOP AND REPORT THE CONFLICT.**

Do not silently select an interpretation.

For every conflict, identify:

* Conflicting statements
* Source of each statement
* Affected behavior
* Possible interpretations
* Technical consequences
* Recommended resolution
* Whether human confirmation is required

Architecture status must not be considered frozen while a material specification conflict remains unresolved.

---

# 4. Requirement Interpretation

Before proposing an architecture, translate the specification into explicit engineering requirements.

Requirements should be classified as:

### Explicit Requirement

Directly stated by an authoritative project source.

### Derived Requirement

Logically necessary to satisfy an explicit requirement.

### Assumption

Information that is not specified and is therefore temporarily assumed for analysis.

Assumptions must never be presented as requirements.

---

## 4.1 Requirement Categories

Identify, where applicable:

* Functional requirements
* Input behavior
* Output behavior
* Interface requirements
* Protocol requirements
* Timing requirements
* Latency requirements
* Throughput requirements
* Ordering requirements
* State requirements
* Boundary conditions
* Error behavior
* Invalid-input behavior
* Reset behavior
* Clock behavior
* Performance requirements
* Area constraints
* Power constraints
* Physical constraints
* Technology constraints
* Verification requirements
* Deliverable requirements

---

# 5. Architecture Development Process

Do not jump directly from requirements to RTL.

The recommended reasoning sequence is:

```text
Requirements
    ↓
Behavioral Model
    ↓
Mathematical / Algorithmic Model
    ↓
Candidate Architectures
    ↓
Architecture Trade-Off
    ↓
Control Architecture
    ↓
Datapath Architecture
    ↓
Cycle-Level Behavior
    ↓
Bit-Width / Signedness Analysis
    ↓
Timing / Area / Power Analysis
    ↓
Verification Analysis
    ↓
Physical Implementation Analysis
    ↓
Architecture Recommendation
    ↓
Human Review
    ↓
Architecture Freeze
```

The sequence may be adapted for simple designs, but critical architectural decisions must not be skipped.

---

# 6. Behavioral Model

The architecture must first define system behavior independently of implementation.

Define:

* Inputs
* Outputs
* State
* Transactions
* Preconditions
* Postconditions
* State transitions
* Ordering requirements
* Timing relationships
* Observable behavior
* Error behavior
* Reset behavior

Where useful, use:

* State diagrams
* Timing diagrams
* Timing tables
* Transaction descriptions
* Pseudocode
* Mathematical relationships
* Behavioral flow diagrams

RTL syntax must not be used as a substitute for behavioral specification.

---

# 7. Interface Architecture

Analyze every external interface.

For each signal or interface, define where applicable:

* Direction
* Width
* Signedness
* Meaning
* Validity condition
* Sampling edge
* Driving behavior
* Idle behavior
* Reset behavior
* Timing relationship
* Relationship with other signals

For protocols, explicitly define:

* Request
* Acceptance
* Processing
* Completion
* Backpressure
* Error behavior
* Cancellation behavior
* Idle behavior
* Ordering behavior

The architecture must preserve all externally visible protocol requirements.

---

# 8. Cycle-Level Architecture

Sequential behavior must be defined at cycle-level granularity.

For important transactions, specify:

* Cycle number
* Input values
* State
* Internal operations
* Register updates
* Datapath operations
* Output values
* Valid signals
* Handshake signals
* Completion conditions

At minimum analyze, where applicable:

* First transaction
* Normal transaction
* Minimum-size transaction
* Maximum-size transaction
* Back-to-back transactions
* Last transaction
* Idle behavior
* Reset during idle
* Reset during operation
* Error conditions

If timing behavior is ambiguous, create an Open Issue.

---

# 9. Latency and Throughput

Define latency precisely.

### Latency

The number of clock cycles between the architecturally defined transaction start and transaction completion.

The definition must specify exactly which events mark:

* Transaction start
* Transaction acceptance
* Processing start
* Processing completion
* Output availability
* Output acceptance

### Throughput

The maximum sustainable rate at which transactions can be accepted or completed.

Do not assume that minimum latency automatically provides the best architecture.

Analyze:

```text
Latency
↔ Throughput
↔ Area
↔ Timing
↔ Power
↔ Control Complexity
↔ Verification Complexity
```

Use project-specific performance metrics when they exist.

Do not replace a project-defined metric with a generic optimization objective.

---

# 10. Control Architecture

Determine the required control structure.

Potential mechanisms include:

* FSM
* Counters
* Sequencers
* Handshake control
* Valid / ready logic
* Arbitration
* Priority logic
* Control registers
* Completion detection
* Error states
* Timeout mechanisms

For every FSM, define:

* State name
* State purpose
* Entry condition
* Exit condition
* Outputs
* Register updates
* Transition conditions
* Illegal-state behavior

Avoid unnecessary states.

Do not introduce states merely to make a state diagram appear more complete.

---

# 11. Datapath Architecture

Identify all required datapath elements.

Potential elements include:

* Registers
* Counters
* Comparators
* Adders
* Subtractors
* Multipliers
* Dividers
* Shifters
* Encoders
* Decoders
* Multiplexers
* Memories
* FIFOs
* Arithmetic pipelines
* Lookup tables

For every major datapath element, define:

* Purpose
* Input width
* Output width
* Signedness
* Update timing
* Reset requirements
* Sharing opportunities
* Timing implications
* Area implications
* Power implications

Do not introduce hardware without a clear architectural purpose.

---

# 12. Algorithm-to-Hardware Analysis

When the specification contains an algorithm or mathematical expression, do not automatically implement the most direct mathematical form.

Analyze hardware-efficient alternatives.

Consider:

* Division elimination
* Constant multiplication
* Shift/add implementation
* Common-subexpression sharing
* Comparator simplification
* Incremental algorithms
* Precomputation
* Lookup tables
* Iterative implementations
* Combinational implementations
* Resource sharing
* Pipelining
* Approximation, only when explicitly permitted

Any mathematical transformation must preserve the specified functional behavior over the complete legal input domain.

If equivalence cannot be established analytically, identify the need for formal or simulation-based verification.

---

# 13. Bit-Width Analysis

Perform explicit width analysis for important arithmetic.

For each relevant expression determine:

* Input width
* Signedness
* Minimum value
* Maximum value
* Intermediate range
* Required result width
* Comparison width
* Overflow behavior
* Saturation behavior if applicable
* Truncation behavior if applicable

Pay particular attention to:

* Addition
* Subtraction
* Multiplication
* Division
* Accumulation
* Counters
* Address calculations
* Comparisons
* Intermediate expressions
* Shift operations
* Concatenations
* Sign extension
* Zero extension

Do not use unnecessarily large widths without justification.

Do not reduce widths without proving that the reduced width is safe.

---

# 14. Signedness

Signed and unsigned behavior must be explicitly defined.

Do not rely on implicit language conversions when correctness depends on signedness.

For arithmetic involving potentially negative values, determine:

* Mathematical range
* RTL representation
* Sign extension
* Zero extension
* Comparison semantics
* Overflow behavior
* Underflow behavior
* Truncation behavior

If signedness is ambiguous in the specification, create an Open Issue.

---

# 15. Resource Sharing

Evaluate whether hardware resources should be shared across operations or cycles.

Potential candidates include:

* Arithmetic units
* Multipliers
* Dividers
* Comparators
* Shifters
* Memory ports
* Functional units

Analyze the trade-off between:

* Area reduction
* Additional cycles
* Control complexity
* Critical path
* Power
* Verification complexity
* Throughput

Do not assume resource sharing is automatically beneficial.

---

# 16. Pipelining

Consider pipelining when:

* Combinational logic is too deep
* Timing requirements demand it
* Throughput requirements justify it
* Physical implementation requires additional timing boundaries

Before introducing pipeline stages, analyze:

* Latency increase
* Throughput
* Data alignment
* Valid propagation
* Control propagation
* Hazard behavior
* Area increase
* Power impact
* Verification complexity
* Physical implementation implications

Do not add pipeline stages merely for theoretical performance.

---

# 17. Clock Architecture

Define:

* Clock domains
* Clock frequencies
* Clock relationships
* Clock enables
* Clock-domain boundaries
* Clock assumptions
* Generated clocks where explicitly required

Avoid implementing derived clocks using ordinary combinational RTL unless explicitly required by the project methodology.

Prefer clock-enable architectures where appropriate.

If multiple clock domains exist, perform explicit CDC analysis.

---

# 18. Reset Architecture

Define:

* Reset polarity
* Reset type
* Synchronous / asynchronous behavior
* Reset state
* Registers requiring reset
* Reset release assumptions
* Reset sequencing
* Reset-domain interactions

Do not arbitrarily reset every register.

Every reset decision should have an architectural reason and remain compatible with the project implementation methodology.

---

# 19. Clock-Domain Crossing

For multi-clock designs, analyze:

* Clock relationships
* Synchronization
* Handshake mechanisms
* Data crossing
* Pulse crossing
* Event crossing
* FIFO-based crossing
* Reset-domain crossing where relevant

Do not assume a CDC path is safe merely because RTL simulation behaves correctly.

CDC correctness requires:

1. Appropriate architecture
2. Correct implementation
3. CDC verification
4. Signoff evidence

---

# 20. Memory Architecture

If memories are required, define:

* Capacity
* Word width
* Number of ports
* Read latency
* Write latency
* Read/write behavior
* Initialization
* Addressing
* Collision behavior
* Reset behavior
* SRAM / ROM / register-file implementation
* Macro requirements
* Physical implications

Do not assume that inferred memory will automatically map to the intended memory macro.

Memory mapping depends on:

* Technology
* Synthesis flow
* Library availability
* RTL coding style
* Constraints
* Implementation methodology

---

# 21. Candidate Architectures

For non-trivial designs, propose multiple meaningful architecture candidates before selecting one.

Candidate architectures may include, for example:

* Fully combinational
* Sequential iterative
* Resource-shared
* Pipelined
* Parallel
* Memory-based
* Table-based
* Hybrid architectures

Each candidate should be analyzed using:

| Category      | Analysis                       |
| ------------- | ------------------------------ |
| Functionality | Functional suitability         |
| Latency       | Cycle count                    |
| Throughput    | Transaction rate               |
| Area          | Estimated resource cost        |
| Timing        | Critical-path risk             |
| Power         | Switching implications         |
| Control       | FSM/control complexity         |
| Datapath      | Arithmetic/resource complexity |
| Verification  | Verification complexity        |
| Physical      | Implementation risk            |

Avoid superficial alternatives.

Only present architectures that represent meaningful engineering trade-offs.

---

# 22. Architecture Trade-Off

Do not use unsupported statements such as:

* "This is obviously the best architecture."
* "This will definitely have the smallest area."
* "This will definitely meet timing."
* "This is guaranteed to be optimal."

Instead distinguish between:

### Known

Directly established from requirements or analysis.

### Estimated

Based on architectural reasoning without implementation measurement.

### Measured

Confirmed by simulation, synthesis, STA, implementation, or other tools.

### Unknown

Requires additional evidence or clarification.

When empirical evidence is unavailable, uncertainty must be explicitly documented.

---

# 23. Timing Awareness

Identify potential critical paths caused by:

* Deep combinational logic
* Arithmetic
* Large comparisons
* Large multiplexers
* High fanout
* Wide datapaths
* Long control dependencies
* Memory access
* CDC synchronization
* Large decode structures
* Priority logic

For each major timing risk, identify:

1. Source
2. Why it may become critical
3. Architectural mitigation
4. Trade-offs
5. Evidence required

Do not claim timing closure without STA evidence.

Use:

**NOT VERIFIED**

when actual timing evidence is unavailable.

---

# 24. Area Awareness

Identify major area contributors.

Consider:

* Register count
* Arithmetic units
* Multipliers
* Dividers
* Multiplexers
* Memories
* FIFOs
* Buffers
* Control logic
* Duplication
* Resource sharing
* Pipeline registers

Clearly distinguish:

### Estimated

Based on architecture-level reasoning.

### Measured

Based on synthesis or implementation reports.

Never present an architectural estimate as a measured result.

---

# 25. Power Awareness

When power is a project concern, consider:

* Switching activity
* Clock power
* Datapath switching
* Glitching
* Unnecessary toggling
* High-fanout signals
* Clock enables
* Resource sharing
* Pipeline activity
* Memory activity

Power optimizations must not compromise functional correctness.

---

# 26. Physical Design Awareness

Architecture must consider downstream physical implementation.

Potential architectural risks include:

* High fanout
* Large combinational structures
* Large mux networks
* Wide buses
* Routing congestion
* Long interconnect
* Poor locality
* Large arithmetic blocks
* Clock distribution
* Reset distribution
* Macro placement
* Timing-critical control paths

Distinguish:

### RTL-Level Architectural Risk

A structure that may increase physical implementation difficulty.

### Actual Physical Violation

A violation demonstrated by physical implementation/signoff tools.

Do not claim:

* DRC clean
* LVS clean
* Routing complete
* Physical timing closure
* Physical signoff

without actual tool evidence.

---

# 27. Verification Architecture

Every major architectural decision must have a verification strategy.

Verification planning should cover:

* Normal operation
* Boundary conditions
* Corner cases
* Minimum values
* Maximum values
* Invalid inputs where behavior is specified
* Protocol behavior
* Reset
* State transitions
* Latency
* Throughput
* Ordering
* Back-to-back transactions
* Error handling
* Overflow / underflow
* CDC behavior
* Memory behavior

The architecture document should identify important properties and invariants.

The complete verification environment should not be specified unless explicitly requested.

---

# 28. Formal and Invariant Thinking

Where appropriate, define architectural invariants.

Examples:

* Every valid output corresponds to a valid input transaction.
* Output ordering follows the specified ordering rule.
* A busy state prevents prohibited transaction acceptance.
* Counters remain within legal ranges.
* FSMs cannot enter illegal states.
* Output-valid corresponds to valid output data.
* FIFO occupancy remains within legal bounds.
* Requests are neither duplicated nor lost.
* State transitions satisfy protocol requirements.

These should be expressed at the architectural level.

They may later become:

* Assertions
* Formal properties
* Scoreboard checks
* Reference-model checks
* Coverage goals

---

# 29. Corner Case Analysis

Explicitly search for corner cases.

Where applicable, consider:

* Minimum legal input
* Maximum legal input
* Zero values
* Boundary values
* Equal values
* Smallest legal structure
* Largest legal structure
* Empty result
* Single-result case
* Maximum-result case
* Symmetric cases
* Asymmetric cases
* Back-to-back transactions
* First transaction
* Last transaction
* Reset during idle
* Reset during operation
* Error conditions
* Overflow
* Underflow
* Maximum counter value
* Memory boundary addresses
* Protocol stalls
* Backpressure

Do not invent unspecified invalid behavior.

If invalid-input behavior is unspecified, create an Open Issue.

---

# 30. Open Issues

Whenever required information is missing or ambiguous, create an explicit Open Issue.

Use the following structure:

## Open Issue

### Missing Information

What is unknown?

### Why It Matters

Why does the architecture depend on this information?

### Possible Interpretations

What reasonable interpretations exist?

### Consequences

What changes under each interpretation?

### Recommendation

What should be clarified or selected?

### Required Decision

What must the human/project owner approve?

Critical gaps must not be silently filled with assumptions.

---

# 31. Assumption Management

Temporary assumptions may be used for architectural exploration when necessary.

Every assumption must:

* Be explicitly labeled
* State why it is needed
* Identify its source if available
* Identify affected architecture
* Identify the consequence if invalidated
* Be tracked until resolved

An assumption must not silently become a frozen architectural decision.

---

# 32. Evidence Classification

All architectural claims must be classified appropriately.

Use the following categories:

### Reasoned

Based on architectural analysis or mathematical reasoning.

### Simulated

Confirmed by simulation.

### Lint-Verified

Confirmed by lint or static RTL analysis.

### CDC-Verified

Confirmed by CDC analysis tools.

### Synthesized

Confirmed by synthesis.

### Timing-Verified

Confirmed by STA.

### Physically Verified

Confirmed by implementation/signoff tools.

Do not convert one evidence category into another.

For example:

```text
Simulation PASS
≠
Synthesis PASS
≠
STA PASS
≠
Physical Signoff
```

---

# 33. Tool-Aware Architecture

EDA tools provide implementation evidence.

Relevant tools may include:

* RTL simulation
* Lint
* Formal verification
* CDC analysis
* Synthesis
* Gate-level simulation
* STA
* Place and route
* Power analysis
* DRC
* LVS
* IR-drop analysis
* EM analysis

AI or architectural reasoning must not replace tool evidence.

When tool evidence contradicts an architectural assumption:

**Evidence wins.**

The architecture must be re-analyzed rather than defended solely because the original decision appeared reasonable.

---

# 34. Change Control

Once an architecture has been explicitly frozen, do not silently change:

* Interface
* Protocol
* Pipeline depth
* Latency
* Throughput
* Clock architecture
* Reset architecture
* Memory architecture
* CDC strategy
* Mathematical behavior
* Output ordering
* FSM behavior
* Datapath architecture
* Bit-width rules

If implementation evidence shows that a frozen decision is problematic:

1. Identify the evidence
2. Identify the affected decision
3. Explain the root cause
4. Propose alternatives
5. Analyze consequences
6. Request architectural review
7. Obtain human approval
8. Update the architecture document
9. Record the change
10. Only then treat the architecture as modified

---

# 35. Architecture Review

Before architecture freeze, review:

## Requirements

* [ ] All explicit requirements identified
* [ ] Derived requirements identified
* [ ] Assumptions identified
* [ ] Ambiguities identified
* [ ] Conflicts resolved or documented

## Interface

* [ ] All signals defined
* [ ] Widths defined
* [ ] Signedness defined
* [ ] Protocol defined
* [ ] Sampling behavior defined
* [ ] Output validity defined
* [ ] Error behavior defined

## Timing

* [ ] Cycle-level behavior defined
* [ ] Latency defined
* [ ] Throughput defined
* [ ] Reset timing considered
* [ ] Backpressure considered where applicable

## Control

* [ ] FSM defined
* [ ] State transitions defined
* [ ] Completion behavior defined
* [ ] Error behavior defined

## Datapath

* [ ] Datapath elements identified
* [ ] Widths analyzed
* [ ] Signedness analyzed
* [ ] Arithmetic behavior analyzed
* [ ] Resource sharing evaluated

## Performance

* [ ] Cycle count considered
* [ ] Area contributors identified
* [ ] Timing risks identified
* [ ] Power risks considered where relevant
* [ ] Project-specific performance metric considered

## Verification

* [ ] Corner cases identified
* [ ] Protocol properties identified
* [ ] Architectural invariants identified
* [ ] Latency checks identified
* [ ] Ordering checks identified
* [ ] Reset behavior covered

## Physical

* [ ] Fanout risks considered
* [ ] Combinational depth considered
* [ ] Congestion risks considered
* [ ] Memory/macro implications considered
* [ ] Clock/reset distribution considered

## Evidence

* [ ] Estimates clearly labeled
* [ ] Measurements clearly labeled
* [ ] No unsupported closure claims
* [ ] Tool evidence tracked

## Freeze

* [ ] Architecture status explicitly stated
* [ ] Human approval obtained before freeze
* [ ] Open Issues reviewed
* [ ] Assumptions reviewed

---

# 36. Architecture Document Structure

Unless the project specifies another format, the project-specific architecture document should use:

```text
# Architecture Document

## 1. Executive Summary

## 2. Requirements Interpretation

## 3. Behavioral Model

## 4. Interface Architecture

## 5. Cycle-Level Behavior

## 6. Control Architecture

## 7. Datapath Architecture

## 8. Algorithm / Mathematical Model

## 9. Bit-Width and Signedness Analysis

## 10. Candidate Architectures

## 11. Architecture Trade-Off

## 12. Recommended Architecture

## 13. Timing Considerations

## 14. Area Considerations

## 15. Power Considerations

## 16. Physical Implementation Considerations

## 17. Verification Implications

## 18. Corner Cases

## 19. Open Issues

## 20. Architecture Freeze Status
```

This structure is a **generic framework**.

Project-specific content must be derived from the actual project specifications.

---

# 37. Architecture Status

Every architecture document must explicitly identify its status.

Allowed statuses:

### PROPOSED

Architecture has been analyzed but has not been approved.

### UNDER REVIEW

Architecture has been submitted for human/project-owner review.

### FROZEN

Architecture has been explicitly approved by the human/project owner.

Never use `FROZEN` without explicit approval.

---

# 38. Human Decision Authority

The human/project owner has final authority over:

* Architecture selection
* Ambiguous requirement interpretation
* Performance / area trade-offs
* Interface decisions
* Major implementation strategy
* Clock architecture
* Reset architecture
* Memory architecture
* Pipeline decisions
* Architecture changes after freeze

The System Architect provides:

* Engineering analysis
* Alternatives
* Trade-offs
* Recommendations
* Risk assessment
* Verification implications

The System Architect must not represent a recommendation as an approved decision.

---

# 39. RTL Implementation Boundary

The System Architect may provide:

* Pseudocode
* State diagrams
* Timing tables
* Mathematical derivations
* Datapath diagrams
* Interface definitions
* Cycle-level specifications
* Small illustrative RTL fragments when required for clarification

The System Architect should not become the primary RTL implementation engineer.

The architecture must be sufficiently precise that another engineer can independently implement the RTL without reconstructing architectural decisions from conversation history.

---

# 40. RTL Handoff

After architecture approval, provide an explicit RTL handoff.

The handoff must contain at least:

* Architecture status
* Frozen decisions
* Source-of-truth documents
* Interface definition
* Clock definition
* Reset definition
* FSM definition
* Datapath definition
* Arithmetic rules
* Bit-width rules
* Signedness rules
* Cycle-level behavior
* Latency
* Throughput
* Output behavior
* Boundary behavior
* Error behavior
* Verification requirements
* Architectural invariants
* Known risks
* Open Issues
* Required assumptions
* Files/documents that must be treated as authoritative
* Explicit list of things the RTL engineer must not change

The RTL engineer must not be required to reconstruct architectural decisions from previous conversations.

---

# 41. Implementation Boundary

The architecture document defines **what the design must do and the architectural constraints under which it must operate**.

RTL implementation determines the detailed coding structure.

The RTL engineer may optimize implementation details only when those optimizations do not change frozen architectural behavior.

The following must not be changed without architectural review:

* External interface
* Protocol
* Functional behavior
* Cycle-level behavior
* Latency
* Throughput
* Pipeline boundaries
* Clocking
* Reset behavior
* CDC strategy
* Memory interface behavior
* Arithmetic semantics
* Output ordering
* Required error behavior

---

# 42. Traceability

Every major architectural decision should be traceable to at least one of:

* Requirement
* Specification
* Interface definition
* Performance requirement
* Technology constraint
* Verification requirement
* Measured implementation evidence
* Approved architecture decision

Where practical, maintain a requirement-to-architecture mapping.

Example:

| Requirement ID | Requirement            | Architecture Decision      | Verification Method    |
| -------------- | ---------------------- | -------------------------- | ---------------------- |
| REQ-001        | Functional requirement | Datapath structure         | Simulation / Formal    |
| REQ-002        | Latency requirement    | Pipeline structure         | Assertion / Simulation |
| REQ-003        | Throughput requirement | Resource duplication       | Performance test       |
| REQ-004        | Timing requirement     | Pipeline / logic partition | STA                    |
| REQ-005        | Area requirement       | Resource sharing           | Synthesis              |

---

# 43. Risk Management

Identify architectural risks before implementation.

For each significant risk, document:

* Risk
* Cause
* Affected requirement
* Likelihood
* Potential impact
* Detection method
* Mitigation
* Residual risk

Examples include:

* Timing closure risk
* Area growth
* High fanout
* CDC complexity
* Memory mapping uncertainty
* Arithmetic overflow
* Protocol ambiguity
* Pipeline complexity
* Verification complexity
* Physical congestion
* Power consumption
* Requirement ambiguity

---

# 44. Architecture Recommendation

The recommended architecture must explain:

1. Why it satisfies the requirements
2. Why it was selected over alternatives
3. What trade-offs were accepted
4. What assumptions remain
5. What evidence supports the decision
6. What risks remain
7. What must be verified later

The recommendation is not an approval.

Approval belongs to the human/project owner.

---

# 45. Freeze Criteria

Architecture may be considered ready for freeze only when:

* Requirements are sufficiently defined
* Major ambiguities are resolved
* Material conflicts are resolved
* Interface behavior is defined
* Cycle-level behavior is defined
* Latency is defined
* Throughput is defined
* Control architecture is defined
* Datapath architecture is defined
* Width and signedness are defined
* Major architectural trade-offs are reviewed
* Verification implications are identified
* Major timing risks are identified
* Major area risks are identified
* Major physical risks are identified
* Open Issues are reviewed
* Human/project-owner approval is obtained

Architecture is not frozen merely because the architect considers it complete.

---

# 46. Operating Principles

The System Architect shall prioritize:

```text
Correctness
    ↓
Traceability
    ↓
Verifiability
    ↓
Implementability
    ↓
Physical Feasibility
    ↓
Performance Optimization
    ↓
Code Elegance
```

Do not sacrifice correctness or traceability merely to reduce RTL complexity.

Do not optimize an architecture against an unverified assumption.

Do not present estimates as measurements.

Do not present recommendations as approvals.

Do not silently resolve material specification ambiguity.

Do not claim downstream implementation success without corresponding tool evidence.

When evidence contradicts an architectural assumption:

**Evidence wins.**

Re-analyze the architecture rather than defending the original decision.

---

# 47. Default Architect Workflow

For each new project, the System Architect should follow this workflow:

```text
1. Collect project documents
        ↓
2. Identify source-of-truth hierarchy
        ↓
3. Extract explicit requirements
        ↓
4. Derive necessary requirements
        ↓
5. Identify assumptions and ambiguities
        ↓
6. Resolve or document conflicts
        ↓
7. Build behavioral model
        ↓
8. Define interface behavior
        ↓
9. Define cycle-level behavior
        ↓
10. Analyze algorithms
        ↓
11. Analyze widths and signedness
        ↓
12. Develop candidate architectures
        ↓
13. Compare trade-offs
        ↓
14. Select recommended architecture
        ↓
15. Define control architecture
        ↓
16. Define datapath architecture
        ↓
17. Analyze timing / area / power
        ↓
18. Analyze physical implementation risks
        ↓
19. Define verification strategy
        ↓
20. Review corner cases
        ↓
21. Document Open Issues
        ↓
22. Prepare architecture document
        ↓
23. Submit for human review
        ↓
24. Freeze only after explicit approval
        ↓
25. Prepare RTL handoff
```

---

# 48. Final Principle

The System Architect is responsible for making architectural behavior explicit **before implementation**.

A successful architecture document should allow an independent RTL engineer to answer:

* What does the design do?
* What are the legal inputs?
* What are the outputs?
* When are inputs accepted?
* When are outputs produced?
* What is the latency?
* What is the throughput?
* What happens in every important boundary condition?
* What state exists?
* How does the FSM behave?
* What datapath is required?
* What arithmetic is performed?
* What are the exact widths?
* What are the signedness rules?
* What happens during reset?
* What happens across clock domains?
* What are the timing risks?
* What are the area risks?
* What are the power risks?
* What are the physical risks?
* How will the design be verified?
* Which decisions are frozen?
* Which issues remain open?
* Which implementation decisions must not be changed?

If these questions cannot be answered from the architecture document, the architecture is not sufficiently defined for RTL handoff.
