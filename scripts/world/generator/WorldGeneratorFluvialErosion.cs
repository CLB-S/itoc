using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorldGenerator;

// By make some steps parallel, total time is reduced about 10%.
public partial class WorldGenerator
{
    private List<CellData> _streamGraph = new();
    private bool _powerEquationConverged = false;
    private int _iterationCount = 0;
    private Dictionary<int, int> _receivers = new(); // Maps node indices to their receiver node indices
    private Dictionary<int, List<int>> _children = new(); // Maps node indices to their children node indices
    private Dictionary<int, double> _drainageArea = new(); // Maps node indices to their drainage area
    private HashSet<int> _lakes = new();
    private HashSet<int> _riverMouths = new();
    private ConcurrentDictionary<int, int> _lakeIdentifiers = new(); // Maps node indices to lake identifiers

    public IReadOnlyList<CellData> StreamGraph => _streamGraph;
    public IReadOnlyDictionary<int, int> Receivers => _receivers;
    public IReadOnlyDictionary<int, double> DrainageArea => _drainageArea;

    private void FindRiverMouths()
    {
        // ReportProgress("Finding river mouths");

        _riverMouths.Clear();
        foreach (var edge in _voronoiEdges)
        {
            var cellPId = _delaunator.Triangles[edge.Index];
            var cellQId = _delaunator.Triangles[_delaunator.Halfedges[edge.Index]];
            var cellP = _cellDatas[cellPId];
            var cellQ = _cellDatas[cellQId];

            if (cellP.PlateType == PlateType.Continent && cellQ.PlateType == PlateType.Oceans)
            {
                if (((Rect2)Settings.Bounds).HasPoint(SamplePoints[cellPId]))
                {
                    cellP.IsRiverMouth = true;
                    cellP.Receiver = cellQ;
                    _riverMouths.Add(cellP.Index);
                }
            }
            else if (cellP.PlateType == PlateType.Oceans && cellQ.PlateType == PlateType.Continent)
            {
                if (((Rect2)Settings.Bounds).HasPoint(SamplePoints[cellQId]))
                {
                    cellQ.IsRiverMouth = true;
                    cellQ.Receiver = cellP;
                    _riverMouths.Add(cellQ.Index);
                }
            }
        }
    }

    private void ComputeStreamTrees()
    {
        // ReportProgress("Computing stream trees");

        if (_streamGraph.Count == 0) return;

        // Reset the receivers and children maps
        _receivers.Clear();
        _children.Clear();
        _lakes.Clear();

        // For each node, find its receiver (neighbor with lowest elevation)
        var receiverPairs = new ConcurrentBag<(int CellIndex, int ReceiverIndex)>();
        var lakes = new ConcurrentBag<int>();

        Parallel.ForEach(_streamGraph, cell =>
        {
            var lowestNeighbor = GetLowestNeighbor(cell);
            if (lowestNeighbor != cell)
            {
                receiverPairs.Add((cell.Index, lowestNeighbor.Index));
            }
            else
            {
                lakes.Add(cell.Index);
            }
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

        foreach (var lakeIndex in lakes)
        {
            _lakes.Add(lakeIndex);
        }
    }

    private CellData GetLowestNeighbor(CellData cell)
    {
        if (cell.IsRiverMouth)
            return cell; // River mouths have no neighbors

        CellData lowest = null;
        double lowestElevation = double.MaxValue;

        foreach (var neighbor in GetNeighborCells(cell))
        {
            if (neighbor.Height < lowestElevation)
            {
                lowest = neighbor;
                lowestElevation = neighbor.Height;
            }
        }

        return lowest.Height < cell.Height ? lowest : cell;
    }

    private void IdentifyLakes()
    {
        // ReportProgress("Identifying lakes");

        _lakeIdentifiers.Clear();
        // Assign lake identifiers to all nodes in each lake's drainage area
        Parallel.ForEach(_lakes, lakeIndex =>
        {
            AssignLakeIdentifiers(lakeIndex, lakeIndex, _lakeIdentifiers);
        });
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
            int currentNode = queue.Dequeue();
            lakeIdentifiers[currentNode] = lakeId;

            // Process all children of the current node
            if (_children.TryGetValue(currentNode, out var childrenList))
            {
                foreach (var childIndex in childrenList)
                {
                    // Only process unvisited children to avoid cycles
                    if (!visited.Contains(childIndex))
                    {
                        queue.Enqueue(childIndex);
                        visited.Add(childIndex);
                    }
                }
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
        var lakeOutflowGraph = new Dictionary<int, Dictionary<int, (int sourceNode, int targetNode, double passHeight)>>();

        // For each cell in a lake
        foreach (var cell in _streamGraph)
        {
            if (!_lakeIdentifiers.TryGetValue(cell.Index, out var sourceLakeId))
                continue; // Skip if the cell is not in a lake

            if (cell.IsRiverMouth)
                continue; // Skip river mouths

            foreach (var neighbor in GetNeighborCells(cell))
            {
                if (!_lakeIdentifiers.TryGetValue(neighbor.Index, out var targetLakeId))
                    continue; // Skip if the neighbor is not in a lake

                if (targetLakeId == sourceLakeId) continue;

                if (!lakeOutflowGraph.ContainsKey(sourceLakeId))
                    lakeOutflowGraph[sourceLakeId] = new Dictionary<int, (int, int, double)>();

                // Calculate pass height (maximum height of the two connecting nodes)
                double passHeight = Mathf.Max(cell.Height, neighbor.Height);

                // Update the pass height if this one is lower
                if (lakeOutflowGraph[sourceLakeId].ContainsKey(targetLakeId))
                {
                    var existingPass = lakeOutflowGraph[sourceLakeId][targetLakeId];
                    if (passHeight < existingPass.passHeight)
                    {
                        lakeOutflowGraph[sourceLakeId][targetLakeId] = (cell.Index, neighbor.Index, passHeight);
                    }
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
        foreach (var lakeId in _riverMouths)
        {
            lakeTrees[lakeId] = (-1, -1); // -1 indicates a root lake (no receiver)
        }

        // Create a list of all lake connections sorted by pass height
        var sortedConnections = new List<(int sourceLake, int targetLake, int sourceNode, int targetNode, double passHeight)>();

        foreach (var (sourceLakeId, outflows) in lakeOutflowGraph)
        {
            foreach (var (targetLakeId, (sourceNode, targetNode, passHeight)) in outflows)
            {
                sortedConnections.Add((sourceLakeId, targetLakeId, sourceNode, targetNode, passHeight));
            }
        }

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

    private void ComputeDrainageAndSlopes()
    {
        // ReportProgress("Computing drainage and slopes");

        // Reset the drainage area map
        _drainageArea.Clear();

        // Initialize the drainage area for each node with its own area
        foreach (var cell in _streamGraph)
        {
            // Use Voronoi cell area for each cell (approximation)
            // The area represents how much rain this cell directly receives
            var nodeArea = cell.Area;
            _drainageArea[cell.Index] = nodeArea;
        }

        // Collect all river mouths (roots)
        var riverMouths = _streamGraph.Where(c => c.IsRiverMouth).ToList();

        // Perform post-order traversal starting from each river mouth to collect nodes in processing order
        var postOrderList = new List<int>();
        var visited = new HashSet<int>();

        foreach (var riverMouth in riverMouths)
        {
            var stack = new Stack<int>();
            stack.Push(riverMouth.Index);

            while (stack.Count > 0)
            {
                int currentIndex = stack.Pop();

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
                    {
                        foreach (var childIndex in children)
                        {
                            stack.Push(childIndex);
                        }
                    }
                }
            }
        }

        // Process each node in post-order to compute drainage area and slopes
        foreach (var index in postOrderList)
        {
            // Accumulate drainage area from children
            if (_children.TryGetValue(index, out var children))
            {
                foreach (var childIndex in children)
                {
                    _drainageArea[index] += _drainageArea[childIndex];
                }
            }

            // Compute slope for this node if it has a receiver
            if (_receivers.TryGetValue(index, out int receiverIndex))
            {
                var cell = _cellDatas[index];
                var receiver = _cellDatas[receiverIndex];
                var distance = UniformDistance(_points[index], _points[receiverIndex]);

                if (distance < 0.001f)
                {
                    // Avoid division by zero
                    cell.Slope = 0.0;
                }
                else
                {
                    cell.Slope = (cell.Height - receiver.Height) / distance;
                }
            }
        }
    }

    private void SolvePowerEquation()
    {
        // ReportProgress("Solving stream power equation");

        // Parameters for the stream power equation
        double k = Settings.ErosionRate; // Erodibility coefficient
        double m = 0.5; // Drainage area exponent (typically 0.5)
        double dt = Settings.ErosionTimeStep; // Time step
        double maxChange = 0.0; // Track maximum height change for convergence check

        // Sort nodes from downstream to upstream to ensure proper calculation order
        var sortedNodes = new List<CellData>();
        var visited = new HashSet<int>();

        // Start from river mouths (outflow points)
        var queue = new Queue<CellData>();
        foreach (var cell in _streamGraph.Where(c => c.IsRiverMouth))
        {
            queue.Enqueue(cell);
            visited.Add(cell.Index);
        }

        // Breadth-first traversal from downstream to upstream
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sortedNodes.Add(current);

            // Process all cells that flow to this cell
            if (_children.TryGetValue(current.Index, out var children))
            {
                foreach (var childIndex in children)
                {
                    if (!visited.Contains(childIndex))
                    {
                        visited.Add(childIndex);
                        queue.Enqueue(_cellDatas[childIndex]);
                    }
                }
            }
        }

        // Process nodes from downstream to upstream (river mouths to sources)
        foreach (var cell in sortedNodes)
        {
            // If this is a river mouth or has no receiver, skip it
            if (cell.IsRiverMouth || !_receivers.TryGetValue(cell.Index, out var receiverIndex))
                continue;

            var receiver = _cellDatas[receiverIndex];

            // Calculate the slope between this node and its receiver
            var distance = UniformDistance(_points[cell.Index], _points[receiverIndex]);

            // Skip if distance is zero to avoid division by zero
            if (distance < 0.001f) continue;

            // Get the drainage area for this node
            double drainageArea = _drainageArea.GetValueOrDefault(cell.Index, _cellArea);

            // Apply uplift
            var uplift = cell.Uplift > 0.01f ? cell.Uplift : 0.01;

            // Calculate the term for the stream power equation
            var erosionTerm = k * Mathf.Pow(drainageArea, m) / distance;

            // Calculate the new height using the implicit scheme from the paper
            var oldHeight = cell.Height;
            var newHeight = (oldHeight + dt * (uplift + erosionTerm * receiver.Height)) / (1 + erosionTerm * dt);

            // Apply thermal erosion correction: limit the maximum slope
            var maxSlopeHeight = receiver.Height + distance * Mathf.Tan(Mathf.DegToRad(30.0f));
            if (newHeight > maxSlopeHeight)
            {
                newHeight = maxSlopeHeight;
            }

            // Update the height
            cell.Height = (double)newHeight;

            // Track the maximum change for convergence check
            maxChange = Mathf.Max(maxChange, Mathf.Abs(newHeight - oldHeight));
        }

        // Check for convergence
        _powerEquationConverged = maxChange < Settings.ErosionConvergenceThreshold ||
                                   ++_iterationCount >= Settings.MaxErosionIterations;

        ReportProgress($"[{_iterationCount}/{Settings.MaxErosionIterations}] Stream power equation solved. Max height change: {maxChange:f4}. Converged: {_powerEquationConverged}");
    }
}
