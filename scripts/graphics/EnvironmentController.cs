using Godot;

public partial class EnvironmentController : WorldEnvironment
{
    public override void _Ready()
    {
        float viewDistance = Core.Instance.Settings.RenderDistance * World.ChunkSize;
        Environment.FogDepthBegin = viewDistance * 0.85f;
        Environment.FogDepthEnd = viewDistance * 0.95f;
    }
}