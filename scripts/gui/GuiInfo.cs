using Godot;
using System.Text;

public partial class GuiInfo : RichTextLabel
{
    public override void _Process(double delta)
    {
        if (!Visible) return;

        // Performance
        var fps = Performance.GetMonitor(Performance.Monitor.TimeFps);
        var drawCalls = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
        var vertices = Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame);
        var staticMem = Performance.GetMonitor(Performance.Monitor.MemoryStatic);
        var staticMemMax = Performance.GetMonitor(Performance.Monitor.MemoryStaticMax);

        // var chunkMem = 0;
        // foreach (var chunk in World.Instance.Chunks.Values)
        //     chunkMem += chunk.ChunkData.GetBytes();

        // Camera and position
        var camPos = CameraHelper.Instance.GetCameraPosition();
        var camFacing = CameraHelper.Instance.GetCameraFacing();
        var camFacingDir = CameraHelper.Instance.GetCameraFacingDirection();
        var camFacingDirName = camFacingDir.Name();
        var chunkPos = World.WorldToChunkPosition(camPos);

        // Time
        var worldTime = World.Instance.Time;
        var worldSettings = World.Instance.Settings;
        var playerPos = World.Instance.PlayerPos;
        var normalizedPos = (new Vector2(playerPos.X, playerPos.Z) - worldSettings.WorldCenter) / worldSettings.Bounds.Size + Vector2.One / 2;
        var latitude = Mathf.Lerp(-90, 90, -normalizedPos.Y);
        var longitude = Mathf.Lerp(-180, 180, normalizedPos.X);
        var localTime = OrbitalUtils.LocalTime(worldTime, longitude, worldSettings.MinutesPerDay);
        (var sunriseTime, var sunsetTime) = OrbitalUtils.CalculateSunriseSunset(worldTime, latitude,
           worldSettings.OrbitalInclinationAngle, worldSettings.OrbitalRevolutionDays, worldSettings.MinutesPerDay);

        var debugTextBuilder = new StringBuilder();
        debugTextBuilder.AppendLine("[b]Debug Info[/b]");
        debugTextBuilder.AppendLine($"[color=yellow]FPS:[/color] {fps:0.0}");
        debugTextBuilder.AppendLine($"[color=yellow]Draw Calls:[/color] {drawCalls}");
        debugTextBuilder.AppendLine($"[color=yellow]Vertices:[/color] {vertices}");
        debugTextBuilder.AppendLine($"[color=yellow]Static Mem:[/color] {BytesToString(staticMem)}/{BytesToString(staticMemMax)}");
        // debugTextBuilder.AppendLine($"[color=yellow]Chunk Mem:[/color] {BytesToString(chunkMem)}");
        debugTextBuilder.AppendLine($"[color=cyan]XYZ:[/color] {camPos.X:0.00}, {camPos.Y:0.00}, {camPos.Z:0.00}");
        debugTextBuilder.AppendLine($"[color=cyan]Chunk:[/color] {chunkPos.X}, {chunkPos.Y}, {chunkPos.Z}");
        debugTextBuilder.AppendLine($"[color=cyan]Facing:[/color] {camFacing.X:0.00}, {camFacing.Y:0.00}, {camFacing.Z:0.00} ({camFacingDirName})");
        debugTextBuilder.AppendLine($"[color=Greenyellow]Chunk Num:[/color] {World.Instance.Chunks.Count}");
        debugTextBuilder.AppendLine($"[color=Greenyellow]ChunkColumn Num:[/color] {World.Instance.ChunkColumns.Count}");
        debugTextBuilder.AppendLine($"[color=green]Time:[/color] {worldTime:0.00}");
        debugTextBuilder.AppendLine($"[color=green]Local Time:[/color] {localTime:0.00}");
        debugTextBuilder.AppendLine($"[color=green]Sunrise:[/color] {sunriseTime:0.00}");
        debugTextBuilder.AppendLine($"[color=green]Sunset:[/color] {sunsetTime:0.00}");


        Text = debugTextBuilder.ToString();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("info"))
            Visible = !Visible;
    }

    private string BytesToString(double size)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        var suffixIndex = 0;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:0.00} {suffixes[suffixIndex]}";
    }
}