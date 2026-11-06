public static class Dijkstra
{
    // Supplied exact baseline. No heuristic; the lowest accumulated cost wins.
    // Study this method before implementing A*: almost all bookkeeping is shared.
    public static SearchResult FindPath(Grid grid)
    {
        var bestCost = new double[grid.Rows, grid.Columns];
        var parent = new Cell?[grid.Rows, grid.Columns];
        for (int r = 0; r < grid.Rows; r++)
            for (int c = 0; c < grid.Columns; c++)
                bestCost[r, c] = double.PositiveInfinity;

        // The entry retains its g cost when enqueued. A later improvement can
        // leave an obsolete entry behind. The priority's sequence number makes
        // equal-cost ties deterministic: earlier insertion is processed first.
        var frontier = new PriorityQueue<(Cell Cell, double G), (double Score, long Order)>();
        long sequence = 0;
        long expansions = 0;
        bestCost[grid.Start.Row, grid.Start.Column] = 0;
        frontier.Enqueue((grid.Start, 0), (0, sequence++));

        while (frontier.Count > 0)
        {
            var entry = frontier.Dequeue();
            Cell current = entry.Cell;
            if (entry.G > bestCost[current.Row, current.Column]) continue;

            expansions++; // Includes the goal; excludes obsolete entries.
            if (current == grid.Goal)
                return new SearchResult(grid.Reconstruct(parent), expansions);

            foreach (Cell neighbour in grid.Neighbours(current))
            {
                double newG = entry.G + grid.MoveCost(current, neighbour);
                if (newG < bestCost[neighbour.Row, neighbour.Column])
                {
                    bestCost[neighbour.Row, neighbour.Column] = newG;
                    parent[neighbour.Row, neighbour.Column] = current;
                    frontier.Enqueue((neighbour, newG), (newG, sequence++));
                }
            }
        }
        return new SearchResult(null, expansions);
    }
}
