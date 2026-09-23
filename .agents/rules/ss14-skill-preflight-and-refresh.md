<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# SS14 Skill Selection

Use this index when a gameplay or content task needs domain guidance and the right skill is not obvious. Open only the matching skill and any rule that governs the changed files. For a known file or a small direct fix, start with the code. After compaction, reread only guidance needed for the remaining work.

## Common cases

| Work | Skill |
|---|---|
| Naming new identifiers | `ss14-naming-conventions` |
| Adding or changing prototypes | `ss14-ecs-prototypes`; `ss14-prototypes-locale` for locale or research nodes |
| Changing inherited or upstream content | `ss14-upstream-maintenance` |
| Components, entity APIs, systems, events, or prediction | Choose the relevant one of `ss14-ecs-components`, `ss14-ecs-entities`, `ss14-ecs-systems`, `ss14-events`, `ss14-prediction` |
| Selecting a test layer or writing tests | `ss14-tests-authoring` |
| Tracing a bug with runtime evidence | `ss14-debugging-workflow`; `ss14-common-api-patterns` only when unfamiliar helpers matter |
| Explaining SS14 architecture | Choose `ss14-prototype-basics`, `ss14-ecs-basics`, or `ss14-client-server-shared` by topic |
| Multi-area gameplay feature | `ss14-gameplay-feature` |
| Documenting a large change | `ss14-documentation-writing` |

## Specialized areas

| Work | Skill |
|---|---|
| Hot paths, `Update()`, frequent events | `ss14-standard-optimizations` |
| Player-facing text or FTL | `ss14-localization-strings`; add `ss14-localization-code` for C# text APIs or localized fields |
| Network events, `NetEntity`, replicated state | `ss14-netcode` |
| `Appearance`, `GenericVisualizer`, sprite layers | `ss14-graphics-generic-visualizer-appearance` |
| Sprites, RSI, overlays, shaders | `ss14-sprite-overlays-shaders` |
| Audio routing, assets, collections, predicted sound | `ss14-audio` |
| Atmospherics, gases, fire, pipes | `ss14-atmos` |
| Transforms, grids, collision, physics | `ss14-transform-physics` |
| PVS and visibility | `ss14-pvs` |
| XAML, BUI, or EUI | Choose `ss14-ui-xaml`, `ss14-ui-bui`, or `ss14-ui-eui` by UI layer |
| Database models, EF Core, migrations | `ss14-databases-migrations` |
| NPCs, HTN, pathfinding | `ss14-npc-ai` |
| Orion research and R&D | `ss14-orion-research`; add `ss14-prototypes-locale` for nodes, recipes, or locale |
| Orion machine parts and construction | `ss14-orion-machine-parts`; add `ss14-tests-authoring` when behavior changes |
| Orion banking, Paydex, vending | `ss14-orion-economy-banking-vending`; add `ss14-ui-bui` for BUI changes |
| Porting, license, attribution | `ss14-porting-and-licensing` |
| Using AI tools in this repository | `ss14-ai-workflow` |

For a task spanning several areas, use only the skills that affect the actual edit. If Rider MCP is available, use it when symbol navigation, refactoring, or diagnostics help; direct search is sufficient for simple lookups.
