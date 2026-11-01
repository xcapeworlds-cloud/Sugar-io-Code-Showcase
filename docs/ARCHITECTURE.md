# Architecture and code-review notes

## Purpose

Sugar.io is a working gameplay-systems prototype whose Unity scenes, prefabs and supporting assets are kept outside this public code showcase. This repository contains source files for review, not a buildable Unity project.

## High-level responsibilities

| Responsibility | Main classes |
| --- | --- |
| Match lifecycle and balance | `TornadoMatchManager`, `MatchBalance`, `LevelManager` |
| Player and AI movement | `PlayerController`, `Tornado`, `TornadoAI`, `TornadoAINavigation` |
| Combat and destructible environment | `TornadoCombat`, `TornadoDestructible`, `TornadoEnvironmentSuction` |
| Collectibles and spawning | `Candy`, `CandySpawner`, `TornadoAISpawner` |
| Visual feedback | `TornadoVFX`, `CandySwirlVFX`, `SugarUI`, `SugarPopup` |

```text
PlayerController ──> Tornado <── TornadoAI
                        │              │
                        │       TornadoAINavigation
                        │
                  TornadoCombat
                        │
               Destructible objects

Candy / CandySpawner ──> Tornado sugar and growth
                                  │
                              SugarChanged
                                  │
                               SugarUI

TornadoMatchManager ──> MatchBalance / match lifecycle
```

This diagram is conceptual; it does not show every Unity component reference or event.

## Focused showcase refactor: SugarUI

The original HUD polled sugar every `Update`, including text-change checks. The showcase revision subscribes to `Tornado.SugarChanged`, updates only when the displayed integer changes, and periodically searches for a replacement player after defeat or destruction. Event handlers are detached when the component is disabled or destroyed.

**Trade-off:** A player discovered after scene activation can take up to half a second to appear. A scene-level player-spawn event would eliminate polling entirely, but would require additional integration with the original project.

## Technical debt and limitations

- `TornadoMatchManager`, `CandySpawner`, `TornadoEnvironmentSuction`, and `TornadoVFX` are large classes. They are candidates for extracting lifecycle, spawning policies, suction rules, and geometry generation into smaller responsibilities.
- Unity `MonoBehaviour` serialization and prefab references constrain safe refactoring. Renaming scripts or serialized fields without migration can break scenes.
- `TornadoEnvironmentGravity` is a compatibility component and should not be removed from a running Unity project without checking scene references.
- `TornadoVFX` refers to a custom shader that is not included in this repository.
- The repository does not include a complete Unity project, so no credible compilation, runtime or FPS claim can be made from the source snapshot alone.

## Verification checklist for Unity integration

- [ ] Open a copy of the original project in the intended Unity version.
- [ ] Confirm scripts compile with the project's packages, shaders and assembly definitions.
- [ ] Inspect missing-script warnings and serialized references on prefabs/scenes.
- [ ] Test player spawning, defeat, respawning, and sugar HUD updates.
- [ ] Test candy collection, AI movement and stuck recovery, combat and destruction.
- [ ] Profile mobile device performance before making optimization claims.

## Provenance

The source was supplied from the author's working Sugar.io prototype. The original project was not modified by these GitHub showcase changes. Commit history distinguishes the uploaded baseline from subsequent improvements.
