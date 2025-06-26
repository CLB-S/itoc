using System;
using Godot;
using ITOC.Core;
using ITOC.Core.Utils;
using ITOC.Core.WorldGeneration.Vanilla;

namespace ITOC;

public partial class EnvironmentController : WorldEnvironment
{
    [Export] public DirectionalLight3D SunLight;
    [Export] public DirectionalLight3D MoonLight;

    // Day-night cycle parameters
    [Export] public Color DaySkyColor = new Color(0.4f, 0.6f, 1.0f);
    [Export] public Color NightSkyColor = new Color(0.03f, 0.03f, 0.1f);
    [Export] public Color DayHorizonColor = new Color(0.8f, 0.8f, 0.9f);
    [Export] public Color NightHorizonColor = new Color(0.1f, 0.1f, 0.2f);
    [Export] public Color SunsetColor = new Color(1.0f, 0.5f, 0.2f, 0.5f);

    [Export] public Color DayAmbientLight = new Color(0.5f, 0.5f, 0.5f);
    [Export] public Color NightAmbientLight = new Color(0.1f, 0.1f, 0.2f);

    [Export] public double DayFogDensity = 0.0005f;
    [Export] public double NightFogDensity = 0.0025f;
    [Export] public Color DayFogColor = new Color(0.75f, 0.75f, 0.85f);
    [Export] public Color NightFogColor = new Color(0.05f, 0.05f, 0.1f);

    [Export] public double SunLightEnergy = 1.0;
    [Export] public double MoonLightEnergy = 0.1;

    [Export] public bool EnableGlow = true;
    [Export] public double DayGlowIntensity = 0.3;
    [Export] public double NightGlowIntensity = 1.0;
    [Export] public double SunsetGlowIntensity = 2.5;

    // Solar elevation angle thresholds for day/night transitions
    [Export] public double SunriseElevationStart = -6.0; // Civil twilight starts at -6 degrees
    [Export] public double SunriseElevationEnd = 12.0;    // Full daylight when sun is at 3 degrees
    [Export] public double SunsetElevationStart = 12.0;   // Sunset begins when sun is at 3 degrees
    [Export] public double SunsetElevationEnd = -6.0;    // Civil twilight ends at -6 degrees

    private Sky _sky;
    private ProceduralSkyMaterial _skyMaterial;
    private double _dayLength;

    public override void _Ready()
    {
        _dayLength = GameController.Instance.CurrentWorld.Settings.MinutesPerDay * 60.0;

        // Setup sky
        _sky = Environment.Sky;
        _skyMaterial = Environment.Sky.SkyMaterial as ProceduralSkyMaterial;
        if (_skyMaterial == null)
        {
            _skyMaterial = new ProceduralSkyMaterial();
            Environment.Sky.SkyMaterial = _skyMaterial;
        }

        // Setup default environment parameters
        Environment.AmbientLightSource = Godot.Environment.AmbientSource.Sky;
        Environment.AmbientLightColor = DayAmbientLight;
        Environment.AmbientLightEnergy = 1.0f;

        // Setup glow if enabled
        if (EnableGlow)
        {
            Environment.GlowEnabled = true;
            Environment.GlowHdrThreshold = 0.8f;
            Environment.GlowIntensity = 0.3f;
            Environment.GlowBlendMode = Godot.Environment.GlowBlendModeEnum.Softlight;
            Environment.GlowHdrLuminanceCap = 3.0f;
        }

        // Initialize moon light
        if (MoonLight != null)
        {
            MoonLight.LightEnergy = MoonLightEnergy;
            MoonLight.LightColor = new Color(0.8f, 0.8f, 1.0f);
            MoonLight.RotationDegrees = new Vector3(0, 0, 0);
            MoonLight.ShadowEnabled = true;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var time = GameController.Instance.CurrentWorld.Time;
        var worldSettings = GameController.Instance.CurrentWorld.Settings;
        var playerPos = Vector3.Zero; // TODO: GameController.Instance.CurrentWorld.PlayerPos;
        var normalizedPos = worldSettings is VanillaWorldSettings settings ?
            (new Vector2(playerPos.X, playerPos.Z) - worldSettings.WorldCenter) / settings.Bounds.Size +
            Vector2.One / 2 : Vector2.One / 2;

        var latitude = -Mathf.Lerp(-90, 90, normalizedPos.Y);
        var longitude = Mathf.Lerp(-180, 180, normalizedPos.X);

        var (solarElevation, solarAzimuth) = OrbitalUtils.CalculateSunPosition(time, latitude, longitude,
            worldSettings.OrbitalInclinationAngle, worldSettings.OrbitalRevolutionDays, worldSettings.MinutesPerDay);

        // Update sun position
        SunLight.RotationDegrees = new Vector3(180 + solarElevation, -solarAzimuth, 0);

        // Update moon position (opposite to sun)
        if (MoonLight != null)
        {
            MoonLight.RotationDegrees = new Vector3(solarElevation, -solarAzimuth, 0);
        }

        // Calculate time-of-day factors for visual transitions based on solar elevation
        var dayFactor = 0.0;
        var sunsetFactor = 0.0;

        // Day factor varies from 0 (night) to 1 (day) based on solar elevation
        if (solarElevation >= SunriseElevationStart && solarElevation <= SunriseElevationEnd)
        {
            // Sunrise transition
            dayFactor = Mathf.InverseLerp(SunriseElevationStart, SunriseElevationEnd, solarElevation);
        }
        else if (solarElevation > SunriseElevationEnd && solarElevation > SunsetElevationStart)
        {
            // Full day
            dayFactor = 1.0;
        }
        else if (solarElevation <= SunsetElevationStart && solarElevation >= SunsetElevationEnd)
        {
            // Sunset transition
            dayFactor = Mathf.InverseLerp(SunsetElevationEnd, SunsetElevationStart, solarElevation);
        }

        // Sunset factor for the orange glow during sunrise/sunset
        if (solarElevation >= SunriseElevationStart && solarElevation <= SunriseElevationEnd)
        {
            // Bell curve for sunrise: peaks in the middle
            var t = Mathf.InverseLerp(SunriseElevationStart, SunriseElevationEnd, solarElevation);
            sunsetFactor = 4.0 * t * (1.0 - t); // Parabola that peaks at 1 when t = 0.5
        }
        else if (solarElevation <= SunsetElevationStart && solarElevation >= SunsetElevationEnd)
        {
            // Bell curve for sunset: peaks in the middle
            var t = Mathf.InverseLerp(SunsetElevationEnd, SunsetElevationStart, solarElevation);
            sunsetFactor = 4.0 * t * (1.0 - t);
        }

        // Update light energies
        SunLight.LightEnergy = SunLightEnergy * dayFactor;
        if (MoonLight != null)
        {
            MoonLight.LightEnergy = MoonLightEnergy * (1.0 - dayFactor);
        }

        // Update sky colors
        if (_skyMaterial != null)
        {
            _skyMaterial.SkyTopColor = DaySkyColor.Lerp(NightSkyColor, 1.0 - dayFactor);
            var horizonColor = DayHorizonColor.Lerp(NightHorizonColor, 1.0 - dayFactor);
            _skyMaterial.SkyHorizonColor = horizonColor;
            _skyMaterial.GroundHorizonColor = horizonColor;
            _skyMaterial.GroundBottomColor = horizonColor;

            if (sunsetFactor > 0)
            {
                horizonColor = _skyMaterial.SkyHorizonColor.Lerp(SunsetColor, sunsetFactor);
                _skyMaterial.SkyHorizonColor = horizonColor;
                _skyMaterial.GroundHorizonColor = horizonColor;
                _skyMaterial.GroundBottomColor = horizonColor;
            }
        }

        // Update ambient lighting
        Environment.AmbientLightColor = DayAmbientLight.Lerp(NightAmbientLight, 1.0 - dayFactor);
        Environment.AmbientLightEnergy = (float)Mathf.Lerp(1.0, 0.5, 1.0 - dayFactor);

        // Update fog
        Environment.FogDensity = (float)Mathf.Lerp(DayFogDensity, NightFogDensity, 1.0 - dayFactor);
        Environment.FogLightColor = DayFogColor.Lerp(NightFogColor, 1.0 - dayFactor);

        // Handle glow effects
        if (EnableGlow)
        {
            float glowIntensity = (float)Mathf.Lerp(DayGlowIntensity, NightGlowIntensity, 1.0 - dayFactor);

            // Increase glow during sunset/sunrise
            if (sunsetFactor > 0)
            {
                glowIntensity = (float)Mathf.Lerp(glowIntensity, SunsetGlowIntensity, sunsetFactor);
            }

            Environment.GlowIntensity = glowIntensity;
            Environment.GlowHdrThreshold = (float)Mathf.Lerp(0.8, 0.5, sunsetFactor);
        }
    }
}