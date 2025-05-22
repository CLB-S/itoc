using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace ITOC.WorldGeneration;

public class FluvialEroderSettings
{
    public double ErosionRate { get; set; } = 0.01;
    public double ErosionTimeStep { get; set; } = 0.1;
    public double ErosionConvergenceThreshold { get; set; } = 0.001;
    public int MaxErosionIterations { get; set; } = 1000;
    public double MaxErosionSlopeAngle { get; set; } = 45.0;
    public double DefaultCellArea { get; set; } = 1000.0;
}

// By make some steps parallel, total time is reduced about 10%.
public class FluvialEroder(
    Dictionary<int, CellData> cellDatas,
    HashSet<int> riverMouths,
    FluvialEroderSettings settings,
    Func<CellData, IEnumerable<CellData>> getNeighborCellsFunc,
    Func<Vector2, Vector2, double> distanceFunc = null)
{
    private int _iterationCount;
    private readonly Dictionary<int, int> _receivers = new(); // Maps node indices to their receiver node indices
    private readonly Dictionary<int, List<int>> _children = new(); // Maps node indices to their children node indices
    private readonly Dictionary<int, double> _drainageAreas = new(); // Maps node indices to their drainage area
    private readonly Dictionary<int, double> _drainages = new(); // Maps node indices to their drainage area
    private readonly HashSet<int> _lakes = new();
    private readonly ConcurrentDictionary<int, int> _lakeIdentifiers = new(); // Maps node indices to lake identifiers

    private bool _powerEquationConverged = false;

    public double MaxHeight { get; private set; }
    public IReadOnlySet<int> Lakes => _lakes;
    public IReadOnlyDictionary<int, int> Receivers => _receivers;
    public IReadOnlyDictionary<int, double> Drainages => _drainages;

    public event Action<string> OnProgressReport;
    private void ReportProgress(string message)
    {
        OnProgressReport?.Invoke(message);
    }

    private void PrepareStreamGraph()
    {
        ComputeStreamTrees();
        IdentifyLakes();
        ProcessLakeOverflow();
        ComputeDrainages();
    }

    public void Erode()
    {
        // Solve the stream power equation iteratively until convergence
        while (!_powerEquationConverged)
        {
            PrepareStreamGraph();
            SolvePowerEquation();
        }
    }

    private void ComputeStreamTrees()
    {
        // ReportProgress("Computing stream trees");

        // Reset the receivers and children maps
        _receivers.Clear();
        _children.Clear();
        _lakes.Clear();

        // For each node, find its receiver (neighbor with lowest elevation)
        var receiverPairs = new ConcurrentBag<(int CellIndex, int ReceiverIndex)>();
        var lakes = new ConcurrentBag<int>();

        Parallel.ForEach(cellDatas.Values, cell =>
        {
            if (cell.PlateType != PlateType.Continent)
                return; // Skip non-continental cells

            var lowestNeighbor = GetLowestNeighbor(cell);
            if (lowestNeighbor != cell)
                receiverPairs.Add((cell.Index, lowestNeighbor.Index));
            else
                lakes.Add(cell.Index);
        });

        // Update _receivers and _children sequentially
        foreach (var pair in receiverPairs)
        {
            _receivers[pair.CellIndex] = pair.ReceiverIndex;
            if (!_children.TryGetValue(pair.ReceiverIndex, out var childrenList))
            {
                childrenList = new List<int>();
                _children[pair.ReceiverIndex] = childrenList;
            }

            childrenList.Add(pair.CellIndex);
        }

        foreach (var lakeIndex in lakes) _lakes.Add(lakeIndex);
    }

    private CellData GetLowestNeighbor(CellData cell)
    {
        if (cell.IsRiverMouth)
            return cell; // River mouths have no neighbors

        CellData lowest = null;
        var lowestElevation = double.MaxValue;

        foreach (var neighbor in getNeighborCellsFunc(cell))
            if (neighbor.Height < lowestElevation)
            {
                lowest = neighbor;
                lowestElevation = neighbor.Height;
            }

        return lowest.Height < cell.Height ? lowest : cell;
    }

    private void IdentifyLakes()
    {
        _lakeIdentifiers.Clear();
        // Assign lake identifiers to all nodes in each lake's drainage area
        Parallel.ForEach(_lakes, lakeIndex => AssignLakeIdentifiers(lakeIndex, lakeIndex, _lakeIdentifiers));
    }


    private void AssignLakeIdentifiers(int nodeIndex, int lakeId, ConcurrentDictionary<int, int> lakeIdentifiers)
    {
        // Use a queue for breadth-first traversal to avoid stack overflow
        var queue = new Queue<int>();
        var visited = new HashSet<int>();

        queue.Enqueue(nodeIndex);
        visited.Add(nodeIndex);

        while (queue.Count > 0)
        {
            var currentNode = queue.Dequeue();
            lakeIdentifiers[currentNode] = lakeId;

            // Process all children of the current node
            if (_children.TryGetValue(currentNode, out var childrenList))
                foreach (var childIndex in childrenList)
                    // Only process unvisited children to avoid cycles
                    if (!visited.Contains(childIndex))
                    {
                        queue.Enqueue(childIndex);
                        visited.Add(childIndex);
                    }
        }
    }


    private void ProcessLakeOverflow()
    {
        // ReportProgress("Processing lake overflow");

        // Exit if there are no lakes to process
        if (_lakes.Count == 0)
        {
            ReportProgress("No lakes to process");
            return;
        }

        // ReportProgress($"Found {_lakes.Count} lakes.");

        // All outflows of a lake. Dictionary<int sourceLakeId, Dictionary<int targetLakeId, (int sourceNode, int targetNode, double passHeight)>>
        var lakeOutflowGraph =
            new Dictionary<int, Dictionary<int, (int sourceNode, int targetNode, double passHeight)>>();

        // For each cell in a lake
        foreach (var cell in cellDatas.Values)
        {
            if (cell.PlateType != PlateType.Continent)
                continue; // Skip non-continental cells

            if (!_lakeIdentifiers.TryGetValue(cell.Index, out var sourceLakeId))
                continue; // Skip if the cell is not in a lake

            if (cell.IsRiverMouth)
                continue; // Skip river mouths

            foreach (var neighbor in getNeighborCellsFunc(cell))
            {
                if (!_lakeIdentifiers.TryGetValue(neighbor.Index, out var targetLakeId))
                    continue; // Skip if the neighbor is not in a lake

                if (targetLakeId == sourceLakeId) continue;

                if (!lakeOutflowGraph.ContainsKey(sourceLakeId))
                    lakeOutflowGraph[sourceLakeId] = new Dictionary<int, (int, int, double)>();

                // Calculate pass height (maximum height of the two connecting nodes)
                var passHeight = Mathf.Max(cell.Height, neighbor.Height);

                // Update the pass height if this one is lower
                if (lakeOutflowGraph[sourceLakeId].TryGetValue(targetLakeId, out var existingPass))
                {
                    if (passHeight < existingPass.passHeight)
                        lakeOutflowGraph[sourceLakeId][targetLakeId] = (cell.Index, neighbor.Index, passHeight);
                }
                else
                {
                    lakeOutflowGraph[sourceLakeId][targetLakeId] = (cell.Index, neighbor.Index, passHeight);
                }
            }
        }

        // ReportProgress($"Found {lakeOutflowGraph.Count} passes between lakes");

        // Extraction of the lake connections
        var lakeTrees = new Dictionary<int, (int targetLake, int targetNode)>(); // Maps lake ID to its receiver lake ID

        // Identify all unique lake IDs
        foreach (var lakeId in riverMouths) lakeTrees[lakeId] = (-1, -1); // -1 indicates a root lake (no receiver)

        // Create a list of all lake connections sorted by pass height
        var sortedConnections =
            new List<(int sourceLake, int targetLake, int sourceNode, int targetNode, double passHeight)>();

        foreach (var (sourceLakeId, outflows) in lakeOutflowGraph)
            foreach (var (targetLakeId, (sourceNode, targetNode, passHeight)) in outflows)
                sortedConnections.Add((sourceLakeId, targetLakeId, sourceNode, targetNode, passHeight));

        // Sort connections by pass height (ascending)
        sortedConnections.Sort((a, b) => a.passHeight.CompareTo(b.passHeight));

        // Process lakes in order of pass height
        bool madeProgress;
        do
        {
            madeProgress = false;
            foreach (var (sourceLake, targetLake, sourceNode, targetNode, _) in sortedConnections)
            {
                // Skip if this lake already has a receiver
                if (lakeTrees.ContainsKey(sourceLake))
                    continue;

                // Skip if target lake isn't yet in the tree
                if (!lakeTrees.ContainsKey(targetLake))
                    continue;

                // Connect this lake to its target
                lakeTrees[sourceLake] = (targetLake, targetNode);
                madeProgress = true;
            }
        } while (madeProgress);

        foreach (var (sourceLakeId, (_, targetNode)) in lakeTrees)
        {
            if (targetNode == -1)
                continue;

            _receivers[sourceLakeId] = targetNode; // Set the receiver for the lake

            if (!_children.ContainsKey(targetNode))
                _children[targetNode] = new List<int>();

            _children[targetNode].Add(sourceLakeId); // Add the lake as a child of its receiver
        }
    }

    private void ComputeDrainages()
    {
        // ReportProgress("Computing drainage and slopes");

        // Reset the drainage area map
        _drainageAreas.Clear();
        _drainages.Clear();

        // Initialize the drainage area for each node with its own area
        foreach (var cell in cellDatas.Values)
        {
            if (cell.PlateType != PlateType.Continent)
                continue; // Skip non-continental cells

            // Use Voronoi cell area for each cell (approximation)
            // The area represents how much rain this cell directly receives
            _drainageAreas[cell.Index] = cell.Area;
            _drainages[cell.Index] = cell.Area * cell.Precipitation;
        }

        // Perform post-order traversal starting from each river mouth to collect nodes in processing order
        var postOrderList = new List<int>();
        var visited = new HashSet<int>();

        foreach (var riverMouth in riverMouths)
        {
            var stack = new Stack<int>();
            stack.Push(riverMouth);

            while (stack.Count > 0)
            {
                var currentIndex = stack.Pop();

                if (visited.Contains(currentIndex))
                {
                    postOrderList.Add(currentIndex);
                }
                else
                {
                    visited.Add(currentIndex);
                    stack.Push(currentIndex); // Push back as visited

                    // Push children (nodes that flow into currentIndex)
                    if (_children.TryGetValue(currentIndex, out var children))
                        foreach (var childIndex in children)
                            stack.Push(childIndex);
                }
            }
        }

        // Process each node in post-order to compute drainage area and slopes
        foreach (var index in postOrderList)
            // Accumulate drainage area from children
            if (_children.TryGetValue(index, out var children))
                foreach (var childIndex in children)
                {
                    _drainageAreas[index] += _drainageAreas[childIndex];
                    _drainages[index] += _drainages[childIndex];
                }
    }

    private void SolvePowerEquation()
    {
        // ReportProgress("Solving stream power equation");

        // Parameters for the stream power equation
        var k = settings.ErosionRate; // Erodibility coefficient
        var m = 0.5; // Drainage area exponent (typically 0.5)
        var dt = settings.ErosionTimeStep; // Time step
        var maxChange = 0.0; // Track maximum height change for convergence check

        // Sort nodes from downstream to upstream to ensure proper calculation order
        var sortedNodes = new List<CellData>();
        var visited = new HashSet<int>();

        // Start from river mouths (outflow points)
        var queue = new Queue<CellData>();
        foreach (var index in riverMouths)
        {
            queue.Enqueue(cellDatas[index]);
            visited.Add(index);
        }

        // Breadth-first traversal from downstream to upstream
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sortedNodes.Add(current);

            // Process all cells that flow to this cell
            if (_children.TryGetValue(current.Index, out var children))
                foreach (var childIndex in children)
                    if (visited.Add(childIndex)) // Add returns true if the item was not already in the set
                        queue.Enqueue(cellDatas[childIndex]);
        }

        // Process nodes from downstream to upstream (river mouths to sources)
        var totalChange = 0.0;
        foreach (var cell in sortedNodes)
        {
            // If this is a river mouth or has no receiver, skip it
            if (cell.IsRiverMouth || !_receivers.TryGetValue(cell.Index, out var receiverIndex))
                continue;

            var receiver = cellDatas[receiverIndex];

            // Calculate the slope between this node and its receiver
            var distance = distanceFunc == null ? cell.Position.DistanceTo(receiver.Position) :
                distanceFunc(cell.Position, receiver.Position);

            // Skip if distance is zero to avoid division by zero
            if (distance < 0.001f) continue;

            // Get the drainage area for this node
            // Instead of using drainage, drainage area will generate higher mountains at low latitudes.  
            var drainageArea = _drainageAreas.GetValueOrDefault(cell.Index, settings.DefaultCellArea);

            // Apply uplift
            var uplift = cell.Uplift > 0.01f ? cell.Uplift : 0.01;

            // Calculate the term for the stream power equation
            var erosionTerm = k * Mathf.Pow(drainageArea, m) / distance;

            // Calculate the new height using the implicit scheme from the paper
            var oldHeight = cell.Height;
            var newHeight = (oldHeight + dt * (uplift + erosionTerm * receiver.Height)) / (1 + erosionTerm * dt);

            // Apply thermal erosion correction: limit the maximum slope
            var maxSlopeHeight = receiver.Height + distance * Mathf.Tan(Mathf.DegToRad(settings.MaxErosionSlopeAngle));
            if (newHeight > maxSlopeHeight) newHeight = maxSlopeHeight;

            // Update the height
            cell.Height = newHeight;
            if (newHeight > MaxHeight) MaxHeight = newHeight;

            // Track the maximum change for convergence check
            var change = Mathf.Abs(newHeight - oldHeight);
            totalChange += change;
            maxChange = Mathf.Max(maxChange, change);
        }

        // Check for convergence
        _powerEquationConverged = maxChange < settings.ErosionConvergenceThreshold ||
                                  ++_iterationCount >= settings.MaxErosionIterations;

        ReportProgress($"""
                        [{_iterationCount}/{settings.MaxErosionIterations}] Stream power equation solved.
                        Max height change: {maxChange:f4}. Total change: {totalChange:f2}
                        """);

        if (_powerEquationConverged)
            ReportProgress($"Stream power equation converged. Max height {MaxHeight:f2}.");
    }
}