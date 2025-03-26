using Godot;
using System;

public partial class GuiInfo : RichTextLabel
{
    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("info"))
        {
            this.Visible = !this.Visible;
        }

        if (!this.Visible) return;

        // 更新性能信息
        var fps = Performance.GetMonitor(Performance.Monitor.TimeFps);
        var drawCalls = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
        var vertices = Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame);
        var staticMem = Performance.GetMonitor(Performance.Monitor.MemoryStatic);
        var staticMemMax = Performance.GetMonitor(Performance.Monitor.MemoryStaticMax);

        // 更新摄像机信息
        Vector3 camPos = CameraHelper.Instance.GetCameraPosition();
        Vector3 camFacing = CameraHelper.Instance.GetCameraFacing();
        Direction camFacingDir = CameraHelper.Instance.GetCameraFacingDirection();
        string camFacingDirName = DirectionHelper.GetDirectionName(camFacingDir);

        // 构建调试信息文本
        string debugText = $"[b]Debug Info[/b]\n";
        debugText += $"[color=yellow]FPS:[/color] {fps:0.0}\n";
        debugText += $"[color=yellow]Draw Calls:[/color] {drawCalls}\n";
        debugText += $"[color=yellow]Vertices:[/color] {vertices}\n";
        debugText += $"[color=yellow]Static Mem:[/color] {BytesToString(staticMem)}/{BytesToString(staticMemMax)}\n";
        debugText += $"[color=cyan]XYZ:[/color] {camPos.X:0.00}, {camPos.Y:0.00}, {camPos.Z:0.00}\n";
        debugText += $"[color=cyan]Facing:[/color] {camFacing.X:0.00}, {camFacing.Y:0.00}, {camFacing.Z:0.00} ({camFacingDirName})";

        this.Text = debugText;
    }

    private string BytesToString(double size)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int suffixIndex = 0;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:0.00} {suffixes[suffixIndex]}";
    }
}