using System.Diagnostics;
using System.Globalization;

// ---------------- SETTINGS: start here ----------------
// Relative paths use the folder containing PathfindingStarter.csproj.
string inputPath = "Data/Examples/terrain-choice.grid"; // A file OR folder of .grid files
string resultsFile = "Results/results.csv";
int repetitions = 5;
bool runAStar = false;       // Enable after implementing StudentAlgorithms.AStar.
bool runWeightedAStar = false; // Requires runAStar; uses the SAME student method.
double[] weights = { 1.25, 1.5, 2.0, 3.0 }; // Ordinary A* at 1 is run separately.

// All combinations are generated: sizes x densities x patterns x grids per combination.
// Defaults produce 36 files. Choose purposeful values for YOUR investigation.
var generation = new GenerationSettings(
    OutputFolder: "Data/Generated/batch-01", // The menu picks a fresh folder if this one is used.
    Sizes: new[] { (30, 40), (60, 80) },       // (Rows, Columns)
    ObstacleDensities: new[] { 0.0, 0.15 },  // Background probability, not exact fraction.
    Patterns: new[] { "random", "wall", "terrain-band" },
    GridsPerCombination: 3,
    FirstSeed: 100,                        // Each combination uses 100, 101, 102.
    MinimumCost: 1,
    MaximumCost: 1,                        // Uniform background; try 1..5 etc.
    GapRows: 3,                            // Bottom rows left without the added feature.
    BandWidth: 3,
    BandCost: 9,
    Start: null,                           // null means top-left; or new Cell(row, column).
    Goal: null);                           // null means bottom-right.
// ------------------------------------------------------

try
{
    Directory.SetCurrentDirectory(FindProjectFolder());
    Console.WriteLine($"Project folder: {Environment.CurrentDirectory}");

    // Keep command-line runs available for existing experiment scripts.
    if (args.Length > 0)
    {
        if (args is ["generate"]) GridGenerator.Generate(generation);
        else if (args[0] == "run" && args.Length <= 2) RunExperiment(args.Length == 2 ? args[1] : inputPath);
        else throw new ArgumentException("Launch without arguments to open the menu.");
        return;
    }

    while (true)
    {
        Console.WriteLine("\nPathfinding experiment");
        Console.WriteLine("1  Run the default example");
        Console.WriteLine("2  Choose a single grid");
        Console.WriteLine("3  Run a folder of grids");
        Console.WriteLine("4  Generate a batch of grids");
        Console.WriteLine("0  Exit");
        Console.Write("Choice: ");
        string? choice = Console.ReadLine()?.Trim();
        if (choice is null or "0") break;
        try
        {
            switch (choice)
            {
                case "1":
                    RunExperiment(inputPath);
                    break;
                case "2":
                case "3":
                    string? folder = PickGridFolder();
                    if (folder is null) break;
                    string? selected = choice == "3" ? folder
                        : Pick("Choose a grid", Directory.GetFiles(folder, "*.grid"));
                    if (selected is not null) RunExperiment(selected);
                    break;
                case "4":
                    string output = generation.OutputFolder;
                    for (int suffix = 2; Directory.Exists(output) || File.Exists(output); suffix++)
                        output = $"{generation.OutputFolder}-{suffix}";
                    GridGenerator.Generate(generation with { OutputFolder = output });
                    break;
                default:
                    Console.WriteLine("Choose a number from 0 to 4.");
                    break;
            }
        }
        catch (Exception error) when (IsUserError(error))
        {
            Console.WriteLine("Could not complete this action. " + error.Message);
        }
    }
}
catch (Exception error) when (IsUserError(error))
{
    Console.Error.WriteLine("Error: " + error.Message);
    Environment.ExitCode = 1;
}

// IDEs may start inside bin/Debug/net8.0. Prefer the project that owns this
// executable, even if the shell's current directory is in another project.
static string FindProjectFolder()
{
    foreach (string anchor in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
        for (DirectoryInfo? dir = new DirectoryInfo(anchor); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "PathfindingStarter.csproj")))
                return dir.FullName;
    throw new DirectoryNotFoundException("Could not find PathfindingStarter.csproj. Extract the whole project and open its .csproj file.");
}

static string? PickGridFolder()
{
    if (!Directory.Exists("Data")) throw new DirectoryNotFoundException(
        $"Place the supplied Data folder beside PathfindingStarter.csproj in {Environment.CurrentDirectory}.");
    var folders = Directory.EnumerateFiles("Data", "*.grid", SearchOption.AllDirectories)
        .Select(file => Path.GetDirectoryName(file)!).Distinct();
    return Pick("Choose a grid folder", folders);
}

static string? Pick(string prompt, IEnumerable<string> paths)
{
    string[] options = paths.OrderBy(path => path, StringComparer.Ordinal).ToArray();
    if (options.Length == 0)
    {
        Console.WriteLine("No grids found. Generate a batch or add .grid files to Data.");
        return null;
    }
    Console.WriteLine(prompt);
    for (int i = 0; i < options.Length; i++) Console.WriteLine($"{i + 1}  {options[i]}");
    while (true)
    {
        Console.Write("Number (0 to go back): ");
        string? answer = Console.ReadLine()?.Trim();
        if (answer is null or "0") return null;
        if (int.TryParse(answer, out int number) && number >= 1 && number <= options.Length)
            return options[number - 1];
        Console.WriteLine($"Choose a number from 0 to {options.Length}.");
    }
}

static bool IsUserError(Exception error) => error is IOException or UnauthorizedAccessException
    or ArgumentException or FormatException or InvalidOperationException or NotImplementedException or OverflowException;

void RunExperiment(string selectedPath)
{
    if (repetitions < 1) throw new ArgumentException("Use at least one repetition.");
    if (runWeightedAStar && !runAStar)
        throw new ArgumentException("Enable runAStar before runWeightedAStar.");
    if (weights.Any(w => !double.IsFinite(w) || w <= 1))
        throw new ArgumentException("Weighted settings must be finite and greater than 1.");

    string[] files = Directory.Exists(selectedPath)
        ? Directory.GetFiles(selectedPath, "*.grid", SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.Ordinal).ToArray()
        : File.Exists(selectedPath) ? new[] { selectedPath }
        : throw new FileNotFoundException($"Grid file/folder not found: {Path.GetFullPath(selectedPath)}", selectedPath);
    if (files.Length == 0) throw new ArgumentException("No .grid files in the selected folder.");

    const string header = "Dataset,Rows,Columns,StartRow,StartColumn,GoalRow,GoalColumn,Algorithm,Weight,Trial,Status,NodesExpanded,Steps,PathCost,OptimalCost,GapPercent,BoundCost,WithinBound,ElapsedMilliseconds";
    if (File.Exists(resultsFile) && new FileInfo(resultsFile).Length > 0
        && File.ReadLines(resultsFile).First() != header)
        throw new IOException("Existing CSV has a different format. Choose a new results filename.");
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(resultsFile))!);
    bool needsHeader = !File.Exists(resultsFile) || new FileInfo(resultsFile).Length == 0;
    using var csv = new StreamWriter(resultsFile, append: true);
    if (needsHeader) csv.WriteLine(header);

    foreach (string file in files)
    {
        Grid grid = Grid.Load(file); // Loading is never timed.
        Console.WriteLine($"\n{file}: {grid.Rows} rows x {grid.Columns} columns");
        Console.WriteLine($"Start {grid.Start}; goal {grid.Goal}");

        // An untimed reference establishes reachability and the optimal cost.
        SearchResult reference = Dijkstra.FindPath(grid);
        double? optimum = reference.Path is null ? null : grid.ValidateAndCost(reference.Path);
        RunTrials("Dijkstra", null, () => Dijkstra.FindPath(grid));
        if (runAStar) RunTrials("A*", 1, () => StudentAlgorithms.AStar(grid, 1));
        if (runWeightedAStar)
            foreach (double weight in weights.Distinct())
                RunTrials("Weighted A*", weight, () => StudentAlgorithms.AStar(grid, weight));

        void RunTrials(string algorithm, double? weight, Func<SearchResult> search)
        {
            // Warm up every configuration, validate it, but do not record its timing.
            CheckResult(search());
            for (int trial = 1; trial <= repetitions; trial++)
            {
                var timer = Stopwatch.StartNew();
                SearchResult result = search();
                timer.Stop();
                double? cost = CheckResult(result); // Validation outside timing.
                double? gap = optimum > 0 && cost.HasValue
                    ? 100 * Math.Max(0, cost.Value - optimum.Value) / optimum.Value : null;
                double? bound = optimum * (weight ?? 1);
                string status = result.Path is null ? "NO_PATH" : "FOUND";
                string withinBound = cost.HasValue ? "True" : ""; // CheckResult throws if violated.
                csv.WriteLine(string.Join(",", Quote(file), grid.Rows, grid.Columns,
                    grid.Start.Row, grid.Start.Column, grid.Goal.Row, grid.Goal.Column,
                    algorithm, Number(weight), trial, status, result.Expansions,
                    result.Path is null ? "" : (result.Path.Count - 1).ToString(CultureInfo.InvariantCulture),
                    Number(cost), Number(optimum), Number(gap), Number(bound), withinBound,
                    Number(timer.Elapsed.TotalMilliseconds)));
                Console.WriteLine($"  {algorithm} w={Number(weight)} trial {trial}: {status}, " +
                    $"cost={Number(cost)}, expanded={result.Expansions}, ms={timer.Elapsed.TotalMilliseconds:F4}");
                // Small routes are readable; large ones remain in the returned Path list.
                if (trial == 1 && result.Path is { Count: <= 30 })
                    Console.WriteLine("    " + string.Join(" -> ", result.Path.Select(p => $"({p.Row},{p.Column})")));
            }
            csv.Flush();

            double? CheckResult(SearchResult result)
            {
                if (result.Expansions < 1) throw new InvalidOperationException("Count the start expansion.");
                if ((result.Path is null) != (reference.Path is null))
                    throw new InvalidOperationException($"{algorithm}: reachability differs from Dijkstra.");
                if (result.Path is null) return null;
                double cost = grid.ValidateAndCost(result.Path);
                double tolerance = 1e-9 * Math.Max(1, optimum!.Value);
                if (cost < optimum.Value - tolerance)
                    throw new InvalidOperationException("Path cost is below the reference. Check both implementations.");
                double allowed = optimum.Value * (weight ?? 1);
                if (cost > allowed + 1e-9 * Math.Max(1, allowed))
                    throw new InvalidOperationException($"{algorithm}: cost exceeds its expected bound. Check before using results.");
                return cost;
            }
        }
    }
    Console.WriteLine($"\nResults appended to {resultsFile}. A* enabled: {runAStar}; weighted: {runWeightedAStar}.");
}

static string Number(double? value) => value?.ToString("G17", CultureInfo.InvariantCulture) ?? "";
static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
