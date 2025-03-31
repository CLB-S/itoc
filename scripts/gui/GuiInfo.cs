using Godot;

public partial class GuiInfo : RichTextLabel
{
    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("info")) Visible = !Visible;

        if (!Visible) return;

        // 更新性能信息
        var fps = Performance.GetMonitor(Performance.Monitor.TimeFps);
        var drawCalls = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
        var vertices = Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame);
        var staticMem = Performance.GetMonitor(Performance.Monitor.MemoryStatic);
        var staticMemMax = Performance.GetMonitor(Performance.Monitor.MemoryStaticMax);

        // 更新摄像机信息
        var camPos = CameraHelper.Instance.GetCameraPosition();
        var camFacing = CameraHelper.Instance.GetCameraFacing();
        var camFacingDir = CameraHelper.Instance.GetCameraFacingDirection();
        var camFacingDirName = camFacingDir.Name();
        var chunkPos = World.WorldToChunkPosition(camPos);

        // 构建调试信息文本
        var debugText = "[b]Debug Info[/b]\n";
        debugText += $"[color=yellow]FPS:[/color] {fps:0.0}\n";
        debugText += $"[color=yellow]Draw Calls:[/color] {drawCalls}\n";
        debugText += $"[color=yellow]Vertices:[/color] {vertices}\n";
        debugText += $"[color=yellow]Static Mem:[/color] {BytesToString(staticMem)}/{BytesToString(staticMemMax)}\n";
        debugText += $"[color=cyan]XYZ:[/color] {camPos.X:0.00}, {camPos.Y:0.00}, {camPos.Z:0.00}\n";
        debugText += $"[color=cyan]Chunk:[/color] {chunkPos.X}, {chunkPos.Y}, {chunkPos.Z}\n";
        debugText +=
            $"[color=cyan]Facing:[/color] {camFacing.X:0.00}, {camFacing.Y:0.00}, {camFacing.Z:0.00} ({camFacingDirName})\n";
        debugText += $"[color=Greenyellow]Chunk Num:[/color] {World.Instance.Chunks.Count}\n";

        Text = debugText;
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