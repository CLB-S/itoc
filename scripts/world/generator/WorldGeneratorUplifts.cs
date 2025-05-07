using System.Collections.Generic;
using Godot;

namespace WorldGenerator;

public partial class WorldGenerator
{
    private readonly HashSet<int> _initialAltitudeIndices = new();

    private void SetInitialUplift(CellData cell, double uplift)
    {
        var f = _upliftPattern.EvaluateSeamlessX(SamplePoints[cell.Index], Settings.Bounds);

        cell.Uplift += uplift * Settings.MaxUplift * (1 + (1 - f) * Settings.UpliftNoiseIntensity);
        _initialAltitudeIndices.Add(cell.Index);
    }

    protected void CalculateInitialUplifts()
    {
        ReportProgress("Calculating initial uplifts.");

        _initialAltitudeIndices.Clear();

        foreach (var edge in _voronoiEdges)
        {
            var cellPId = _delaunator.Triangles[edge.Index];
            var cellQId = _delaunator.Triangles[_delaunator.Halfedges[edge.Index]];
            if (_cellDatas.TryGetValue(cellPId, out var cellP) && _cellDatas.TryGetValue(cellQId, out var cellQ))
            {
                if (cellP.TectonicMovement == cellQ.TectonicMovement) continue;

                // [-1, 1]
                var l = _points[cellPId] - _points[cellQId];
                var relativeMovement = (cellQ.TectonicMovement.Dot(l) - cellP.TectonicMovement.Dot(l)) /
                                       (2 * l.Length() * Settings.MaxTectonicMovement);

                if (Mathf.Abs(relativeMovement) < 0.5)
                    relativeMovement = Mathf.Pow(relativeMovement * 2, 3) / 2;
                else if (relativeMovement > 0.5)
                    relativeMovement = 1 - 2 * (1 - relativeMovement) * (1 - relativeMovement);
                else if (relativeMovement < -0.5)
                    relativeMovement = -1 + 2 * (1 + relativeMovement) * (1 + relativeMovement);

                // if (Mathf.Abs(relativeMovement) < 0.15)
                //     continue;

                cellP.RoundPlateJunction = true;
                cellQ.RoundPlateJunction = true;

                double uplift;
                if (cellP.PlateType == PlateType.Continent && cellQ.PlateType == PlateType.Continent)
                {
                    if (relativeMovement < 0)
                    {
                        uplift = Mathf.Pow(relativeMovement, 3) / 4.0 + 0.25;
                        uplift *= uplift;
                    }
                    else
                    {
                        uplift = 1 - 0.75f * (1 - relativeMovement) * (1 - relativeMovement);
                    }

                    SetInitialUplift(cellP, uplift);
                    SetInitialUplift(cellQ, uplift);
                }
                else if (cellP.PlateType == PlateType.Oceans && cellQ.PlateType == PlateType.Oceans)
                {
                    uplift = Mathf.Pow(relativeMovement, 3) / 2f + 0.5f;
                    uplift = uplift * uplift * 0.5f - 0.3f;
                    if (relativeMovement > 0)
                        uplift += 0.25f * (1 - (1 - relativeMovement) * (1 - relativeMovement));

                    SetInitialUplift(cellP, uplift);
                    SetInitialUplift(cellQ, uplift);
                }
                else if (cellP.PlateType == PlateType.Continent && cellQ.PlateType == PlateType.Oceans)
                {
                    if (relativeMovement < 0)
                    {
                        // cellP.Altitude = -50f * relativeMovement;
                        // cellQ.Altitude = -100f * relativeMovement;
                    }
                    else
                    {
                        uplift = 1 - 0.75 * (1 - relativeMovement) * (1 - relativeMovement) * (1 - relativeMovement);
                        SetInitialUplift(cellP, uplift);

                        // var altitude = Mathf.Pow(relativeMovement, 3) / 2f + 0.5f;
                        // altitude = altitude * altitude - 0.2f;
                        // cellQ.Uplift += altitude;
                    }
                }
                else if (cellP.PlateType == PlateType.Oceans && cellQ.PlateType == PlateType.Continent)
                {
                    if (relativeMovement < 0)
                    {
                        // cellP.Altitude = -100f * relativeMovement;
                        // cellQ.Altitude = -50f * relativeMovement;
                    }
                    else
                    {
                        uplift = 1 - 0.75 * (1 - relativeMovement) * (1 - relativeMovement) * (1 - relativeMovement);
                        SetInitialUplift(cellQ, uplift);

                        // var altitude = Mathf.Pow(relativeMovement, 3) / 2f + 0.5f;
                        // altitude = altitude * altitude - 0.25f;
                        // cellP.Uplift += altitude;
                    }
                }
            }
        }
    }

    protected void PropagateUplifts()
    {
        ReportProgress("Propagating uplifts.");

        var used = new HashSet<int>();
        var queue = new PriorityQueue<int, double>();
        var sharpness = Settings.UpliftPropagationSharpness;

        foreach (var i in _initialAltitudeIndices)
        {
            used.Add(i);
            queue.Enqueue(i, -Mathf.Abs(_cellDatas[i].Uplift));
        }

        while (queue.Count > 0)
        {
            var currentIndex = queue.Dequeue();
            var currentCell = _cellDatas[currentIndex];
            var parentHeight = currentCell.Uplift;

            var propagatedHeight = parentHeight *
                                   Mathf.Pow(Settings.UpliftPropagationDecrement,
                                       Settings.NormalizedMinimumCellDistance);
            if (Mathf.Abs(propagatedHeight) < 0.01f)
                continue;

            foreach (var neighborIndex in GetNeighborCellIndices(currentIndex))
                if (!used.Contains(neighborIndex) && _cellDatas[neighborIndex].PlateType == PlateType.Continent)
                {
                    var neighbor = _cellDatas[neighborIndex];
                    var mod = sharpness == 0 ? 1.0f : 1.0f + (_rng.Randf() - 0.5) * sharpness;
                    var heightContribution = propagatedHeight * mod;

                    neighbor.Uplift += heightContribution;

                    used.Add(neighborIndex);
                    queue.Enqueue(neighborIndex, -Mathf.Abs(_cellDatas[neighborIndex].Uplift));
                }
        }
    }
}