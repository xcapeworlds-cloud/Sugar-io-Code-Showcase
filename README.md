# Sugar.io | Unity / C# Gameplay Systems

Sugar.io is a **working gameplay-systems prototype**, not a finished game. It explores a candy-themed tornado arena with player and AI-controlled tornadoes, collectible candy, growth, combat, destructible scenery, and match flow.

This public repository is a **code showcase** for software engineering reviewers. The working Unity project remains separate. Scripts may depend on Unity scenes, prefabs, assets, packages, and shaders that are not published here; **do not expect this repository to run as a standalone Unity project**.

## Systems to inspect

| Area | Key scripts | What to look for |
| --- | --- | --- |
| AI decisions and navigation | `TornadoAI.cs`, `TornadoAINavigation.cs`, `TornadoAIBalance.cs` | Wander, collect, environment, hunt and flee decisions; personality variations; navigation and stuck recovery |
| AI spawning | `TornadoAISpawner.cs` | Spawn placement and opponent setup |
| Tornado gameplay | `Tornado.cs`, `TornadoCombat.cs`, `TornadoColors.cs` | Movement, sugar-based growth, collision checks, capture and color progression |
| Collectibles | `Candy.cs`, `CandySpawner.cs`, `CandyAppearance.cs` | Candy lifecycle, spatial lookup, spawning, visual customization |
| Destructible environment | `TornadoDestructible.cs`, `TornadoEnvironmentSuction.cs`, `TornadoRuntimeCollider.cs` | Size-gated destruction, object ownership, debris suction and collision setup |
| Match and progression | `TornadoMatchManager.cs`, `MatchBalance.cs`, `LevelManager.cs`, `WorldSizeSystem.cs` | Match states, scoring, configurable balance, level activation and size classes |
| Player and camera | `PlayerController.cs`, `CameraFollow.cs`, `PlayingField.cs` | Touch and keyboard input, camera tracking and playable bounds |
| Presentation | `TornadoVFX.cs`, `CandySwirlVFX.cs`, `KawaiiColors.cs`, `KawaiiFloor.cs`, `GameAudioManager.cs` | Procedural tornado visuals, collection effects, color palette and audio |
| UI | `SugarUI.cs`, `SugarPopup.cs` | Sugar display and pooled world-space feedback |

## Engineering details

- The AI decision component is separate from its navigation helper.
- Candy maintains an active registry and a spatial-cell lookup.
- Tornado movement and candy checks run in Unity's physics update, with candy checks staggered across instances.
- Match settings are exposed through a serializable `MatchBalance` configuration.
- Some visual effects use pooling and material property blocks.

These are examples visible in the source, **not claims of a complete production architecture or benchmarked performance**.

## Repository scope

This is a source-code review project. It is not a downloadable game, and it does not include every Unity scene, prefab, asset, package, or shader required to compile and run the prototype.

The original Unity project has not been modified for this showcase. Refactoring and code review can be documented separately from the working prototype.

## Screenshot

`Unity_Editor_Proof.png` shows an earlier Unity Editor view of the prototype. The full project is also being tested with Unity's mobile Device Simulator.
