<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# AGENTS.md

## How To Use Repo Guidance

- Read only the repository guidance relevant to the current task. Do not preload all files in `.agents/rules/` or `.agents/skills/`.
- For domain-specific gameplay work, consult the relevant rule or skill directly. Use [the skill router](.agents/rules/ss14-skill-preflight-and-refresh.md) only when you need help choosing one.
- Read a nearer subtree `AGENTS.md` when working in that subtree.
- Keep output and file reads focused; report the checks actually run and any material gaps.

## Scope

This repository is a large Space Station 14 fork with a clear split between gameplay code, client code, and content data:

- `Content.Shared/`, `Content.Server/`, `Content.Client/`: main shared/server/client content assemblies.
- `Content.Goobstation.Shared/`, `Content.Goobstation.Server/`, `Content.Goobstation.Client/`, plus `Content.Goobstation.Common/`, `Content.Goobstation.Maths/`, and `Content.Goobstation.UIKit/`: real module assemblies included in `SpaceStation14.sln`; check references before moving code across them.
- `Content.Server.Database/` and `Content.Shared.Database/`: database and persistence projects.
- `Resources/`: prototypes, locale, maps, textures, audio, guidebook/server info, and other content data.
- `Content.Tests/`: NUnit content/unit tests.
- `Content.IntegrationTests/`: NUnit integration tests that boot larger client/server slices.
- `Content.YAMLLinter/`: repository YAML/prototype/content validation tool.

## Working Style

- Make the smallest change that fully solves the task.
- Respect existing folder and system boundaries; do not create new `Misc` buckets.
- Do not mix feature work, bug fixes, refactors, and mapping sweeps unless the task clearly requires it.
- Read nearby systems, prototypes, and tests before introducing a new pattern.
- Prefer expanding an established feature path over inventing a parallel architecture.

## Engine Boundaries

- Do not edit `RobustToolbox/` or other engine-side files unless the task explicitly requires it.
- Prefer fixing gameplay behavior in content code before assuming an engine change is needed.
- For Orion-only behavior, prefer existing `_Orion` folders when they exist. This repository also contains inherited/vendor-specific trees such as `_Goobstation`, `_EinsteinEngines`, `_Shitmed`, `_DV`, `_NF`, `_Mono`, `_RMC14`, `_White`, and others; extend the tree that already owns the feature instead of assuming all fork code belongs under `_Orion`.
- When you must touch an upstream content file, keep the diff narrow and preserve surrounding structure and style.
- When adding or changing Orion-specific code in an inherited file outside any `_Orion` path, mark it when the surrounding file already uses edit markers or the change would otherwise be hard to distinguish:
  - Single added or changed line: append `// Orion` as an inline comment.
  - Multiple lines: wrap with block markers:

```csharp
// Orion-Edit-Start
...code here...
// Orion-Edit-End
```

- Keep edit-marker ranges as narrow as practical. For files that do not use `//` comments, use the native comment syntax while preserving `Orion-Edit-Start`, `Orion-Edit-End`, and `Orion`.

## Assembly Placement

- Put main shared data, shared events, networked state, and predicted logic in `Content.Shared/`; use `Content.Goobstation.Shared/` only for code that belongs to that existing module path.
- Put main server-only authority and non-predicted server simulation in `Content.Server/`; use `Content.Goobstation.Server/` for features already rooted in that module.
- Put main client-only visuals, overlays, XAML, and BUI front-ends in `Content.Client/`; use `Content.Goobstation.Client/` or `Content.Goobstation.UIKit/` only when matching existing references and ownership.
- Do not make shared projects depend on client-only or server-only projects.

## ECS Rules

- Keep components data-only. Put gameplay logic in entity systems.
- Prefer entity-system public methods over method events.
- Public entity-system APIs that operate on entities should usually take `Entity<T?>` or `EntityUid` first and call `Resolve(...)` early.
- Prefer `Entity<T?>` over parallel `(EntityUid uid, T component)` parameters when the call site already has the pair.
- Prefer `[Dependency]` fields over ad-hoc `IoCManager.Resolve(...)` inside methods.
- Use `EntityUid?` for optional entities; do not use `EntityUid.Invalid` as a "missing" sentinel.
- Use `sealed`, `abstract`, `static`, or `[Virtual]` on new classes where appropriate.
- Prefer prototypes over enums for in-game content types.
- Use `ProtoId<T>` or `EntProtoId` instead of raw prototype ID strings in data fields and static references.
- Prefer `[DataField]` without string field names on new code unless serializer compatibility or a non-default data name is required.

## Interaction Flow

Use the repo-standard action flow for interactions and state-changing gameplay APIs:

- `OnEvent(...)` is the event entry point.
- `TryDoSomething(...)` is the public action API.
- `CanDoSomething(...)` checks whether the action is allowed.
- `DoSomething(...)` or the execution part of `TryDoSomething(...)` performs the mutation.

More detail lives in `.agents/rules/ss14-interaction-flow.md`.

## Naming

- Event handlers should use `On...` names.
- Public action methods should prefer `Try...`.
- Check methods should prefer `Can...`.
- Dependency fields should use the existing underscore-prefixed style such as `_popup`, `_hands`, `_audio`.
- Use specific `kebab-case` localization IDs.
- Keep new prototype IDs, event names, and component names aligned with nearby conventions rather than inventing new suffixes.

## Prediction And Networking

When a local player action should feel immediate, check whether it should be predicted.

- Predicted systems and their relevant components belong in `Content.Shared/`.
- Shared predicted components should use `NetworkedComponent`, `AutoGenerateComponentState`, and `AutoNetworkedField` where appropriate.
- Dirty networked state every time authoritative data changes. Use `DirtyField(...)` when field deltas make sense.
- Use predicted APIs such as `PopupPredicted`, `PopupClient`, `PlayPredicted`, predicted BUI messages, and predicted spawn/delete helpers instead of server-only equivalents.
- If a shared system needs client/server special cases, keep a shared base plus both server and client concrete systems.
- Never add `NetworkedComponent` to purely server-only or purely client-only components.
- Be careful with predicted randomness and reference-type networked fields.
- Prefer maximum practical prediction support for new player-facing systems instead of adding prediction as after-the-fact cleanup.

## UI

- Prefer XAML over constructing full UIs in C#.
- Keep `.xaml` paired with `.xaml.cs` and the relevant BUI/client system.
- Reuse existing style classes and `FancyWindow` patterns before adding new stylesheet rules.
- Localize all player-visible UI text.
- When the client already has the needed networked component state, prefer reading it instead of duplicating it in separate BUI state objects unless the existing pattern clearly requires both.

## Resources

- Put prototypes under the most specific existing subtree in `Resources/Prototypes/`.
- If you introduce a new prototype parent tree, put parent prototypes in `base.yml` and variants in sibling files.
- Keep entity prototype field order as `type`, `abstract`, `parent`, `id`, `categories`, `name`, `suffix`, `description`, `components`.
- Do not insert blank lines between `- type:` entries inside a `components:` list.
- Separate prototype blocks with one blank line.
- Prefer `suffix` for spawn-menu distinctions instead of changing prototype `name`.
- Use sound collections/specifiers and sprite specifiers instead of ad-hoc raw asset strings in code when reusable content is intended.
- Keep RSI `meta.json` ordered as `version`, `license`, `copyright`, `size`, `states` with 4-space indentation.

## Localization

- Every player-facing string must be localized.
- Add or update FTL entries under `Resources/Locale/`, starting with `en-US`; add/update matching `ru-RU` entries when the same feature already maintains Russian locale or the change is Orion-facing and you can do so without guessing translations.
- Use specific `kebab-case` localization IDs.
- Do not compare localized strings or expose raw enum `ToString()` output to players.
- Treat localization as mandatory work, not optional polish.

## Rider MCP

If Rider MCP is available in the environment, prefer it over shell equivalents for search, navigation, edits, and file diagnostics.

Preferred order:

1. Search and navigation: symbol/text/file/tree tools.
2. Reading and diagnostics: file reads, symbol info, file problems.
3. Solution structure: modules, dependencies, run configurations.
4. Edits and refactors: replace text, rename refactorings, reformat.
5. Verification: build the affected project, then run targeted configurations when available.

If Rider MCP is not available, use the normal shell/file tools.

## Testing And Validation

Pick the smallest verification that meaningfully covers the change.

- SDK: `global.json` pins .NET SDK `9.0.100` with `latestFeature` roll-forward.
- Submodules: run `git submodule update --init --recursive` before restore/build/test when `RobustToolbox/` is not initialized; CI also pulls engine updates before building.
- Baseline build: `dotnet restore` then `dotnet build --configuration DebugOpt --no-restore /m`.
- Content tests after build: `dotnet test --no-build --configuration DebugOpt Content.Tests/Content.Tests.csproj -- NUnit.ConsoleOut=0`.
- Integration tests after build: `dotnet test --no-build --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed`.
- Standalone content test run without a prior build: `dotnet test --configuration DebugOpt Content.Tests/Content.Tests.csproj -- NUnit.ConsoleOut=0`.
- Standalone integration test run without a prior build: `dotnet test --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed`.
- YAML/resource edits: `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj -c DebugOpt`. CI builds Release first and then runs `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj --no-build`.
- RSI edits: `python3 RobustToolbox/Schemas/validate_rsis.py Resources/` after initializing the `RobustToolbox` submodule and installing the script dependencies (`pillow` and `jsonschema`).
- Gameplay/UI fixes should ideally be verified in-game; if you cannot do that locally, say so explicitly.
- If code touches prototypes or FTL, run the YAML linter.
- If code touches C#, build the affected project or the repo slice that covers it.
- If code touches the client, run or otherwise verify the client path when possible and call out when runtime verification was not possible.

More detail lives in `.agents/rules/ss14-testing-and-validation.md`.

## Fork Commenting Conventions (outside `_Arcane`)

Правила комментирования при добавлении или изменении кода/прототипов не в папке `_Arcane`:

- Новая строчка — `# Arcane` (в YAML) / `// Arcane` (в C#)
- Изменённая — `# Arcane-Edit` (в YAML) / `// Arcane-Edit` (в C#)
- 2 и более новых подряд — `# Arcane-Start` ... `# Arcane-End` (или `// Arcane-Start` ... `// Arcane-End`)
- 2 и более изменённых подряд — `# Arcane-Edit-Start` ... `# Arcane-Edit-End` (или `// Arcane-Edit-Start` ... `// Arcane-Edit-End`)

**Важно:**
- Решётка `#` всегда ставится в самом начале закомментированной строки.
- Маркеры (например: `# Arcane-Edit-Start: Removed ...`) располагаются на том же уровне, что и остальной код — без лишних отступов и на одной линии с соседними элементами.
- Маркер конца всегда должен быть строго на том же уровне отступа, что и маркер начала.

## PR Expectations

- Keep feature work, bug fixes, refactors, and mapping changes separate when practical.
- Do not force-push or rewrite history unless explicitly asked.
- For player-visible changes, prepare PR-ready changelog text in `:cl:` format when useful, but do not hand-edit generated changelog artifacts unless the task explicitly asks for it.
- If a change is breaking for APIs, namespaces, or prototype IDs, call it out clearly.
