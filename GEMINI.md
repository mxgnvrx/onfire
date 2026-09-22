<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Gemini Repo Instructions

Follow [AGENTS.md](AGENTS.md) as the primary repository instruction file for this SS14 fork.

Before editing:

- Consult `.agents/rules/` and `.agents/skills/` strictly ON DEMAND (never dump entire folders into context).
- Prefer the nearest subtree `AGENTS.md` when one exists for the touched files.

Core expectations:

- Keep components data-only and behavior in systems.
- Use `On... -> Try... -> Can... -> Do...` for gameplay actions.
- Prefer `Entity<T?>`, `ProtoId<T>`, `EntProtoId`, and localized strings.
- Avoid `RobustToolbox/` edits unless engine work is explicitly required.
- Use prediction and localization as first-pass design constraints, not cleanup.
- Context budgeting: cap tool outputs, slice file reads by line ranges, and stop after 2 failed attempts or 10 min without edits.

Правила комментирования не в папке `_Arcane`:
- Новая строчка: `# Arcane` (YAML) / `// Arcane` (C#)
- Изменённая: `# Arcane-Edit` (YAML) / `// Arcane-Edit` (C#)
- 2+ новых подряд: `# Arcane-Start` ... `# Arcane-End` (или `// Arcane-Start` ... `// Arcane-End`)
- 2+ изменённых подряд: `# Arcane-Edit-Start` ... `# Arcane-Edit-End` (или `// Arcane-Edit-Start` ... `// Arcane-Edit-End`)
- Решётка `#` всегда в самом начале закомментированной строки.
- Маркеры — строго на том же уровне отступа, что и код; маркер конца строго на уровне маркера начала.

