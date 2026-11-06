# Pathfinding Experiment (C#)

A console starter for the Science of Computing & Digital Media unit (UI110010). Investigate **search effort versus path quality** using fixed weighted grids. Dijkstra is supplied; students implement Euclidean A* and extend it with heuristic weighting. The focus is experimental evidence and analysis.

## Run the project

Extract the entire ZIP. In Visual Studio, open `PathfindingStarter.csproj` and press **Ctrl+F5**. In a terminal, open the project folder and run `dotnet run -c Release`. Both routes open the same menu. The project requires the .NET 8 SDK.

| Choice | Action |
|---|---|
| 1 | Run Dijkstra on the default example, plus any student algorithms you have enabled |
| 2 | Choose a folder, then a single grid by number |
| 3 | Choose a folder and run every grid in it, including subfolders |
| 4 | Generate a batch using the settings in `Program.cs` |
| 0 | Exit, or return to the main menu from a selection list |

The app displays its project folder on startup. It finds the folder containing `PathfindingStarter.csproj` by searching upward from the executable, then from the launch directory if needed. Keep `Data` beside that project file. The menu reads those grids directly and writes results to the same project's `Results/results.csv`, including when your IDE launches from `bin/Debug/net8.0`.

## What is supplied

| File | Purpose |
|---|---|
| `Program.cs` | Experiment and generator settings, repeated runs, validation, timing and CSV |
| `Grid.cs` | File loading, movement, costs, Euclidean estimate, path reconstruction and validation |
| `Dijkstra.cs` | Working exact baseline, with comments explaining the search |
| `StudentAlgorithms.cs` | One clearly marked implementation point for A* and weighted A* |
| `GridGenerator.cs` | Batch generation, saved settings, reachability checks and SVG previews |

`Data/Examples` contains five small validation environments. `Data/SampleBatch` contains a generated batch to inspect and use immediately. These are starting points, not a prescribed experimental dataset. See `Data/README.md` for the format and examples.

## Make your own environments

Edit the `generation` settings near the top of `Program.cs`, restart the application, and choose **4**. The generator combines every selected **size × obstacle probability × pattern × seed**. Defaults produce 36 grids. Each menu run uses a fresh folder under `Data/Generated`. Choose **2** or **3** afterwards to run the new grids in the same session.

| Setting | What you control |
|---|---|
| `Sizes` | One or more `(rows, columns)` pairs |
| `ObstacleDensities` | Background obstacle probabilities, from 0 to 1 |
| `Patterns` | `random`, `wall`, `terrain-band`, or a selection of these |
| `GridsPerCombination`, `FirstSeed` | Number of instances per combination and starting seed |
| `MinimumCost`, `MaximumCost` | Random background terrain multipliers, inclusive, from 1 to 9 |
| `GapRows` | Bottom rows left without the added wall or terrain band |
| `BandWidth`, `BandCost` | Width and terrain multiplier of the expensive band |
| `Start`, `Goal` | Explicit `new Cell(row, column)`, or `null` for opposite corners |

A `random` grid has only the chosen random background. With probability 0 and costs 1..1, it is open uniform terrain. A `wall` adds a central vertical wall with an opening at the bottom. A `terrain-band` adds a central expensive strip, leaving a route around its bottom. Background obstacles remain in place, including in the gap. These patterns do **not** guarantee a particular search result.

Each file records its settings in comments. The batch also contains:

- An SVG preview per grid: pale cells are cheap, orange cells expensive, dark cells blocked; green marks start and pink marks goal. Open in a browser. If start equals goal, pink covers green.
- `manifest.csv`: settings, actual obstacle fraction, reachability and the supplied Dijkstra cost.

Start and goal are forced open at the minimum terrain cost. **Unreachable grids are saved and labelled**, not silently repaired or replaced. Use reachable grids for quality comparisons; record your selection rule and keep no-path results separate. Requested obstacle probability differs from the realised fraction, especially after adding walls.

The actual saved `.grid` file is your repeatable input. Retain it: a seed alone is not a portable guarantee across generator or .NET versions. Generation, previews and reachability checks are not part of timed experiments. The same seed is reused across combinations deliberately; related variants are not independent samples.

Choose environments to test an idea. For example, compare a cheap detour with an expensive direct route, or place start and goal on opposite sides of a wall near its closed end. You can edit saved files to build specific cases; save edits under new names and update their provenance comments. More grids do not automatically make a stronger investigation.

## Where to add your algorithm

Open `StudentAlgorithms.cs`:

```csharp
public static SearchResult AStar(Grid grid, double weight)
```

The method contains a detailed implementation checklist. You may copy and adapt the supplied Dijkstra search: the essential change is queue priority, from accumulated cost to **accumulated cost + weight × Euclidean estimate**. Keep accumulated cost separately from priority.

1. Implement and test ordinary A* using `weight = 1`.
2. Set `runAStar = true` in `Program.cs`. The runner compares its path cost with Dijkstra.
3. Use `weight` in both the initial and neighbour priorities. Set `runWeightedAStar = true` and choose the `weights` array. No new algorithm registration is needed.

Return `new SearchResult(path, expansions)`. A path contains both endpoints; `grid.Reconstruct(parent)` builds it for you. Return `new SearchResult(null, expansions)` if the queue empties without reaching the goal. Helpers already handle neighbours, corner rules and move costs.

Use the baseline's queue tie rule and expansion counter. Skip stale entries **before** counting or checking the goal. Stop when the goal is removed as a current entry, not when first discovered. Allow a previously expanded cell back into the queue when its accumulated cost improves; a permanent visited set can prevent this.

For help understanding the process, read the support tutorials alongside `Dijkstra.cs`. If your implementation is incomplete, document your attempt, checks and observations; do not present unvalidated results as successful searches.

## Movement rules

Cells use zero-based `(row, column)` coordinates. Movement is cardinal or diagonal. Entering a cell costs its terrain multiplier times **1** for cardinal movement or **√2** for diagonal movement. Both orthogonal cells beside a diagonal must be open. The start costs zero initially, but retains its ordinary terrain value.

Terrain multipliers are at least 1, making the supplied Euclidean estimate an admissible base heuristic. With the documented queue/reopening behaviour, ordinary A* should match Dijkstra's cost and weighted A* should return a cost no greater than `weight × optimum`. More weight does not guarantee fewer expansions or a worse path on every instance.

## Measurements

Each algorithm/configuration receives an untimed warm-up and the selected number of timed trials. There is also an untimed Dijkstra reference search. Timing includes search setup and path reconstruction, but excludes loading, validation, final path costing, console output and CSV writing.

**NodesExpanded** means non-stale queue removals, including the start and goal. Re-expansions count again; obsolete queue entries do not. This convention applies to every algorithm.

Results append to `Results/results.csv`, recording the dataset, endpoints, algorithm, weight, trial, reachability, expansions, steps, path cost, reference cost, percentage gap, bound and milliseconds. Ordinary A* uses weight 1; Dijkstra has no weight. Blank quality fields mean not applicable, including percentage gaps when the optimal cost is zero. Gaps within floating-point tolerance below zero are displayed as zero.

The runner validates returned routes and checks costs against the baseline with a small numerical tolerance. A failed check stops the run before writing that trial; earlier valid rows remain. Passing these checks supports correctness but does not prove an implementation correct on all inputs. Independently check the small examples first.

Repeated timings measure runtime variation. Repeating a deterministic search on the same file does not create new environments. Keep your code version and experimental settings with the CSV, and choose a new results filename when changing implementations to avoid mixing experiments accidentally.
