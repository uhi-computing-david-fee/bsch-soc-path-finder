# Pathfinding grids

Use these files to test the starter, then generate and save environments for your own investigation.

## Small examples

| File in `Examples/` | Purpose |
|---|---|
| `open-diagonal.grid` | Two diagonal moves; optimum `2 × √2` (about 2.828427) |
| `corner-blocked.grid` | No path: diagonal corner cutting is forbidden |
| `terrain-choice.grid` | A costly direct route and a cheaper detour |
| `wall-detour.grid` | A wall forces travel through an opening |
| `start-is-goal.grid` | One-cell path, zero cost; percentage gap is not applicable |

`SampleBatch/` contains 36 generated grids, their settings in `manifest.csv`, and SVG previews. They are teaching inputs, not external benchmarks. View a preview in a browser; use the `.grid` file as the algorithm input.

## Select or generate files

Start the app and choose **1** for the default example, **2** for one grid, or **3** for a folder. Pick `Data/Examples` for small checks or `Data/SampleBatch` for larger experiments.

To make grids, choose **4**. The generator uses the settings in `Program.cs` and saves a fresh batch under `Data/Generated`. Generated folders appear in the menu immediately. Keep the saved files so every algorithm sees the same environment.

## File format

```text
GRID 1
; Optional comment describing this environment
START 0 0
GOAL 2 3
1111
1##1
1231
```

Coordinates are **row then column, starting at zero**. Rows must have equal width and contain no spaces. `#` is an obstacle; `1` to `9` are terrain multipliers; `.` also means 1. Dimensions come from the map. Endpoints must be traversable. Do not put S/G characters in the map.

A move costs the destination multiplier, multiplied by √2 for a diagonal. Diagonals cannot cut past an obstacle on either side. The starting cell is not charged initially.

You may edit a copy of a grid to create a deliberate test. Give it a new filename and explain the change in its comments; old generator settings/optimal costs may no longer describe it. The runner recalculates the baseline rather than trusting comments or the manifest.

Generated batches retain unreachable grids and identify them in the manifest. Use reachable cases for path-quality comparisons, and document any selection or editing. A seed helps explain provenance; the saved file is the authoritative experimental input.
