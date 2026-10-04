# Python Coding Standard

## 1. Purpose

This document defines the coding standards and implementation conventions for the Python project.

Its purpose is to ensure that all Python code follows a consistent:

* modular structure
* interface design
* naming convention
* type system usage
* error handling strategy
* dependency structure
* constant definition strategy
* class design
* function design
* configuration strategy
* maintainability standard

This document defines **HOW Python code should be written**.

It does not define:

* project requirements
* system architecture
* product behavior
* feature specifications

---

# 2. Coding Standard Hierarchy

When writing Python code, follow:

1. `GLOBAL_RULE.md`
2. `SPECIFICATION.md`
3. `ARCHITECTURE.md`
4. `CODING_STANDARD.md`
5. assigned implementation task

The Coding Engineer must follow this standard unless an explicit project-level decision overrides it.

---

# 3. General Design Principles

The project follows:

* modularity
* separation of concerns
* high cohesion
* low coupling
* explicit interfaces
* predictable behavior
* maintainability
* readability
* type safety where practical

Prefer simple designs over unnecessarily sophisticated abstractions.

Do not introduce an abstraction unless it provides a meaningful engineering benefit.

---

# 4. Module Design

Each module should have a clear responsibility.

A module should not become a collection of unrelated functionality.

Prefer:

```text
project/
├── parser/
│   ├── __init__.py
│   ├── parser.py
│   └── errors.py
│
├── model/
│   ├── __init__.py
│   └── profile.py
│
└── io/
    ├── __init__.py
    └── file.py
```

over:

```text
project/
└── utils.py
```

containing unrelated functions.

---

# 5. Single Responsibility

A module, class, or function should have a clearly identifiable primary responsibility.

Avoid classes such as:

```python
class SystemManager:
    ...
```

that simultaneously handle:

* file I/O
* parsing
* validation
* business logic
* logging
* configuration
* output generation

Separate these responsibilities when they become sufficiently complex.

---

# 6. Package Structure

Prefer a package structure that reflects functional responsibilities.

Example:

```text
src/
└── project/
    ├── __init__.py
    ├── core/
    ├── parser/
    ├── model/
    ├── services/
    ├── io/
    ├── config/
    └── errors/
```

Do not create directories merely to make the project appear more modular.

The directory structure should reflect actual architectural boundaries.

---

# 7. Public vs Internal API

Public APIs should be explicit.

Use naming conventions to distinguish internal implementation details.

Public:

```python
def parse_profile(data: bytes) -> Profile:
    ...
```

Internal:

```python
def _parse_tag_header(data: bytes) -> TagHeader:
    ...
```

Internal helpers should not become accidental public interfaces.

---

# 8. Function Design

Functions should generally:

* perform one coherent operation
* have clear inputs
* have clear outputs
* avoid hidden side effects
* remain reasonably small

Avoid extremely large functions.

If a function contains multiple independent responsibilities, consider decomposition.

However, do not split simple logic into dozens of trivial functions merely to reduce line count.

---

# 9. Function Parameters

Prefer explicit parameters.

Avoid excessive use of:

```python
def process(**kwargs):
    ...
```

or:

```python
def process(*args):
    ...
```

when a defined interface is possible.

Prefer:

```python
def process_profile(
    profile: Profile,
    output_path: Path,
    *,
    preserve_metadata: bool = True,
) -> None:
    ...
```

when explicitness improves clarity.

---

# 10. Return Values

Return values should have predictable meaning.

Avoid functions that sometimes return:

```text
None
```

and sometimes:

```text
object
```

unless this behavior is explicitly defined.

For operations with multiple meaningful outputs, consider:

* dataclass
* named structure
* explicit result object

rather than undocumented tuples.

---

# 11. Type Hints

Use type hints for public functions, important internal functions, and complex data structures.

Prefer:

```python
def read_tag(data: bytes, offset: int) -> bytes:
    ...
```

over:

```python
def read_tag(data, offset):
    ...
```

when type information improves clarity.

Avoid unnecessarily complicated type expressions.

---

# 12. `dataclass`

Use `dataclass` for structured data objects that primarily represent state.

Example:

```python
from dataclasses import dataclass

@dataclass
class TagInfo:
    signature: str
    offset: int
    size: int
```

Use `dataclass` when:

* the object primarily stores data
* fields are well-defined
* automatic initialization/repr/equality are useful

Do not use `dataclass` merely because a class exists.

---

# 13. Enum

Use `Enum` when a variable represents a finite set of named states or categories.

Example:

```python
from enum import Enum

class ProcessingMode(Enum):
    READ_ONLY = "read_only"
    MODIFY = "modify"
    VALIDATE = "validate"
```

Prefer:

```python
ProcessingMode.MODIFY
```

over:

```python
"modify"
```

when the value represents a controlled domain state.

Do not create an Enum for arbitrary strings that do not form a meaningful finite domain.

---

# 14. Abstract Base Classes

Use `ABC` / `abstractmethod` when multiple implementations must follow a common contract.

Example:

```python
from abc import ABC, abstractmethod

class TagParser(ABC):

    @abstractmethod
    def parse(self, data: bytes) -> object:
        ...
```

Appropriate use cases include:

* interchangeable implementations
* plugin systems
* backend abstraction
* storage abstraction
* parser abstraction
* strategy pattern

Do NOT use abstract classes merely because classes appear related.

If only one implementation exists and no meaningful substitution is required, prefer a normal class or function.

---

# 15. Protocol

Prefer `typing.Protocol` when structural typing is sufficient and inheritance is unnecessary.

Example:

```python
from typing import Protocol

class FileReader(Protocol):

    def read(self, size: int) -> bytes:
        ...
```

Use `Protocol` when the project benefits from:

* loose coupling
* dependency inversion
* test doubles
* interchangeable implementations

Prefer `ABC` when explicit inheritance and runtime class hierarchy are meaningful.

---

# 16. ABC vs Protocol

Use the following guideline:

```text
Need explicit inheritance / shared base behavior?
        ↓
       ABC

Need interface compatibility without inheritance?
        ↓
     Protocol

Only one implementation and no abstraction benefit?
        ↓
     Normal class/function
```

Do not create both an ABC and Protocol for the same interface unless there is a documented reason.

---

# 17. Constants

Python does not use C/C++-style `#define` for normal constant definitions.

Use module-level constants.

Example:

```python
MAX_TAG_COUNT = 64
DEFAULT_BUFFER_SIZE = 4096
ICC_HEADER_SIZE = 128
```

Use uppercase naming for constants.

For stronger type-checking intent:

```python
from typing import Final

ICC_HEADER_SIZE: Final = 128
```

---

# 18. Constant Organization

Related constants should be grouped in an appropriate module.

Example:

```text
project/
└── constants.py
```

or, for larger systems:

```text
project/
└── constants/
    ├── __init__.py
    ├── file.py
    ├── protocol.py
    └── limits.py
```

Do not create a giant `constants.py` containing unrelated project data.

Constants should be grouped by responsibility.

---

# 19. Enum vs Constant

Use:

### Constant

when representing a fixed numeric/string/value:

```python
MAX_TAG_COUNT = 64
ICC_HEADER_SIZE = 128
```

### Enum

when representing a finite semantic category:

```python
class TagType(Enum):
    TEXT = "text"
    BINARY = "binary"
    PROFILE = "profile"
```

Do not use constants as pseudo-enums:

```python
MODE_READ = 0
MODE_WRITE = 1
MODE_VALIDATE = 2
```

Prefer an Enum when these values represent semantic states.

---

# 20. Magic Numbers

Avoid unexplained magic numbers.

Bad:

```python
if len(data) < 128:
    ...
```

Prefer:

```python
ICC_HEADER_SIZE = 128

if len(data) < ICC_HEADER_SIZE:
    ...
```

If the value has protocol or specification significance, name it according to that meaning.

---

# 21. Error Classes

Define project-specific exceptions when callers need to distinguish failure categories.

Example:

```python
class ProfileError(Exception):
    """Base exception for profile processing errors."""


class InvalidProfileError(ProfileError):
    """Raised when the profile structure is invalid."""


class UnsupportedTagError(ProfileError):
    """Raised when a tag cannot be processed."""
```

Prefer an exception hierarchy over unrelated exception classes when failures belong to the same domain.

---

# 22. Exception Handling

Catch exceptions only when the current layer can meaningfully handle them.

Avoid:

```python
try:
    ...
except Exception:
    ...
```

unless there is a clear architectural reason.

Do not silently suppress exceptions.

Use exception chaining when converting low-level errors:

```python
try:
    data = path.read_bytes()
except OSError as exc:
    raise ProfileReadError(path) from exc
```

---

# 23. Logging

Use the standard `logging` module for application-level logging unless the project specifies another logging system.

Prefer:

```python
logger = logging.getLogger(__name__)
```

over:

```python
print("processing...")
```

for operational logs.

Do not log sensitive data unnecessarily.

---

# 24. Configuration

Configuration values should not be scattered throughout the code.

Avoid:

```python
timeout = 30
```

appearing independently in multiple modules.

Centralize configuration where appropriate.

Example:

```python
@dataclass
class AppConfig:
    timeout: float
    max_file_size: int
```

Configuration should be distinguishable from immutable program constants.

---

# 25. Mutable Global State

Avoid mutable global variables.

Bad:

```python
cache = {}
```

used as hidden shared state across unrelated modules.

Prefer explicit ownership and dependency passing.

If global state is genuinely required, document its purpose and lifecycle.

---

# 26. Imports

Prefer absolute imports within the project.

Example:

```python
from project.parser import ProfileParser
```

Avoid circular dependencies.

If two modules require each other, reconsider the module boundary before introducing import hacks.

Do not use wildcard imports:

```python
from module import *
```

---

# 27. Circular Dependency Prevention

Architecture should minimize circular imports.

If:

```text
A → B
B → A
```

appears necessary, investigate whether:

* shared data should move to another module
* an interface should be extracted
* dependency direction should change
* responsibilities should be separated

Do not solve architectural circular dependencies merely with local import tricks.

---

# 28. Dependency Direction

Dependencies should generally flow from higher-level orchestration toward lower-level implementation.

Avoid unnecessary dependency cycles.

Example:

```text
Application
    ↓
Service
    ↓
Domain / Model
    ↓
Low-level IO
```

Low-level modules should not unnecessarily depend on high-level application modules.

---

# 29. Data Models

Use appropriate data structures based on semantics.

Use:

* `dataclass` for structured mutable/immutable domain data
* `Enum` for finite semantic states
* `TypedDict` for dictionary-shaped external data when appropriate
* `NamedTuple` only when tuple semantics are actually useful
* normal classes for behavior-rich objects
* dictionaries for genuinely dynamic key/value data

Do not use dictionaries for strongly structured domain objects merely for convenience.

---

# 30. `TypedDict`

Use `TypedDict` when interacting with dictionary-shaped data whose keys are known.

Example:

```python
from typing import TypedDict

class TagRecord(TypedDict):
    signature: str
    offset: int
    size: int
```

This is particularly useful for:

* JSON-like data
* configuration structures
* external API responses

For internal domain objects with behavior, prefer a class or dataclass.

---

# 31. String Literals vs Enum

Do not automatically convert every string into an Enum.

Use a string directly when:

* the value is open-ended
* external data defines the value
* there is no finite controlled domain

Use Enum when:

* valid values are finite
* values have semantic meaning
* invalid states should be prevented

---

# 32. Utility Modules

Avoid creating a large generic:

```text
utils.py
```

containing unrelated functions.

Prefer domain-specific modules:

```text
binary_utils.py
path_utils.py
validation.py
encoding.py
```

If a utility is only used by one module, consider keeping it private to that module.

---

# 33. Class Design

Use classes when an object has:

* meaningful state
* meaningful behavior
* lifecycle
* invariants
* multiple related operations

Prefer functions when the operation is stateless and simple.

Do not convert every function into a class.

---

# 34. Inheritance

Prefer composition over inheritance unless inheritance expresses a real relationship.

Use inheritance when:

* substitutability exists
* shared interface is meaningful
* polymorphism is required

Avoid deep inheritance hierarchies.

Prefer shallow hierarchies.

---

# 35. Composition

Prefer:

```python
class ProfileService:
    def __init__(self, parser: ProfileParser):
        self.parser = parser
```

over embedding unrelated responsibilities through inheritance.

Composition should be the default approach for assembling behavior.

---

# 36. Dependency Injection

When a component depends on an external service, parser, storage mechanism, or backend, consider injecting the dependency.

Example:

```python
class ProfileService:
    def __init__(self, reader: FileReader):
        self.reader = reader
```

This improves:

* modularity
* testability
* replacement of implementations

Do not introduce dependency injection frameworks unless the project actually requires one.

Simple constructor injection is preferred.

---

# 37. Side Effects

Make side effects explicit.

Functions that modify files, network state, databases, or external resources should have clear naming and responsibility.

Avoid functions that appear to be pure calculations but secretly modify global or persistent state.

---

# 38. File I/O

Separate file I/O from parsing or business logic when practical.

Prefer:

```text
File Reader
    ↓
Raw Data
    ↓
Parser
    ↓
Domain Model
    ↓
Business Logic
```

rather than embedding all operations into a single function.

---

# 39. Binary Data

When processing binary data:

* explicitly define byte order
* validate offsets
* validate lengths
* validate boundaries
* avoid implicit encoding assumptions
* use appropriate standard-library tools
* isolate binary parsing logic

Prefer named constants for format-defined sizes.

Example:

```python
HEADER_SIZE: Final = 128
ENTRY_SIZE: Final = 12
```

---

# 40. Resource Management

Use context managers for resources whenever appropriate.

Prefer:

```python
with path.open("rb") as file:
    data = file.read()
```

over manually managing:

```python
file = open(...)
...
file.close()
```

unless there is a specific reason.

---

# 41. Mutable Default Arguments

Never use mutable objects as default function arguments.

Avoid:

```python
def process(items=[]):
    ...
```

Prefer:

```python
def process(items=None):
    ...
```

or an appropriate immutable/default representation.

---

# 42. Boolean Parameters

Avoid excessive boolean parameters that make calls unclear.

Instead of:

```python
process(profile, True, False, True)
```

prefer explicit keyword arguments:

```python
process(
    profile,
    preserve_metadata=True,
    validate=True,
    overwrite=True,
)
```

If many boolean options exist, consider a configuration object.

---

# 43. Naming

Use standard Python naming conventions:

```text
module_name
function_name
variable_name
ClassName
CONSTANT_NAME
_internal_name
```

Names should communicate intent.

Avoid meaningless names such as:

```text
data
temp
obj
thing
x
manager
helper
utils
```

unless their context makes the meaning genuinely obvious.

---

# 44. File Naming

Python modules should generally use:

```text
snake_case.py
```

Avoid:

```text
CamelCase.py
```

unless required by an external convention.

---

# 45. Documentation Strings

Public modules, classes, and functions should have docstrings when their purpose is not obvious.

Docstrings should explain:

* purpose
* important inputs
* important outputs
* important exceptions
* important constraints

Do not write meaningless docstrings that merely repeat the function name.

---

# 46. Testing-Friendly Design

Production code should be designed so that important logic can be exercised independently.

Prefer:

```text
Input
 ↓
Pure transformation
 ↓
Result
```

when practical.

Avoid embedding business logic directly inside:

* CLI entry points
* file I/O
* global initialization
* environment-specific code

---

# 47. CLI Design

If the project provides a CLI, keep CLI parsing separate from core business logic.

Prefer:

```text
CLI
 ↓
Command Handler
 ↓
Application Service
 ↓
Domain Logic
```

The CLI should primarily:

* parse arguments
* construct configuration
* invoke application logic
* report results/errors

---

# 48. Environment Handling

Do not scatter environment-specific checks throughout the code.

Centralize environment/configuration handling where practical.

Avoid hard-coded machine-specific paths.

Bad:

```python
path = "C:\\Users\\Developer\\Desktop\\project"
```

Prefer configuration or relative/project-defined paths.

---

# 49. Performance-Sensitive Code

When performance matters:

1. establish the requirement
2. identify the bottleneck
3. measure where practical
4. optimize the bottleneck
5. preserve correctness
6. document non-obvious optimizations

Do not optimize based solely on intuition.

---

# 50. Security

Avoid:

* unsafe deserialization
* arbitrary command execution
* path traversal vulnerabilities
* uncontrolled file writes
* leaking sensitive information
* unnecessary privilege requirements

External input should be treated as untrusted unless its trust boundary is explicitly defined.

---

# 51. Backward Compatibility

When modifying an existing project:

consider whether existing:

* APIs
* configuration
* file formats
* CLI behavior
* function behavior
* data structures

must remain compatible.

Breaking changes require explicit approval.

---

# 52. Code Duplication

Avoid unnecessary duplication.

If identical logic appears repeatedly, consider extracting shared functionality.

However, do not prematurely create generic abstractions for code that only happens to look similar.

Prefer meaningful duplication over an incorrect abstraction.

---

# 53. Abstraction Decision Rule

Before creating an abstraction, ask:

```text
Is there a real shared concept?
        ↓
Is there more than one meaningful implementation/use case?
        ↓
Does the abstraction reduce coupling or duplication?
        ↓
Does it improve maintainability?
```

If the answer is mostly NO:

Prefer a simpler implementation.

---

# 54. Standard Abstraction Selection

Use the following guideline:

```text
Simple calculation / stateless operation
        → function

Structured data
        → dataclass

Finite semantic states
        → Enum

Known dictionary-shaped external data
        → TypedDict

Common interface without inheritance
        → Protocol

Common interface with explicit inheritance
        → ABC

Behavior-rich object
        → class

Interchangeable implementation
        → Protocol / ABC

Shared behavior assembly
        → composition
```

Do not force every concept into a class hierarchy.

---

# 55. Constants / Definitions Organization

Small project-wide definitions may be centralized.

Example:

```text
project/
├── constants.py
├── enums.py
├── errors.py
└── models.py
```

For larger projects, organize them by domain.

Avoid one giant file containing every constant, enum, exception, and model in the project.

---

# 56. No C-Style Preprocessor Thinking

Python does not require C/C++-style preprocessor definitions.

Do not attempt to simulate:

```c
#define MAX_SIZE 1024
```

with unnecessary mechanisms.

Prefer:

```python
MAX_SIZE: Final = 1024
```

Use `Enum`, classes, or configuration objects when the semantic requirement is more complex than a simple constant.

---

# 57. Implementation Consistency

Similar problems should generally be solved using similar patterns.

For example:

If all project-specific errors inherit from:

```python
ProjectError
```

new project-specific errors should follow the same hierarchy.

If all parsers expose:

```python
parse(data)
```

new parsers should follow the same interface unless there is a documented reason not to.

Consistency is preferred over individual stylistic preference.

---

# 58. Exceptions to the Standard

A coding standard is a guideline, not a reason to produce poor code.

If a rule should not be followed for a specific case:

1. identify the rule
2. explain why it is unsuitable
3. explain the alternative
4. document the deviation when meaningful

Do not silently violate important project conventions.

---

# 59. Code Review Checklist

Before considering implementation complete, check:

### Structure

* Is module responsibility clear?
* Is coupling reasonable?
* Are dependencies correctly directed?

### Design

* Is the chosen abstraction necessary?
* Should this be a function, class, dataclass, Enum, Protocol, or ABC?
* Is inheritance actually required?

### Interfaces

* Are public interfaces explicit?
* Are types clear?
* Are return values predictable?

### Constants

* Are magic numbers removed?
* Are constants centralized appropriately?
* Should a semantic value be an Enum instead?

### Errors

* Are exceptions meaningful?
* Are unexpected exceptions suppressed?

### Maintainability

* Is the code readable?
* Is duplication reasonable?
* Are comments explaining WHY where needed?

### Safety

* Is external input validated?
* Are file/resource operations safe?
* Are destructive operations explicit?

---

# 60. Core Principle

The project follows this principle:

> Use the simplest abstraction that correctly represents the problem.

Do not use:

```text
class
```

when a function is sufficient.

Do not use:

```text
ABC
```

when a normal class is sufficient.

Do not use:

```text
Enum
```

when a value is genuinely open-ended.

Do not use:

```text
constant
```

when the value represents a semantic state better represented by an Enum.

Do not create:

```text
utils.py
```

when the functionality belongs to a specific domain module.

The objective of the coding standard is not to maximize the number of design patterns.

The objective is to produce Python code that is:

* modular
* consistent
* understandable
* maintainable
* extensible
* safe
* predictable
* appropriately abstracted
