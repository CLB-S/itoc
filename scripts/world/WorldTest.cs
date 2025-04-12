using Godot;
using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WorldGenerator;

public partial class WorldTest : Node2D
{
    public enum ColorPreset
    {
        Plates,
        Height,
        PlateTypes,
        Precipitation,
    }

    // [Export] public ulong Seed { get; set; } = 0;

    [Export] public RichTextLabel TerminalLabel;
    [Export] public MeshInstance3D HeightMapMesh;
    [Export] public Button GenerateMapButton;
    [Export] public Button GenerateHeightMapButton;
    [Export] public Node2D HeightMapSubViewportSprite;
    [Export] public Rect2 DrawingRect = new Rect2(-500, -500, 1000, 1000);

    public ImageTexture HeightMapTexture;

    public ColorPreset DrawingCorlorPreset = ColorPreset.Height;
    public bool DrawTectonicMovement = false;
    public bool DrawCellOutlines = false;
    public bool DrawRivers = false;
    public bool DrawInterpolatedHeightMap = false;

    private WorldGenerator _worldGenerator;

    private int _heightMapResolution = 1000;
    private Vector2 _scalingFactor;

    public override void _Ready()
    {
        base._Ready();

        if (TerminalLabel != null)
            TerminalLabel.GetVScrollBar().Visible = false;

        _worldGenerator = new WorldGenerator();
        _scalingFactor = DrawingRect.Size / _worldGenerator.Settings.Bounds.Size;
        _worldGenerator.ProgressUpdatedEvent += (_, args) => Log(args.Message);
        _worldGenerator.GenerationStartedEvent += (_, _) => GenerateMapButton.Disabled = true;
        _worldGenerator.GenerationCompletedEvent += (_, _) =>
        {
            GenerateMapButton.Disabled = false;
            QueueRedraw();
        };

        _worldGenerator.GenerationFailedEvent += (_, ex) =>
        {
            GenerateMapButton.Disabled = false;
            Log($"[color=red]Generation failed:[/color]\n{ex.Message}");
        };

        Task.Run(_worldGenerator.GenerateWorldAsync);
    }

    private void Log(string message)
    {
        GD.Print(message);
        TerminalLabel?.CallDeferred(RichTextLabel.MethodName.AppendText, message + "\n");
    }

    private void DrawArrow(Vector2 start, Vector2 end, Color color, bool twoLineHead = false)
    {
        // the arrow size and flatness
        var arrowSize = (end - start).Length() * 0.4f;
        var flatness = 0.5f;

        // the line starting and ending points
        DrawLine(start, end, color, Mathf.Sqrt(arrowSize));

        // calculate the direction vector
        var direction = (end - start).Normalized();

        // calculate the side vectors
        var side1 = new Vector2(-direction.Y, direction.X);
        var side2 = new Vector2(direction.Y, -direction.X);

        // calculate the T-junction points
        var e1 = end + side1 * arrowSize * flatness;
        var e2 = end + side2 * arrowSize * flatness;

        // calculate the arrow edges
        var p1 = e1 - direction * arrowSize;
        var p2 = e2 - direction * arrowSize;

        // draw the arrow sides as a polygon
        if (!twoLineHead)
        {
            DrawColoredPolygon([end, p1, p2], color);
        }
        else
        {
            // alternatively, draw the arrow as two lines
            DrawLine(end, p1, color, 2);
            DrawLine(end, p2, color, 2);
        }
    }

    public override void _Draw()
    {
        Log($"Redrawing...");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        if (_worldGenerator.CellDatas != null)
            foreach (var (i, cellData) in _worldGenerator.CellDatas)
            {
                if (!((Rect2)_worldGenerator.Settings.Bounds).HasPoint(_worldGenerator.SamplePoints[i])) continue;

                if (cellData.Cell.Points.Length >= 3)
                {
                    var points = cellData.Cell.Points.Select(p => p * _scalingFactor).ToArray();
                    switch (DrawingCorlorPreset)
                    {
                        case ColorPreset.Plates:
                            var color = ColorUtils.RandomColorHSV(cellData.PlateSeed);
                            DrawColoredPolygon(points, color);
                            break;
                        case ColorPreset.Height:
                            var height = cellData.Altitude / _worldGenerator.Settings.MaxAltitude;
                            DrawColoredPolygon(points, ColorUtils.GetHeightColor((float)height));
                            break;
                        case ColorPreset.PlateTypes:
                            DrawColoredPolygon(points,
                                new Color(0.2f * (int)cellData.PlateType, 0.2f * (int)cellData.PlateType,
                                    (int)cellData.PlateType));
                            break;
                        case ColorPreset.Precipitation:
                            DrawColoredPolygon(points,
                                new Color((float)cellData.Precipitation, (float)cellData.Precipitation, (float)cellData.Precipitation));
                            break;
                    }
                }
            }

        if (DrawCellOutlines && _worldGenerator.CellEdges != null)
        {
            var lines = new Vector2[_worldGenerator.CellEdges.Count() * 2];
            var i = 0;
            foreach (var edge in _worldGenerator.CellEdges)
            {
                lines[i++] = edge.P * _scalingFactor;
                lines[i++] = edge.Q * _scalingFactor;
            }

            DrawMultiline(lines, Colors.White);
        }

        // foreach (var pos in _worldGenerator.SamplePoints)
        // {
        //     DrawCircle(pos, 2f, Colors.White);
        // }

        // foreach (var i in _delaunator.GetHullPoints())
        // {
        //     DrawCircle(i, 2f, Colors.White);
        // }

        if (DrawInterpolatedHeightMap && HeightMapTexture != null) DrawTextureRect(HeightMapTexture, DrawingRect, false);

        if (DrawTectonicMovement)
            foreach (var (i, cellData) in _worldGenerator.CellDatas)
            {
                if (i % 10 != 0) continue;
                var pos = _worldGenerator.SamplePoints[i] * _scalingFactor;
                var end = pos + cellData.TectonicMovement * 3;
                var length = (float)(cellData.TectonicMovement.LengthSquared() / 100.0);
                DrawArrow(pos, end, new Color(length, 0.5f, 1 - length));
            }


        // Draw rivers
        if (DrawRivers)
            foreach (var river in _worldGenerator.Rivers.Values)
            {
                if (river.Path.Count < 2) continue;

                var points = river.Path.ToArray();
                var color = new Color(0.2f, 0.4f, 0.8f);

                for (var i = 0; i < points.Length - 1; i++)
                {
                    if ((points[i] - points[i + 1]).LengthSquared() >
                        _worldGenerator.Settings.MinimumCellDistance * _worldGenerator.Settings.MinimumCellDistance * 5) continue;
                    DrawLine(points[i] * _scalingFactor, points[i + 1] * _scalingFactor, color, river.Width);
                }
                // Draw main river channel
                // DrawPolyline(points, color, river.Width);
                // Draw river banks
                // var bankColor = new Color(0.1f, 0.3f, 0.7f);
                // DrawPolyline(points, bankColor, river.Width * 1.2f);
            }

        // DrawTextureRect(ImageTexture.CreateFromImage(_noise.GetImage((int)_worldGenerator.Settings.Bounds.Size.X, (int)_worldGenerator.Settings.Bounds.Size.Y)), Rect, false);


        // Find neigbour cells of a cell.
        // var itest = 99;
        // DrawCircle(_worldGenerator.SamplePoints[_delaunator.Triangles[itest]], 6f, Colors.Blue);
        // foreach (var item in _delaunator.EdgesAroundPoint(Delaunator.PreviousHalfedge(itest)))
        // {
        //     DrawCircle(_worldGenerator.SamplePoints[_delaunator.Triangles[item]], 4f, Colors.Red);
        // }

        // DrawCircle(_worldGenerator.SamplePoints[itest], 6f, Colors.Blue);
        // foreach (var item in _delaunator.EdgesAroundPoint(Delaunator.PreviousHalfedge(_worldGenerator.CellDatas[itest].TriangleIndex)))
        // {
        //     DrawCircle(_worldGenerator.SamplePoints[_delaunator.Triangles[item]], 4f, Colors.Red);
        // }

        // Find the two cells forming the edge.
        // var edge1 = _worldGenerator.CellEdges[itest];
        // var cellP = _delaunator.Triangles[edge1.Index];
        // var cellQ = _delaunator.Triangles[_delaunator.Halfedges[edge1.Index]];

        // DrawCircle(_worldGenerator.SamplePoints[cellP], 4f, Colors.Red);
        // DrawCircle(_worldGenerator.SamplePoints[cellQ], 4f, Colors.Blue);
        // DrawLine(edge1.P, edge1.Q, Colors.Aqua);

        DrawRect(DrawingRect, Colors.Red, false);

        stopwatch.Stop();
        Log($"Draw time: {stopwatch.ElapsedMilliseconds / 1000.0f}s");
    }

    public void OnSeedSpinBoxValueChanged(float value)
    {
        _worldGenerator.Settings.Seed = (ulong)value;
    }

    public void OnContinentRatioSpinBoxValueChanged(float value)
    {
        _worldGenerator.Settings.ContinentRatio = value;
    }

    public void OnPlateMergeRatioSpinBoxValueChanged(float value)
    {
        _worldGenerator.Settings.PlateMergeRatio = value;
    }

    public void OnCellDistanceSpinBoxValueChanged(float value)
    {
        _worldGenerator.Settings.MinimumCellDistance = value;
    }

    public void OnRegenerateButtonPressed()
    {
        Task.Run(_worldGenerator.GenerateWorldAsync);
    }

    public void OnGenerateHeightMapButtonPressed()
    {
        if (_worldGenerator.State == GenerationState.Completed)
            GenerateFullHeightMap();
    }

    private async void GenerateFullHeightMap()
    {
        GenerateHeightMapButton.Disabled = true;
        Log("Generating height map...");
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await Task.Run(() => HeightMapTexture = _worldGenerator.GetFullHeightMapImageTexture(_heightMapResolution, _heightMapResolution));
        GenerateHeightMapButton.Disabled = false;
        var mat = HeightMapMesh.GetSurfaceOverrideMaterial(0) as ShaderMaterial;
        mat.SetShaderParameter("heightmap", HeightMapTexture);
        stopwatch.Stop();
        Log($"Height map generated in {stopwatch.ElapsedMilliseconds / 1000.0f}s.");

        if (DrawInterpolatedHeightMap)
            QueueRedraw();
    }

    public void OnDrawingCorlorPresetSelected(int value)
    {
        DrawingCorlorPreset = (ColorPreset)value;
        QueueRedraw();
    }

    public void OnDrawTectonicMovementToggled(bool toggledOn)
    {
        DrawTectonicMovement = toggledOn;
        QueueRedraw();
    }

    public void OnDrawCellOutlinesToggled(bool toggledOn)
    {
        DrawCellOutlines = toggledOn;
        QueueRedraw();
    }

    public void OnDrawRiversToggled(bool toggledOn)
    {
        DrawRivers = toggledOn;
        QueueRedraw();
    }

    public void OnDrawInterpolatedHeightMapToggled(bool toggledOn)
    {
        DrawInterpolatedHeightMap = toggledOn;
        QueueRedraw();
    }

    public void OnShow3DHeightMapToggled(bool toggledOn)
    {
        HeightMapSubViewportSprite.Visible = toggledOn;
    }
}
