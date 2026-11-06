using System.Globalization;

// A position uses row first, column second. Both are zero-based.
public readonly record struct Cell(int Row, int Column);

// Shared by Dijkstra and the student algorithm. Null Path means no route exists.
// Expansions counts non-stale queue removals, INCLUDING the goal. A repeated
// expansion counts again; an obsolete queue entry does not count.
public sealed record SearchResult(List<Cell>? Path, long Expansions);

public sealed class Grid
{
    private readonly int[,] terrain; // 0 = obstacle; 1..9 = entering cost multiplier
    public int Rows => terrain.GetLength(0);
    public int Columns => terrain.GetLength(1);
    public Cell Start { get; }
    public Cell Goal { get; }

    // Keep this order fixed: it influences which equally good path is selected.
    private static readonly (int dr, int dc)[] Directions =
    {
        (-1, 0), (0, 1), (1, 0), (0, -1),
        (-1, 1), (1, 1), (1, -1), (-1, -1)
    };

    public Grid(int[,] terrain, Cell start, Cell goal)
    {
        if (terrain.Length == 0) throw new ArgumentException("Grid cannot be empty.");
        this.terrain = (int[,])terrain.Clone();
        foreach (int value in terrain)
            if (value < 0 || value > 9) throw new ArgumentException("Terrain must be 0..9.");
        Start = start;
        Goal = goal;
        if (!IsOpen(start) || !IsOpen(goal))
            throw new ArgumentException("Start and goal must be in bounds and traversable.");
    }

    public bool IsOpen(Cell cell) =>
        cell.Row >= 0 && cell.Row < Rows && cell.Column >= 0 && cell.Column < Columns
        && terrain[cell.Row, cell.Column] != 0;

    public bool CanMove(Cell from, Cell to)
    {
        if (!IsOpen(from) || !IsOpen(to)) return false;
        int dr = to.Row - from.Row;
        int dc = to.Column - from.Column;
        if (Math.Abs(dr) > 1 || Math.Abs(dc) > 1 || (dr == 0 && dc == 0)) return false;
        // A diagonal cannot squeeze past either adjoining blocked cell.
        if (dr != 0 && dc != 0)
            return IsOpen(new Cell(from.Row + dr, from.Column))
                && IsOpen(new Cell(from.Row, from.Column + dc));
        return true;
    }

    public IEnumerable<Cell> Neighbours(Cell from)
    {
        foreach (var (dr, dc) in Directions)
        {
            var to = new Cell(from.Row + dr, from.Column + dc);
            if (CanMove(from, to)) yield return to;
        }
    }

    public double MoveCost(Cell from, Cell to)
    {
        if (!CanMove(from, to)) throw new ArgumentException("Illegal grid move.");
        bool diagonal = from.Row != to.Row && from.Column != to.Column;
        return terrain[to.Row, to.Column] * (diagonal ? Math.Sqrt(2) : 1);
    }

    public double Euclidean(Cell cell)
    {
        double dr = Goal.Row - cell.Row;
        double dc = Goal.Column - cell.Column;
        return Math.Sqrt(dr * dr + dc * dc);
    }

    // Reconstruct from the goal by following parents, then reverse the list.
    public List<Cell> Reconstruct(Cell?[,] parent)
    {
        var path = new List<Cell>();
        Cell current = Goal;
        while (true)
        {
            path.Add(current);
            if (current == Start) break;
            if (path.Count > terrain.Length || parent[current.Row, current.Column] is null)
                throw new InvalidOperationException("Broken or cyclic parent chain.");
            current = parent[current.Row, current.Column]!.Value;
        }
        path.Reverse();
        return path;
    }

    // Independently checks the returned path. Called OUTSIDE the timed search.
    // Include both endpoints once; start==goal is a one-cell path costing zero.
    public double ValidateAndCost(IReadOnlyList<Cell> path)
    {
        if (path.Count == 0 || path[0] != Start || path[^1] != Goal)
            throw new InvalidOperationException("Path must run from start to goal.");
        var seen = new HashSet<Cell>();
        double cost = 0;
        for (int i = 0; i < path.Count; i++)
        {
            if (!IsOpen(path[i]) || !seen.Add(path[i]))
                throw new InvalidOperationException("Path contains a blocked/repeated cell.");
            if (i > 0) cost += MoveCost(path[i - 1], path[i]);
        }
        return cost;
    }

    public static Grid Load(string file)
    {
        // Comments begin with ';'. Blank lines are ignored. No spaces in map rows.
        var lines = File.ReadAllLines(file).Select(s => s.Trim())
            .Where(s => s.Length > 0 && !s.StartsWith(';')).ToArray();
        if (lines.Length < 4 || lines[0] != "GRID 1")
            throw new FormatException("Expected GRID 1, START, GOAL, then map rows.");
        Cell ReadCell(string line, string label)
        {
            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3 || parts[0] != label)
                throw new FormatException($"Expected {label} row column.");
            return new Cell(int.Parse(parts[1], CultureInfo.InvariantCulture),
                int.Parse(parts[2], CultureInfo.InvariantCulture));
        }
        Cell start = ReadCell(lines[1], "START");
        Cell goal = ReadCell(lines[2], "GOAL");
        int rows = lines.Length - 3;
        int columns = lines[3].Length;
        var terrain = new int[rows, columns];
        for (int r = 0; r < rows; r++)
        {
            if (lines[r + 3].Length != columns) throw new FormatException("Unequal row widths.");
            for (int c = 0; c < columns; c++)
                terrain[r, c] = lines[r + 3][c] switch
                {
                    '#' => 0,
                    '.' => 1,
                    >= '1' and <= '9' => lines[r + 3][c] - '0',
                    _ => throw new FormatException("Use #, . or digits 1..9 in map rows.")
                };
        }
        return new Grid(terrain, start, goal);
    }

    public void Save(string file, IEnumerable<string> comments)
    {
        // Never overwrite an existing experimental input by accident.
        using var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write);
        using var writer = new StreamWriter(stream);
        writer.WriteLine("GRID 1");
        foreach (string comment in comments) writer.WriteLine("; " + comment);
        writer.WriteLine($"START {Start.Row} {Start.Column}");
        writer.WriteLine($"GOAL {Goal.Row} {Goal.Column}");
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
                writer.Write(terrain[r, c] == 0 ? '#' : (char)('0' + terrain[r, c]));
            writer.WriteLine();
        }
    }
}
