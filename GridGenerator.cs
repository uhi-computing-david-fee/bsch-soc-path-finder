using System.Globalization;
using System.Text;

// A batch combines every size, density and pattern with several different seeds.
// Change the settings in Program.cs; students need not edit generation internals.
public sealed record GenerationSettings(
    string OutputFolder,
    (int Rows, int Columns)[] Sizes,
    double[] ObstacleDensities,
    string[] Patterns,
    int GridsPerCombination,
    int FirstSeed,
    int MinimumCost,
    int MaximumCost,
    int GapRows,
    int BandWidth,
    int BandCost,
    Cell? Start = null,
    Cell? Goal = null);

public static class GridGenerator
{
    public static void Generate(GenerationSettings settings)
    {
        if (settings.Sizes.Length == 0 || settings.ObstacleDensities.Length == 0
            || settings.Patterns.Length == 0 || settings.GridsPerCombination < 1)
            throw new ArgumentException("Choose at least one value for each batch setting.");
        long count = (long)settings.Sizes.Length * settings.ObstacleDensities.Length
            * settings.Patterns.Length * settings.GridsPerCombination;
        if (count > 10000) throw new ArgumentException("Split batches above 10,000 grids.");
        if (settings.MinimumCost < 1 || settings.MaximumCost > 9
            || settings.MinimumCost > settings.MaximumCost || settings.BandCost < 1
            || settings.BandCost > 9 || settings.GapRows < 1 || settings.BandWidth < 1)
            throw new ArgumentException("Costs must be 1..9; gap and band widths must be positive.");
        foreach (double density in settings.ObstacleDensities)
            if (!double.IsFinite(density) || density < 0 || density > 1)
                throw new ArgumentException("Obstacle probabilities must be between 0 and 1.");
        foreach (string pattern in settings.Patterns)
            if (pattern is not ("random" or "wall" or "terrain-band"))
                throw new ArgumentException("Patterns: random, wall, terrain-band.");
        foreach (var (rows, columns) in settings.Sizes)
        {
            if (rows < 3 || columns < 3 || rows > 1000 || columns > 1000
                || settings.GapRows >= rows || settings.BandWidth >= columns - 1)
                throw new ArgumentException("Sizes must be 3..1000; gap/band must fit inside grid.");
            foreach (Cell cell in new[] { settings.Start ?? new Cell(0, 0),
                         settings.Goal ?? new Cell(rows - 1, columns - 1) })
                if (cell.Row < 0 || cell.Row >= rows || cell.Column < 0 || cell.Column >= columns)
                    throw new ArgumentException("Start/goal must fit every selected size.");
        }
        _ = checked(settings.FirstSeed + settings.GridsPerCombination - 1);
        if (Directory.Exists(settings.OutputFolder)
            && Directory.EnumerateFileSystemEntries(settings.OutputFolder).Any())
            throw new IOException("Output folder is not empty. Choose a new batch folder.");
        Directory.CreateDirectory(settings.OutputFolder);
        using var manifest = new StreamWriter(Path.Combine(settings.OutputFolder, "manifest.csv"));
        manifest.WriteLine("File,Rows,Columns,Pattern,Seed,ObstacleProbability,ActualObstacleFraction,MinimumCost,MaximumCost,GapRows,BandWidth,BandCost,Reachable,OptimalCost");
        int index = 0;
        foreach (var (rows, columns) in settings.Sizes)
        foreach (double density in settings.ObstacleDensities)
        foreach (string pattern in settings.Patterns)
        for (int trial = 0; trial < settings.GridsPerCombination; trial++)
        {
            int seed = checked(settings.FirstSeed + trial);
            var random = new Random(seed);
            var terrain = new int[rows, columns];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                {
                    // Always draw both values: changing obstacle probability does
                    // not change the random terrain draws for the same size/seed.
                    double obstacleDraw = random.NextDouble();
                    int cost = random.Next(settings.MinimumCost, settings.MaximumCost + 1);
                    terrain[r, c] = obstacleDraw < density ? 0 : cost;
                }
            // Features are applied on top of the random background. The bottom
            // GapRows rows are left as they were, not cleared or guaranteed open.
            if (pattern == "wall")
                for (int r = 0; r < rows - settings.GapRows; r++)
                    terrain[r, columns / 2] = 0;
            if (pattern == "terrain-band")
            {
                int firstColumn = (columns - settings.BandWidth) / 2;
                for (int r = 0; r < rows - settings.GapRows; r++)
                    for (int c = firstColumn; c < firstColumn + settings.BandWidth; c++)
                        if (terrain[r, c] != 0) terrain[r, c] = settings.BandCost;
            }
            Cell start = settings.Start ?? new Cell(0, 0);
            Cell goal = settings.Goal ?? new Cell(rows - 1, columns - 1);
            // Explicit exception: endpoints are always traversable at minimum cost.
            terrain[start.Row, start.Column] = settings.MinimumCost;
            terrain[goal.Row, goal.Column] = settings.MinimumCost;
            var grid = new Grid(terrain, start, goal);
            var baseline = Dijkstra.FindPath(grid);
            double? optimum = baseline.Path is null ? null : grid.ValidateAndCost(baseline.Path);
            int blocked = terrain.Cast<int>().Count(v => v == 0);
            double actualDensity = (double)blocked / terrain.Length;
            string filename = $"grid-{++index:D4}-{pattern}-{rows}x{columns}-seed{seed}.grid";
            string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
            grid.Save(Path.Combine(settings.OutputFolder, filename), new[]
            {
                "Generated by PathfindingStarter generator v1; System.Random on " + Environment.Version,
                $"pattern={pattern}; seed={seed}; rows={rows}; columns={columns}",
                $"obstacleProbability={F(density)}; actualObstacleFraction={F(actualDensity)}",
                $"backgroundCosts={settings.MinimumCost}..{settings.MaximumCost}; gapRows={settings.GapRows}; bandWidth={settings.BandWidth}; bandCost={settings.BandCost}",
                "Endpoints forced open at minimum cost. No connectivity repair or rejection.",
                $"reachable={baseline.Path is not null}; DijkstraCost={(optimum.HasValue ? F(optimum.Value) : "NA")}",
                "Move cost = destination terrain times 1 or sqrt(2); no corner cutting."
            });
            SavePreview(Path.Combine(settings.OutputFolder, filename + ".svg"), terrain, start, goal);
            manifest.WriteLine(string.Join(",", filename, rows, columns, pattern, seed,
                F(density), F(actualDensity), settings.MinimumCost, settings.MaximumCost,
                settings.GapRows, settings.BandWidth, settings.BandCost,
                baseline.Path is not null, optimum.HasValue ? F(optimum.Value) : ""));
            Console.WriteLine($"Saved {filename}: {(optimum.HasValue ? "reachable" : "NO PATH")}");
        }
        Console.WriteLine($"Saved {index} fixed grids, SVG previews and manifest.csv in {settings.OutputFolder}.");
    }

    // A compact vector preview: equal terrain runs are combined into rectangles.
    // Dark = obstacle, pale = cheap terrain, orange = expensive; green/red = endpoints.
    private static void SavePreview(string file, int[,] terrain, Cell start, Cell goal)
    {
        int rows = terrain.GetLength(0), columns = terrain.GetLength(1);
        string[] colours = { "#222831", "#f4f5f7", "#fff0d6", "#ffe0b2", "#ffcc80",
            "#ffb74d", "#ffa726", "#fb8c00", "#ef6c00", "#d84315" };
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {columns} {rows}\" width=\"800\" height=\"800\" shape-rendering=\"crispEdges\">\n");
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns;)
            {
                int end = c + 1;
                while (end < columns && terrain[r, end] == terrain[r, c]) end++;
                svg.AppendLine($"<rect x=\"{c}\" y=\"{r}\" width=\"{end - c}\" height=\"1\" fill=\"{colours[terrain[r, c]]}\"/>");
                c = end;
            }
        foreach (var (cell, colour) in new[] { (start, "#00c853"), (goal, "#e91e63") })
            svg.AppendLine($"<rect x=\"{cell.Column}\" y=\"{cell.Row}\" width=\"1\" height=\"1\" fill=\"{colour}\"/>");
        svg.AppendLine("</svg>");
        File.WriteAllText(file, svg.ToString());
    }
}
