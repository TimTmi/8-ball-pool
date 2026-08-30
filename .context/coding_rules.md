# Coding Rules: 8 Ball Pool

## Priority Order
1. Correctness
2. Simplicity
3. Readability
4. Maintainability
5. Performance
6. Cleverness

---

## Coding Standards

### General
- Prefer simple solutions over abstract/flexible designs
- Keep files focused on one responsibility
- Delete dead code immediately
- Avoid premature optimization
- Avoid unnecessary abstractions

### Structure
- Follow standard Unity folder structure (`Assets/Scripts`, `Assets/Prefabs`, `Assets/Scenes`, etc.)
- Use namespaces based on folders (e.g., `EightBall.Core`, `EightBall.UI`)
- Separate UI logic from Game/Physics logic

### Complexity
- Max function length: ~40 lines unless justified
- Max nesting depth: 3
- Prefer early returns over nested conditionals
- Refactor duplicated logic immediately after second occurrence

### Naming
- PascalCase for Classes, Structs, Enums, Methods, and Properties
- camelCase for local variables and parameters
- _camelCase for private fields
- Use full descriptive names
- Avoid generic names like `util`, `helper`, `manager`, `misc` (use specific managers like `GameLoopManager`)
- Boolean names should read naturally (e.g., `isFull`, `hasConflict`)

### State and Side Effects
- Prefer pure functions where possible
- Avoid hidden side effects
- Pass dependencies explicitly (e.g., via SerializeField or constructor injection)
- No mutable global state where avoidable (use ScriptableObjects or scoped managers)

### Error Handling
- Never swallow exceptions silently
- Return user-safe error messages at API boundaries
- Check for null references especially with Unity components (`GetComponent`, `Find`)

### Testing
- Critical business logic (like 8-ball rules) should be decoupled from Unity `MonoBehaviour` where possible to allow unit testing

### Refactor Triggers
Refactor immediately when:
- duplicate logic appears twice
- function needs comment to explain flow
- parameter count exceeds 4
- branching becomes difficult to follow
- file handles multiple domains/responsibilities

### Forbidden
- giant service classes
- god objects
- boolean parameter traps
- deep inheritance trees
- copy-paste reuse
- empty `Update()` or `Start()` methods

---

## Documentation
- Update `decisions.md` whenever significant architectural decisions change
- Keep `progress.md` updated after major milestones
- Documentation must reflect current implementation reality, not plans
- Record important tradeoffs and rejected approaches in `decisions.md`

---

## Interaction
- When service/domain complexity grows, update `design.md` or add a focused `.context` file before implementation
- Ask for clarification instead of guessing critical business rules
- Prefer incremental implementation over massive one-shot generation
- If existing code violates rules, refactor nearby code while working
