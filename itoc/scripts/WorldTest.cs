using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ITOC.Core;
using ITOC.Core.Utils;
using ITOC.Core.WorldGeneration;
using Array = Godot.Collections.Array;

namespace ITOC;

public partial class WorldTest : Node2D
{
    public enum ColorPreset
    {
        Plates,
        Uplift,

        // UpliftPattern,
        Height,
        PlateTypes,
        Temperature,
        Precipitation,
        Biome
    }

    // [Export] public ulong Seed { get; set; } = 0;

    [Export] public RichTextLabel TerminalLabel;
    [Export] public MeshInstance3D HeightMapMesh;
    [Export] public OptionButton ColorPresetOptionButton;
    [Export] public Button GenerateMapButton;
    [Export] public Button GenerateHeightMapButton;
    [Export] public Button StartGameButton;
    [Export] public SpinBox SeedSpinBox;
    [Export] public SpinBox ContinentRatioSpinBox;
    [Export] public SpinBox PlateMergeRatioSpinBox;
    [Export] public SpinBox CellDistanceSpinBox;
    [Export] public SpinBox NoiseFrequencySpinBox;
    [Export] public SpinBox ErosionRateSpinBox;
    [Export] public SpinBox ErotionTimeStepSpinBox;
    [Export] public SpinBox MaxErosionIterationsSpinBox;


    [Export] public Node2D HeightMapSubViewportSprite;
    [Export] public Rect2 DrawingRect = new(-500, -500, 1000, 1000);

    public ImageTexture HeightMapTexture;

    public ColorPreset DrawingCorlorPreset = ColorPreset.Height;
    public bool DrawTectonicMovement;
    public bool DrawCellOutlines;
    public bool DrawRivers = true;
    public bool DrawInterpolatedHeightMap;
    public bool DrawWinds;

    private WorldGenerator _worldGenerator => GameControllerNode.Instance.WorldGenerator as WorldGenerator;
    private int _heightMapResolution = 1000;
    private Vector2 _scalingFactor;

    private Gradient _temperatureGradient;
    private Gradient _precipitationGradient;
    private Gradient _heightmapGradient;

    private readonly List<(Mesh, CellData)> _polygons = new(10000);

    public override void _Ready()
    {
        base._Ready();

        if (TerminalLabel != null)
            TerminalLabel.GetVScrollBar().Visible = false;

        StartGameButton.Disabled = true;

        SeedSpinBox.SetValueNoSignal(_worldGenerator.Settings.Seed);
        ContinentRatioSpinBox.SetValueNoSignal(_worldGenerator.Settings.ContinentRatio);
        PlateMergeRatioSpinBox.SetValueNoSignal(_worldGenerator.Settings.PlateMergeRatio);
        CellDistanceSpinBox.SetValueNoSignal(_worldGenerator.Settings.NormalizedMinimumCellDistance);
        NoiseFrequencySpinBox.SetValueNoSignal(_worldGenerator.Settings.NormalizedNoiseFrequency);
        ErosionRateSpinBox.SetValueNoSignal(_worldGenerator.Settings.ErosionRate);
        ErotionTimeStepSpinBox.SetValueNoSignal(_worldGenerator.Settings.ErosionTimeStep);
        MaxErosionIterationsSpinBox.SetValueNoSignal(_worldGenerator.Settings.MaxErosionIterations);

        ColorPresetOptionButton.Clear();
        foreach (var value in Enum.GetValues(typeof(ColorPreset)))
            ColorPresetOptionButton.AddItem(value.ToString(), (int)value);

        ColorPresetOptionButton.Selected = (int)DrawingCorlorPreset;

        _temperatureGradient = ResourceLoader.Load<Gradient>("res://assets/gradients/temperature_gradient.tres");
        _precipitationGradient = ResourceLoader.Load<Gradient>("res://assets/gradients/rainbow_gradient.tres");
        _heightmapGradient = ResourceLoader.Load<Gradient>("res://assets/gradients/heightmap_gradient.tres");

        _scalingFactor = DrawingRect.Size / _worldGenerator.Settings.Bounds.Size;
        _worldGenerator.ProgressUpdatedEvent += (_, args) => Log(args.Message);
        _worldGenerator.GenerationStartedEvent += (_, _) =>
        {
            CallDeferred(MethodName.SetGenerateMapButtonAvailability, false);
            CallDeferred(MethodName.SetStartGameButtonAvailability, false);
        };

        _worldGenerator.GenerationCompletedEvent += (_, _) =>
        {
            CalculatePolygonMeshes();

            CallDeferred(MethodName.SetGenerateMapButtonAvailability, true);
            CallDeferred(MethodName.SetStartGameButtonAvailability, true);
            CallDeferred(CanvasItem.MethodName.QueueRedraw);
        };

        _worldGenerator.GenerationFailedEvent += (_, ex) =>
        {
            CallDeferred(MethodName.SetGenerateMapButtonAvailability, true);
            Log($"[color=red]Generation failed:[/color]\n{ex.Message}");
        };

        Task.Run(_worldGenerator.GenerateWorldAsync);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButtonEvent)
            if (mouseButtonEvent.IsPressed() && _worldGenerator.State == WorldGenerationState.Completed)
            {
                var mousePos = GetGlobalMousePosition();
                if (!DrawingRect.HasPoint(mousePos)) return;

                var worldPos = mousePos / _scalingFactor;
                var nearestCell = _worldGenerator.GetCellDatasNearby(worldPos);
                Log(nearestCell.FirstOrDefault()?.ToString());
            }
    }

    private void Log(string message)
    {
        GD.Print(message);
        TerminalLabel?.CallDeferred(RichTextLabel.MethodName.AppendText, message + "\n");
    }

    private void SetGenerateMapButtonAvailability(bool isEnabled)
    {
        GenerateMapButton.Disabled = !isEnabled;
    }

    private void SetStartGameButtonAvailability(bool isEnabled)
    {
        StartGameButton.Disabled = !isEnabled;
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

    private void CalculatePolygonMeshes()
    {
        _polygons.Clear();

        if (_worldGenerator.CellDatas != null)
            foreach (var (i, cellData) in _worldGenerator.CellDatas)
                if (cellData.Cell.Points.Length >= 3)
                {
                    var indices = new List<int>();
                    var vertices = new List<Vector3>();

                    var polygonPoints = cellData.Cell.Points.Select(p => p * _scalingFactor).ToArray();
                    var triangles = Geometry2D.TriangulatePolygon(polygonPoints);
                    if (triangles == null || triangles.Length == 0) continue;

                    indices.AddRange(triangles);
                    vertices.AddRange(polygonPoints.Select(p => new Vector3(p.X, p.Y, 0)));

                    // Initialize the ArrayMesh.
                    var arrMesh = new ArrayMesh();
                    Array arrays = [];
                    arrays.Resize((int)Mesh.ArrayType.Max);
                    arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
                    arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();

                    // Create the Mesh.
                    arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

                    _polygons.Add((arrMesh, cellData));
                }
    }

    public override void _Draw()
    {
        Log("Redrawing...");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        foreach (var (mesh, cellData) in _polygons)
        {
            Color color;
            var plateType = (int)cellData.PlateType;
            var plateColor = new Color(0.2f * plateType, 0.2f * plateType, plateType);
            switch (DrawingCorlorPreset)
            {
                case ColorPreset.Plates:
                    color = ColorUtils.RandomColorHSV(cellData.PlateSeed);
                    DrawMesh(mesh, null, modulate: color);
                    break;
                case ColorPreset.Uplift:
                    var uplift = (float)(cellData.Uplift / _worldGenerator.Settings.MaxUplift);
                    color = new Color(uplift, uplift, uplift).Lerp(plateColor, 0.5);
                    DrawMesh(mesh, null, modulate: color);
                    break;
                // case ColorPreset.UpliftPattern:
                //     var upliftPatternValue = (float)(0.5 * _worldGenerator._upliftPattern.EvaluateSeamlessX(
                //         _worldGenerator.SamplePoints[cellData.Index], _worldGenerator.Settings.Bounds) + 0.5);
                //     color = new Color(upliftPatternValue, upliftPatternValue, upliftPatternValue).Lerp(plateColor, 0.5);
                //     DrawMesh(mesh, null, modulate: color);
                //     break;
                case ColorPreset.Height:
                    var height = (float)((cellData.Height * 1.5 / _worldGenerator.MaxHeight + 1) * 0.5 - 0.01);
                    DrawMesh(mesh, null, modulate: _heightmapGradient.Sample(height));
                    break;
                case ColorPreset.PlateTypes:
                    DrawMesh(mesh, null, modulate: plateColor);
                    break;
                case ColorPreset.Precipitation:
                    var precipitation = cellData.Precipitation / _worldGenerator.Settings.MaxPrecipitation;
                    color = _precipitationGradient
                        .Sample((float)(precipitation / _worldGenerator.Settings.MaxPrecipitation))
                        .Lerp(plateColor, 0.5);
                    DrawMesh(mesh, null, modulate: color);
                    break;
                case ColorPreset.Temperature:
                    var temperature = cellData.Temperature;
                    var minTemp = _worldGenerator.Settings.PolarTemperature * 1.2;
                    // var maxTemp = _worldGenerator.Settings.EquatorialTemperature;
                    // minTemp < 0 < maxTemp < -minTemp
                    var temperatureColor =
                        _temperatureGradient.Sample((float)((temperature - minTemp) / (2 * -minTemp)));
                    color = temperatureColor.Lerp(plateColor, 0.5);
                    DrawMesh(mesh, null, modulate: color);
                    break;
                case ColorPreset.Biome:
                    var biomeColor = cellData.Biome?.Color ?? Colors.White;
                    DrawMesh(mesh, null, modulate: biomeColor);
                    break;
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

        // Testing `GetTriangleContainingPoint` and `GetNeighborCellIndices`
        // if (_worldGenerator.State == WorldGenerationState.Completed)
        // {
        //     var nearestNeighbor = _worldGenerator.GetCellDatasNearby(new Vector2(0, 0), 1).ToArray();
        //     var nearestId = nearestNeighbor[0].Index;

        //     foreach (var item in _worldGenerator.GetNeighborCellIndices(nearestId))
        //     {
        //         var pos = _worldGenerator.SamplePoints[item] * _scalingFactor;
        //         DrawCircle(pos, 2f, Colors.Blue);
        //     }

        //     var (p0, p1, p2) = _worldGenerator.GetTriangleContainingPoint(new Vector2(0, 0));
        //     DrawCircle(_worldGenerator.SamplePoints[p0] * _scalingFactor, 2f, Colors.Red);
        //     DrawCircle(_worldGenerator.SamplePoints[p1] * _scalingFactor, 2f, Colors.Red);
        //     DrawCircle(_worldGenerator.SamplePoints[p2] * _scalingFactor, 2f, Colors.Red);
        // }

        if (DrawInterpolatedHeightMap && HeightMapTexture != null)
            DrawTextureRect(HeightMapTexture, DrawingRect, false);

        if (DrawTectonicMovement)
            foreach (var (i, cellData) in _worldGenerator.CellDatas)
            {
                if (i % 10 != 0) continue;
                var pos = cellData.Position * _scalingFactor;
                var end = pos + cellData.TectonicMovement * 3;
                var length = (float)(cellData.TectonicMovement.LengthSquared() / 100.0);
                DrawArrow(pos, end, new Color(length, 0.5f, 1 - length));
            }

        // Draw stream graph
        if (DrawRivers && _worldGenerator.State == WorldGenerationState.Completed)
        {
            // Access the stream graph data from the world generator
            var receivers = _worldGenerator.Receivers;

            if (receivers != null && receivers.Count > 0)
                // Draw stream connections (edges in the stream tree)
                foreach (var cell in _worldGenerator.CellDatas.Values)
                {
                    if (cell.PlateType != PlateType.Continent) continue;

                    if (receivers.TryGetValue(cell.Index, out var receiverIndex))
                        if (_worldGenerator.CellDatas.TryGetValue(receiverIndex, out var receiver))
                        {
                            if (cell.Index == receiverIndex) continue;
                            var start = cell.Position * _scalingFactor;
                            var end = receiver.Position * _scalingFactor;

                            if ((start - end).LengthSquared() >
                                _worldGenerator.Settings.MinimumCellDistance *
                                _worldGenerator.Settings.MinimumCellDistance * 5) continue;

                            var drainage = _worldGenerator.Drainages[cell.Index];
                            if (drainage < 6000000) continue;

                            var width = Mathf.Log(1 + (drainage - 6000000) * 0.0005f) * 0.5f;

                            // Calculate color based on water flow
                            // Deeper blue for higher drainage areas
                            // var alpha = (float)Mathf.Clamp(width / 5.0, 0.7, 1.0);
                            var color = new Color(0.1f, 0.4f, 0.8f);

                            DrawLine(start, end, color, width);
                        }
                }

            // Draw lake areas in a lighter blue color
            foreach (var lakeId in _worldGenerator.Lakes)
            {
                if (_worldGenerator.CellDatas[lakeId].IsRiverMouth) continue;

                var pos = _worldGenerator.CellDatas[lakeId].Position * _scalingFactor;
                DrawCircle(pos, 4f, new Color(0.2f, 0.6f, 0.9f, 0.7f));
            }
            // foreach (var cell in streamGraph)
            // {
            //     // A cell is part of a lake if it's not a receiver for any other node
            //     // or if it's at the bottom of a depression
            //     if (!receivers.ContainsKey(cell.Index) && !cell.IsRiverMouth)
            //     {
            //         // Draw lake node as a circle
            //         var pos = _worldGenerator.SamplePoints[cell.Index] * _scalingFactor;
            //         DrawCircle(pos, 4f, new Color(0.2f, 0.6f, 0.9f, 0.7f));
            //     }
            // }
            // Draw river mouths with a different color
            // foreach (var cell in streamGraph.Where(c => c.IsRiverMouth))
            // {
            //     var pos = _worldGenerator.SamplePoints[cell.Index] * _scalingFactor;
            //     DrawCircle(pos, 3f, new Color(0.0f, 0.2f, 0.6f, 0.8f));
            // }
        }

        // Draw wind vectors
        if (DrawWinds && _worldGenerator.State == WorldGenerationState.Completed)
        {
            var windSettings = new WindSettings();
            using var rng = new RandomNumberGenerator();
            rng.Seed = _worldGenerator.Settings.Seed;

            foreach (var (i, _) in _worldGenerator.CellDatas)
            {
                if (i % 10 != 0) continue;

                var pos = _worldGenerator.SamplePoints[i];
                var longitude = _worldGenerator.GetLongitude(pos);
                var latitude = _worldGenerator.GetLatitude(pos);
                var wind = ClimateUtils.GetSurfaceWind(latitude, longitude, windSettings, rng);
                var end = pos * _scalingFactor + wind * 20;
                DrawArrow(pos * _scalingFactor, end, Colors.Black);
            }
        }

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
        _worldGenerator.Settings.NormalizedMinimumCellDistance = value;
    }

    public void OnNoiseFrequencySpinBoxValueChanged(float value)
    {
        _worldGenerator.Settings.NormalizedNoiseFrequency = value;
    }

    public void OnErosionRateSpinBoxValueChanged(float value)
    {
        _worldGenerator.Settings.ErosionRate = value;
    }

    public void OnErotionTimeStepSpinBoxValueChanged(float value)
    {
        _worldGenerator.Settings.ErosionTimeStep = value;
    }

    public void OnMaxErosionIterationsSpinBoxValueChanged(float value)
    {
        _worldGenerator.Settings.MaxErosionIterations = (int)value;
    }

    public void OnRegenerateButtonPressed()
    {
        Task.Run(_worldGenerator.GenerateWorldAsync);
    }

    public void OnGenerateHeightMapButtonPressed()
    {
        if (_worldGenerator.State == WorldGenerationState.Completed)
            GenerateFullHeightMap();
    }

    private async void GenerateFullHeightMap()
    {
        GenerateHeightMapButton.Disabled = true;
        Log("Generating height map...");
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await Task.Run(() =>
            HeightMapTexture =
                _worldGenerator.GetFullHeightMapImageTexture(_heightMapResolution, _heightMapResolution));
        GenerateHeightMapButton.Disabled = false;
        var mat = HeightMapMesh.GetSurfaceOverrideMaterial(0) as ShaderMaterial;
        mat.SetShaderParameter("heightmap", HeightMapTexture);
        stopwatch.Stop();
        Log($"Height map generated in {stopwatch.ElapsedMilliseconds / 1000.0f}s.");

        if (DrawInterpolatedHeightMap)
            QueueRedraw();
    }

    public void OnStartGameButtonPressed()
    {
        if (_worldGenerator.State == WorldGenerationState.Completed)
        {
            StartGameButton.Disabled = true;
            GameControllerNode.Instance.GotoWorldScene();
        }
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

    public void OnDrawWindsToggled(bool toggledOn)
    {
        DrawWinds = toggledOn;
        QueueRedraw();
    }

    public void OnShow3DHeightMapToggled(bool toggledOn)
    {
        HeightMapSubViewportSprite.Visible = toggledOn;
    }
}