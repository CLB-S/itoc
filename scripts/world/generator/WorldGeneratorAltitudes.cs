using Godot;
using System.Collections.Generic;

namespace WorldGenerator;

public partial class WorldGenerator
{
    private List<int> _initialAltitudeIndices;

    private void CalculateAltitudes()
    {
        ReportProgress("Calculating altitudes");
        _initialAltitudeIndices = new List<int>();

        foreach (var edge in _edges)
        {
            var cellPId = _delaunator.Triangles[edge.Index];
            var cellQId = _delaunator.Triangles[_delaunator.Halfedges[edge.Index]];
            var cellP = _cellDatas[cellPId];
            var cellQ = _cellDatas[cellQId];
            if (cellP.TectonicMovement != cellQ.TectonicMovement)
            {
                // [-1, 1]
                var l = _points[cellPId] - _points[cellQId];
                var relativeMovement = (float)((cellQ.TectonicMovement.Dot(l) - cellP.TectonicMovement.Dot(l)) /
                                       (2 * l.Length() * Settings.MaxTectonicMovement));

                if (Mathf.Abs(relativeMovement) < 0.25f)
                    continue;

                cellP.RoundPlateJunction = true;
                cellQ.RoundPlateJunction = true;
                _initialAltitudeIndices.Add(cellP.Cell.Index);
                _initialAltitudeIndices.Add(cellQ.Cell.Index);

                if (cellP.PlateType == PlateType.Continent && cellQ.PlateType == PlateType.Continent)
                {
                    float altitude;
                    if (relativeMovement < 0)
                    {
                        altitude = Mathf.Pow(relativeMovement, 3) / 2.0f + 0.5f;
                        altitude *= altitude;
                    }
                    else
                    {
                        altitude = 1 - 0.75f * (relativeMovement - 1) * (relativeMovement - 1);
                    }

                    cellP.Altitude += altitude * Settings.MaxAltitude;
                    cellQ.Altitude += altitude * Settings.MaxAltitude;
                }
                else if (cellP.PlateType == PlateType.Oceans && cellQ.PlateType == PlateType.Oceans)
                {
                    var altitude = Mathf.Pow(relativeMovement, 3) / 2f + 0.5f;
                    altitude = altitude * altitude * 0.5f - 0.3f;
                    if (relativeMovement > 0)
                        altitude += 0.25f * (1 - (1 - relativeMovement) * (1 - relativeMovement));

                    cellP.Altitude += altitude * Settings.MaxAltitude;
                    cellQ.Altitude += altitude * Settings.MaxAltitude;
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
                        cellP.Altitude += 1 - (relativeMovement - 1) * (relativeMovement - 1);

                        var altitude = Mathf.Pow(relativeMovement, 3) / 2f + 0.5f;
                        altitude = altitude * altitude - 0.25f;
                        cellQ.Altitude += altitude;
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
                        cellQ.Altitude += 1 - (relativeMovement - 1) * (relativeMovement - 1);

                        var altitude = Mathf.Pow(relativeMovement, 3) / 2f + 0.5f;
                        altitude = altitude * altitude - 0.25f;
                        cellP.Altitude += altitude;
                    }
                }
            }
        }

    }

    private void PropagateAltitudes()
    {
        var used = new HashSet<int>();
        var queue = new PriorityQueue<int, double>();
        var sharpness = Settings.AltitudePropagationSharpness;

        foreach (var i in _initialAltitudeIndices)
        {
            used.Add(i);
            queue.Enqueue(i, -Mathf.Abs(_cellDatas[i].Altitude));
        }

        while (queue.Count > 0)
        {
            var currentIndex = queue.Dequeue();
            var currentCell = _cellDatas[currentIndex];
            var parentHeight = currentCell.Altitude;

            var propagatedHeight = parentHeight * Settings.AltitudePropagationDecrement;
            if (Mathf.Abs(propagatedHeight) < 0.01f)
                continue;

            foreach (var neighborIndex in GetNeighborCells(currentIndex))
                if (!used.Contains(neighborIndex))
                {
                    var neighbor = _cellDatas[neighborIndex];
                    var mod = sharpness == 0 ? 1.0f : 1.1f - sharpness + (float)_rng.Randf() * sharpness;
                    var heightContribution = propagatedHeight * mod;

                    neighbor.Altitude += heightContribution;

                    used.Add(neighborIndex);
                    queue.Enqueue(neighborIndex, -Mathf.Abs(_cellDatas[neighborIndex].Altitude));
                }
        }
    }
}
