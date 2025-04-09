using Godot;

public partial class EnvironmentController : WorldEnvironment
{
    public override void _Ready()
    {
        float viewDistance = World.Instance.Settings.LoadDistance * World.ChunkSize;
        Environment.FogDepthBegin = viewDistance * 0.85f;
        Environment.FogDepthEnd = viewDistance * 0.95f;
    }
}