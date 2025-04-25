using Godot;

public partial class EnvironmentController : WorldEnvironment
{
    [Export] public DirectionalLight3D SunLight;

    public override void _Ready()
    {
        // float viewDistance = Core.Instance.Settings.RenderDistance * World.ChunkSize;
        // Environment.FogDepthBegin = viewDistance * 0.85f;
        // Environment.FogDepthEnd = viewDistance * 0.95f;
    }

    public override void _Process(double delta)
    {
        var time = World.Instance.Time;
        var worldSettings = World.Instance.Settings;
        var playerPos = World.Instance.PlayerPos;
        var normalizedPos =
            (new Vector2(playerPos.X, playerPos.Z) - worldSettings.WorldCenter) / worldSettings.Bounds.Size +
            Vector2.One / 2;
        var latitude = -Mathf.Lerp(-90, 90, normalizedPos.Y);
        var longitude = Mathf.Lerp(-180, 180, normalizedPos.X);

        var (solarElevation, solarAzimuth) = OrbitalUtils.CalculateSunPosition(time, latitude, longitude,
            worldSettings.OrbitalInclinationAngle, worldSettings.OrbitalRevolutionDays, worldSettings.MinutesPerDay);
        SunLight.RotationDegrees = new Vector3(solarElevation, -solarAzimuth, 0);
    }
}