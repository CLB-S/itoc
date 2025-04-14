using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WorldGenerator;

public partial class WorldGenerator
{
    private List<CellData> _streamGraph = new();
    private bool _powerEquationConverged = false;
    private int _iterationCount = 0;
    private Dictionary<int, int> _receivers = new(); // Maps node indices to their receiver node indices
    private Dictionary<int, List<int>> _children = new(); // Maps node indices to their children node indices
    private Dictionary<int, float> _drainageArea = new(); // Maps node indices to their drainage area
    private HashSet<int> _lakes = new();
    private Dictionary<int, int> _lakeIdentifiers = new(); // Maps node indices to lake identifiers

    public IReadOnlyList<CellData> StreamGraph => _streamGraph;
    public IReadOnlyDictionary<int, int> Receivers => _receivers;
    public IReadOnlyDictionary<int, float> DrainageArea => _drainageArea;

    private void ComputeStreamTrees()
    {
        ReportProgress("Computing stream trees");

        if (_streamGraph.Count == 0) return;

        // Reset the receivers and children maps
        _receivers.Clear();
        _children.Clear();
        _lakes.Clear();

        // For each node, find its receiver (neighbor with lowest elevation)
        foreach (var cell in _streamGraph)
        {
            var lowestNeighbor = GetLowestNeighbor(cell);
            if (!_children.ContainsKey(lowestNeighbor.Index))
                _children[lowestNeighbor.Index] = new List<int>();
            if (lowestNeighbor != cell)
            {
                // Set the receiver for this node
                _receivers[cell.Index] = lowestNeighbor.Index;

                // Add this node as a child of its receiver
                _children[lowestNeighbor.Index].Add(cell.Index);
            }
            else
            {
                _lakes.Add(cell.Index);
            }
        }
    }

    private CellData GetLowestNeighbor(CellData cell)
    {
        if (cell.IsRiverMouth)
            return cell; // River mouths have no neighbors

        CellData lowest = null;
        float lowestElevation = float.MaxValue;

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
        ReportProgress("Identifying lakes");

        _lakeIdentifiers.Clear();
        // Assign lake identifiers to all nodes in each lake's drainage area
        foreach (var lakeIndex in _lakes)
        {
            AssignLakeIdentifiers(lakeIndex, lakeIndex, _lakeIdentifiers);
        }
    }

    private void AssignLakeIdentifiers(int nodeIndex, int lakeId, Dictionary<int, int> lakeIdentifiers)
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
        ReportProgress("Processing lake overflow");

        // Exit if there are no lakes to process
        if (_lakes.Count == 0)
        {
            ReportProgress("No lakes to process");
            return;
        }

        ReportProgress($"Found {_lakes.Count} lakes.");

        // All outflows of a lake. Dictionary<int sourceLakeId, Dictionary<int targetLakeId, (int sourceNode, int targetNode, float passHeight)>>
        var lakeOutflowGraph = new Dictionary<int, Dictionary<int, (int sourceNode, int targetNode, float passHeight)>>();

        // For each cell in a lake
        foreach (var cell in _streamGraph)
        {
            var sourceLakeId = _lakeIdentifiers[cell.Index];

            if (cell.IsRiverMouth)
                continue; // Skip river mouths

            foreach (var neighbor in GetNeighborCells(cell))
            {
                if (!_lakeIdentifiers.TryGetValue(neighbor.Index, out var targetLakeId))
                    continue; // Skip if the neighbor is not a lake

                if (targetLakeId == sourceLakeId) continue;

                if (!lakeOutflowGraph.ContainsKey(sourceLakeId))
                    lakeOutflowGraph[sourceLakeId] = new Dictionary<int, (int, int, float)>();

                // Calculate pass height (maximum height of the two connecting nodes)
                float passHeight = Mathf.Max(cell.Height, neighbor.Height);

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

        ReportProgress($"Found {lakeOutflowGraph.Count} passes between lakes");

        // Extraction of the lake connections
        var lakeTrees = new Dictionary<int, (int targetLake, int targetNode)>(); // Maps lake ID to its receiver lake ID

        // Identify all unique lake IDs
        foreach (var lakeId in _riverMouths)
        {
            lakeTrees[lakeId] = (-1, -1); // -1 indicates a root lake (no receiver)
        }

        while (lakeOutflowGraph.Count > 0)
        {
            var lowestLakeId = -1;
            var lowestLakeTarget = -1;
            var lowestLakeTargetNode = -1;
            var lowestPassHeight = -1.0f;
            foreach (var (sourceLakeId, outflows) in lakeOutflowGraph)
            {
                foreach (var (targetLakeId, (sourceNode, targetNode, passHeight)) in outflows)
                {
                    if (lakeTrees.ContainsKey(targetLakeId))
                    {
                        if (lowestLakeId == -1 || passHeight < lowestPassHeight)
                        {
                            lowestLakeId = sourceLakeId;
                            lowestLakeTarget = targetLakeId;
                            lowestLakeTargetNode = targetNode;
                            lowestPassHeight = passHeight;
                        }
                    }
                }
            }

            if (lowestLakeId != -1)
            {
                lakeTrees[lowestLakeId] = (lowestLakeTarget, lowestLakeTargetNode);
                lakeOutflowGraph.Remove(lowestLakeId);
            }
            else
            {
                ReportProgress("No more lake connections found");
                break;
            }
        }

        foreach (var (sourceLakeId, (_, targetNode)) in lakeTrees)
        {
            _receivers[sourceLakeId] = targetNode; // Set the receiver for the lake
            if (targetNode == -1)
                continue;
            _children[targetNode].Add(sourceLakeId); // Add the lake as a child of its receiver
        }
    }

    private void ComputeDrainageAndSlopes()
    {
        ReportProgress("Computing drainage and slopes");

        // Reset the drainage area map
        _drainageArea.Clear();

        // Initialize the drainage area for each node with its own area
        foreach (var cell in _streamGraph)
        {
            // Use Voronoi cell area for each cell (approximation)
            // The area represents how much rain this cell directly receives
            var nodeArea = _cellArea; // Default to _cellArea
            _drainageArea[cell.Index] = nodeArea;
        }

        // Store cells in breadth-first traversal order (from leaves to roots)
        var traversalOrder = new List<CellData>();

        // Queue for breadth-first traversal
        var queue = new Queue<CellData>();
        var visited = new HashSet<int>();

        // Start with leaf nodes (nodes that have no children)
        foreach (var cell in _streamGraph)
        {
            if (!_children.ContainsKey(cell.Index) || _children[cell.Index].Count == 0)
            {
                queue.Enqueue(cell);
                visited.Add(cell.Index);
            }
        }

        // Perform breadth-first traversal
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            traversalOrder.Add(current);

            // If this node has a receiver, process it next (if all its other children have been visited)
            if (_receivers.TryGetValue(current.Index, out var receiverIndex))
            {
                if (receiverIndex == -1) break;

                var receiver = _cellDatas[receiverIndex];
                var allChildrenVisited = true;

                if (_children.TryGetValue(receiver.Index, out var receiverChildren))
                {
                    foreach (var childIndex in receiverChildren)
                    {
                        if (!visited.Contains(childIndex))
                        {
                            allChildrenVisited = false;
                            break;
                        }
                    }
                }

                if (allChildrenVisited && !visited.Contains(receiver.Index))
                {
                    queue.Enqueue(receiver);
                    visited.Add(receiver.Index);
                }
            }
        }

        // Process nodes in reverse traversal order (from leaves to roots)
        for (int i = 0; i < traversalOrder.Count; i++)
        {
            var cell = traversalOrder[i];

            // If this node has a receiver, add its drainage area to the receiver's
            if (_receivers.TryGetValue(cell.Index, out var receiverIndex))
            {
                _drainageArea[receiverIndex] += _drainageArea[cell.Index];

                // Calculate the slope between this node and its receiver
                var receiver = _cellDatas[receiverIndex];
                var distance = (_points[cell.Index] - _points[receiverIndex]).Length();

                // Store the slope value if needed (could be stored in CellData or a separate map)
                // For now, we'll calculate it on demand in the SolvePowerEquation method
            }
        }
    }

    private void SolvePowerEquation()
    {
        ReportProgress("Solving stream power equation");

        // Parameters for the stream power equation
        float k = Settings.ErosionRate; // Erodibility coefficient
        float m = 0.5f; // Drainage area exponent (typically 0.5)
        float dt = Settings.TimeStep; // Time step
        float maxChange = 0.0f; // Track maximum height change for convergence check

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
            var distance = (_points[cell.Index] - _points[receiverIndex]).Length();

            // Skip if distance is zero to avoid division by zero
            if (distance < 0.001f) continue;

            // Get the drainage area for this node
            float drainageArea = _drainageArea.GetValueOrDefault(cell.Index, 1.0f);

            // Apply uplift
            float uplift = cell.Uplift * dt;

            // Calculate the term for the stream power equation
            float erosionTerm = (float)(k * Mathf.Pow(drainageArea, m) / distance);

            // Calculate the new height using the implicit scheme from the paper
            float oldHeight = cell.Height;
            float newHeight = (oldHeight + dt * (uplift + erosionTerm * receiver.Height)) / (1 + erosionTerm * dt);

            // Apply thermal erosion correction: limit the maximum slope
            float maxSlopeHeight = (float)(receiver.Height + distance * Mathf.Tan(Mathf.DegToRad(30.0f)));
            if (newHeight > maxSlopeHeight)
            {
                newHeight = maxSlopeHeight;
            }

            // Update the height
            cell.Height = newHeight;

            // Propagate height changes to uplift for the next iteration
            // This is necessary to maintain continuity between iterations
            cell.Uplift += (newHeight - oldHeight) / dt;

            // Track the maximum change for convergence check
            maxChange = Mathf.Max(maxChange, Mathf.Abs(newHeight - oldHeight));
        }

        // Check for convergence
        _powerEquationConverged = maxChange < Settings.ErosionConvergenceThreshold ||
                                   ++_iterationCount >= Settings.MaxErosionIterations;

        ReportProgress($"[{_iterationCount}/{Settings.MaxErosionIterations}] Stream power equation solved. Max height change: {maxChange}. Converged: {_powerEquationConverged}");
    }
}
