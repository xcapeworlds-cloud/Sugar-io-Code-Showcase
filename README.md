# Sugar.io | Unity / C# Gameplay Systems

**A Unity gameplay-systems prototype built around a candy-themed tornado arena.** Players and AI-controlled tornadoes collect candy, grow, compete, and interact with destructible scenery.

[Explore the source code](Scripts/) · [Architecture and code review](docs/ARCHITECTURE.md)


## What this repository demonstrates

| System | Source | Engineering focus |
| --- | --- | --- |
| AI behavior | [AI](Scripts/AI/) | Decision-making, navigation, stuck recovery, spawn setup |
| Tornado mechanics | [Tornado](Scripts/Tornado/) | Movement, growth, combat, destruction, collision and procedural effects |
| Candy collection | [Collectibles](Scripts/Collectibles/) | Object lifecycle, spatial lookup, appearance and spawning |
| Match flow | [Game](Scripts/Game/) | Match coordination, scoring, balancing and progression |
| Player controls | [Player](Scripts/Player/) | Input and player control |
| World and camera | [Field](Scripts/Field/) · [Camera](Scripts/Camera/) | Play bounds, size classes and camera behavior |
| User interface | [UI](Scripts/UI/) | Sugar display and pooled popup feedback |
| Presentation | [Colors](Scripts/Colors/) | Environment palette and visual setup |

## Selected implementation details

- **AI separation:** `TornadoAI` handles behavior decisions while `TornadoAINavigation` handles navigation and recovery.
- **Collection queries:** `Candy` maintains a registry and spatial-cell lookup for nearby candy queries.
- **Update scheduling:** tornado candy checks are staggered; the sugar HUD now subscribes to `Tornado.SugarChanged` rather than reformatting text every frame.
- **Configurable tuning:** `MatchBalance` holds gameplay tuning parameters used by multiple systems.
- **Visual performance considerations:** some effects use pooling and material property blocks.

These are source-level observations, not performance benchmarks.

## Scope and limitations

This is a **code-review showcase**, not a complete Unity project or a downloadable game. The original working Unity project is separate and has **not** been changed by these repository edits. Scenes, prefabs, project settings, packages, assets and shaders required to build and play the prototype are not all included.

The 29 scripts are provided as an organized source snapshot, with targeted showcase improvements tracked in Git history. **The modified showcase scripts have not been compiled or play-tested in the original Unity project.** Review [architecture notes, trade-offs and validation steps](docs/ARCHITECTURE.md) before reusing them.

## Where to start

1. [Tornado.cs](Scripts/Tornado/Tornado.cs) for growth, movement and gameplay events.
2. [TornadoAI.cs](Scripts/AI/TornadoAI.cs) and [TornadoAINavigation.cs](Scripts/AI/TornadoAINavigation.cs) for AI responsibilities.
3. [Candy.cs](Scripts/Collectibles/Candy.cs) for collection indexing.
4. [TornadoMatchManager.cs](Scripts/Game/TornadoMatchManager.cs) and [MatchBalance.cs](Scripts/Game/MatchBalance.cs) for orchestration and tuning.
5. [SugarUI.cs](Scripts/UI/SugarUI.cs) for a focused event-driven UI refactor.

