using Godot;
using System;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class Settings
{
    // Graphics
    public int RenderDistance { get; set; } = 16;
    public int PhysicsDistance { get; set; } = 8;
    public bool VSync { get; set; } = true;
    public DisplayServer.WindowMode WindowMode { get; set; } = DisplayServer.WindowMode.Windowed;
    public int WindowWidth { get; set; } = 1920;
    public int WindowHeight { get; set; } = 1080;

    // Performance
    public int MaxChunkGenerationsPerFrame { get; set; } = 1;
    public bool DrawDebugChunkCollisionShape { get; set; } = false;

    private static readonly string SettingsPath = "user://settings.yml";

    public void Save()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var yaml = serializer.Serialize(this);
        using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Write);
        file.StoreString(yaml);
    }

    public static Settings Load()
    {
        if (!FileAccess.FileExists(SettingsPath))
            return new Settings();

        using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Read);
        var yaml = file.GetAsText();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        try
        {
            return deserializer.Deserialize<Settings>(yaml);
        }
        catch (Exception)
        {
            GD.PrintErr("Failed to load settings, using defaults");
            return new Settings();
        }
    }

    public void ApplyGraphicsSettings()
    {
        DisplayServer.WindowSetVsyncMode(VSync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
        DisplayServer.WindowSetMode(WindowMode);
        DisplayServer.WindowSetSize(new Vector2I(WindowWidth, WindowHeight));
    }
}
