#!/usr/bin/env python3
"""Repository integrity checks for the public Sugar.io source showcase.

This is NOT a Unity compilation or gameplay test.
"""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
EXPECTED = {
    "AI": {"TornadoAI.cs", "TornadoAIBalance.cs", "TornadoAINavigation.cs", "TornadoAISpawner.cs"},
    "Camera": {"CameraFollow.cs"},
    "Collectibles": {"Candy.cs", "CandyAppearance.cs", "CandySpawner.cs"},
    "Colors": {"KawaiiColors.cs", "KawaiiFloor.cs"},
    "Field": {"PlayingField.cs", "WorldSizeSystem.cs"},
    "Game": {"DisableRuntimeRenderDebugUI.cs", "GameAudioManager.cs", "LevelManager.cs", "MatchBalance.cs", "TornadoMatchManager.cs"},
    "Player": {"PlayerController.cs"},
    "Tornado": {"CandySwirlVFX.cs", "Tornado.cs", "TornadoColors.cs", "TornadoCombat.cs", "TornadoDestructible.cs", "TornadoEnvironmentGravity.cs", "TornadoEnvironmentSuction.cs", "TornadoRuntimeCollider.cs", "TornadoVFX.cs"},
    "UI": {"SugarPopup.cs", "SugarUI.cs"},
}

def check():
    errors = []
    expected_paths = {ROOT / "Scripts" / folder / name for folder, names in EXPECTED.items() for name in names}
    actual_paths = set((ROOT / "Scripts").rglob("*.cs"))
    for path in sorted(expected_paths - actual_paths):
        errors.append(f"Missing source: {path.relative_to(ROOT)}")
    for path in sorted(actual_paths - expected_paths):
        errors.append(f"Unlisted source: {path.relative_to(ROOT)}")
    for path in ROOT.glob("*.cs"):
        errors.append(f"Duplicate/unorganized root-level source: {path.name}")

    for path in sorted(actual_paths):
        content = path.read_text(encoding="utf-8-sig")
        if not content.strip():
            errors.append(f"Empty source file: {path.relative_to(ROOT)}")
        if re.search(r"(?m)^\s*(?:TODO|PLACEHOLDER)\s*$", content):
            errors.append(f"Placeholder marker: {path.relative_to(ROOT)}")

    for name in ("README.md", "docs/ARCHITECTURE.md"):
        if not (ROOT / name).is_file():
            errors.append(f"Missing documentation: {name}")

    if errors:
        for error in errors:
            print("FAIL:", error, file=sys.stderr)
        return 1
    print(f"PASS: {len(expected_paths)} expected C# scripts, documentation, and repository structure.")
    print("NOTE: Unity compilation, runtime behavior, and performance were NOT tested.")
    return 0

if __name__ == "__main__":
    raise SystemExit(check())
