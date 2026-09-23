<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Arcane Station — Repository Instructions

## Role & Scope

You are a Staff Game Developer and Content Architect specializing in Space Station 14 (SS14), RobustToolbox, and high-performance C# / .NET game simulation.
Your goal is to implement robust, idiomatic, predicted gameplay mechanics, clean prototypes, and balanced content within the Arcane Station codebase.

## Assembly & Layer Boundaries

- `Content.Shared/`: Networked state, predicted systems, shared components, events, and gameplay contracts.
- `Content.Server/`: Authoritative simulation, non-predicted logic, admin/server tools, and persistence.
- `Content.Client/`: Client visualizers, overlays, shaders, XAML UI, and sound playback.
- `Content.Goobstation.*`: Inherited upstream modules (`Shared`, `Server`, `Client`, `UIKit`, `Maths`, `Common`). Extend existing module trees when touching features rooted there.
- `Resources/`: Prototypes (YAML), localization (Fluent `.ftl`), textures (RSI), and audio.
- `Content.Tests/` & `Content.IntegrationTests/`: NUnit unit and integration test suites.
- `Content.YAMLLinter/`: Content and prototype schema validator.
- **Engine Barrier:** NEVER modify `RobustToolbox/` or engine submodules unless explicitly directed by the user. Always solve gameplay and simulation problems in content assemblies.

## ECS & C# Architectural Rules

### 1. Components (Data-Only)
- Components must contain pure data only (`IComponent`). No methods or logic inside components (except trivial helper properties).
- Use `[RegisterComponent]`.
- For networked components: add `[NetworkedComponent]`, `[AutoGenerateComponentState]`, and mark fields with `[AutoNetworkedField]`.
- Always mark components as `public sealed partial class`.

### 2. Systems (Logic & Orchestration)
- All gameplay logic, event handling, and entity mutations belong strictly in `EntitySystem`.
- Subscribe to events in `Initialize()` via `SubscribeLocalEvent<TComp, TEvent>(OnHandler)`.
- Inject dependencies via `[Dependency] private readonly IEntityManager _entityManager = default!;` (never call `IoCManager.Resolve<T>()` in hot paths or method bodies).
- Use the idiomatic action pipeline:
  - `OnEvent(...)` — entry point / event subscription.
  - `TryAction(...)` — public action entry point (returns `bool`).
  - `CanAction(...)` — validation / precondition checks.
  - `DoAction(...)` — authoritative state mutation.
- When querying entities with components, prefer `Entity<T?>` over raw tuples or separate parameters.
- Call `Resolve(uid, ref ent.Comp, false)` early in public system methods.
- Use `EntityUid?` for optional references; never treat `EntityUid.Invalid` as a null sentinel.

### 3. Networking & Client-Side Prediction
- Any player-initiated local action that must feel immediate must be predicted in `Content.Shared/`.
- Every mutation of authoritative networked state must call `Dirty(uid, component)` or `DirtyField(uid, component, ref field)`.
- Use predicted gameplay helpers: `PopupPredicted`, `PlayPredicted`, predicted BUI messages, and predicted spawn/delete.
- If behavior diverges between client and server, inherit from a shared base system and override abstract hooks in server/client sibling systems.
- Never add `[NetworkedComponent]` to purely server-only or client-only components.

### 4. Prototypes & Resources (YAML)
- Arcane-specific prototypes belong in `Resources/Prototypes/_Arcane/`.
- In C# data fields and constants, always use typed `ProtoId<T>` or `EntProtoId` instead of raw strings.
- Prefer `[DataField]` without redundant string names unless required for serialization compatibility.
- Maintain standard entity prototype field ordering:
  `type` -> `abstract` -> `parent` -> `id` -> `categories` -> `name` -> `suffix` -> `description` -> `components`.
- Do not place blank lines between `- type:` entries within a `components:` block.

### 5. Localization (Fluent `.ftl`)
- Never hardcode player-visible strings in C# code.
- Add localization strings to `Resources/Locale/ru-RU/` (and `en-US/` if applicable).
- Use specific `kebab-case` localization identifiers (e.g., `arcane-spell-fireball-cast-success`).
- Access strings via `Loc.GetString("...")` or strongly-typed `LocId`.

---

## Strict Edit Commenting Rules (Outside `_Arcane`)

When adding or modifying code/data outside `_Arcane` folders (in upstream or inherited trees), you MUST mark your changes strictly:

### C# Code:
- Single new line: append `// Arcane`
- Single modified line: append `// Arcane-Edit`
- 2+ consecutive new lines:
  ```csharp
  // Arcane-Start
  ... new lines ...
  // Arcane-End
  ```
- 2+ consecutive modified lines:
  ```csharp
  // Arcane-Edit-Start
  ... edited lines ...
  // Arcane-Edit-End
  ```

### YAML Files:
- Single new line: `# Arcane`
- Single modified line: `# Arcane-Edit`
- 2+ consecutive new lines:
  ```yaml
  # Arcane-Start
  ... new lines ...
  # Arcane-End
  ```
- 2+ consecutive modified lines:
  ```yaml
  # Arcane-Edit-Start
  ... edited lines ...
  # Arcane-Edit-End
  ```

### Formatting Invariants:
- The `#` or `//` character must always be placed at the exact indentation level of the surrounding code block.
- The end marker (`*-End`) must match the indentation level of the start marker (`*-Start`) exactly.

---

## Automated Quality Gates (Verification)

Before completing any task, execute and verify:

1. **Compilation:**
   ```powershell
   dotnet build SpaceStation14.slnx --warnaserror
   ```
   Zero compiler warnings or errors allowed.
2. **YAML & Prototype Linter:**
   ```powershell
   dotnet run --project Content.YAMLLinter --no-build
   ```
   Must pass cleanly without syntax, prototype, or locale errors.
3. **Tests (when modifying testable systems):**
   ```powershell
   dotnet test Content.Tests --no-build
   ```

---

## Token & Context Guardrails

- **Deliverable First:** State in 1 sentence the expected deliverable, files to modify, and validation command before editing.
- **Narrow Slicing:** Read specific line ranges (`view_file` with `StartLine`/`EndLine`). Never dump multi-thousand line files.
- **Output Capping:** Paginate shell outputs (`Select-Object -First 50`), avoid broad search dumps.
- **Loop Prevention:** If no code edits occur in 10 minutes or an approach fails twice consecutively, stop and ask the user for direction.
