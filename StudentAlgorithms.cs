public static class StudentAlgorithms
{
    // STUDENT IMPLEMENTATION POINT
    // Return the path AND your expansion count. Do not time or print here.
    // weight=1 is ordinary Euclidean A*. weight>1 is weighted A*.
    public static SearchResult AStar(Grid grid, double weight)
    {
        if (!double.IsFinite(weight) || weight < 1)
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be at least 1.");

        // First implement ordinary A*. You may copy Dijkstra.FindPath into this
        // method and adapt it: sharing the bookkeeping is intentional scaffolding.
        // Enable runAStar in Program.cs only once this method returns a result.
        //
        // 1. SET UP YOUR RECORDS
        //    bestCost[r,c] starts at PositiveInfinity, except start = 0.
        //    parent[r,c] starts at null. Use Cell?[,] for these parent pointers.
        //    Use the same queue element and priority types as Dijkstra.
        //    Start sequence and expansions at zero.
        //
        // 2. PUT THE START IN THE QUEUE
        //    Its g cost is zero. Its priority is weight * grid.Euclidean(grid.Start).
        //    Keep g in the queue ENTRY; put g + weight*h in the PRIORITY.
        //    Keep sequence++ as the second priority component for ties.
        //
        // 3. REMOVE THE LOWEST-PRIORITY ENTRY
        //    If its stored g exceeds bestCost for that cell, skip the stale entry.
        //    Otherwise increment expansions, INCLUDING if it is the goal.
        //    If it is the goal, return grid.Reconstruct(parent) and expansions.
        //    Do not stop merely when the goal is first added to the queue.
        //
        // 4. EXAMINE EACH LEGAL NEIGHBOUR
        //    grid.Neighbours(current) already applies obstacles and corner rules.
        //    newG = entry.G + grid.MoveCost(current, neighbour).
        //    Only if newG is strictly LESS than bestCost for that neighbour:
        //      - replace its bestCost with newG;
        //      - record current as its parent;
        //      - calculate h using grid.Euclidean(neighbour);
        //      - enqueue (neighbour, newG), with priority (newG + weight*h, sequence++).
        //    Notice that the heuristic does NOT become part of bestCost.
        //
        // 5. REPEAT UNTIL SUCCESS OR AN EMPTY QUEUE
        //    An empty queue means return new SearchResult(null, expansions).
        //    Do not permanently exclude previously expanded cells. Allow an
        //    improved g to put them back in the queue; weighted A* may need this.
        //
        // Start with weight=1, and compare costs with Dijkstra on small grids.
        // Once correct, using weight in BOTH priority calculations enables WA*.
        // Program.cs handles runs, validation, timing, bounds and CSV output.
        throw new NotImplementedException("Implement AStar in StudentAlgorithms.cs first.");
    }
}
