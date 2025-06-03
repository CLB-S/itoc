using Godot;

[Tool]
public partial class NishitaSky : Node3D
{
    #region Exported Properties
    [Export] public bool SunEnabled { get; set; } = true;
    [Export] public Color LightColor { get; set; } = Colors.White;
    [Export] public ShaderMaterial SkyMaterial { get; set; }
    [Export] public NodePath SunObjectPath { get; set; }
    [Export] public NodePath MoonObjectPath { get; set; }
    [Export] public double SunGroundHeight { get; set; } = 1000.0;
    [Export] public double SunSaturationScale { get; set; } = 100.0;
    [Export] public double SunSaturationMult { get; set; } = 0.3;
    [Export(PropertyHint.Range, "0.0000001,1.0")]
    public double SunDesaturationHeight { get; set; } = 0.25;
    [Export] public GradientTexture1D SunGradient { get; set; }
    [Export] public GradientTexture1D SunCloudGradient { get; set; }
    [Export] public GradientTexture1D SunCloudAmbientGradient { get; set; }
    [Export] public GradientTexture1D SunCloudGroundGradient { get; set; }

    [Export]
    public bool ComputeGradientToggle
    {
        get => false;
        set
        {
            if (value)
                ComputeGradients();
        }
    }
    #endregion

    #region Private Fields
    private Color sunColor = Colors.Black;
    private readonly Vector3 groundColor = new(0.1f, 0.07f, 0.034f);
    private const double GroundBrightness = 1.0;
    private const int NumSamples = 16 * 16;
    private const int NumSamplesL = 8 * 2;
    #endregion

    #region Shader Parameter Methods
    private void SetParam(string param, Variant value)
    {
        SkyMaterial?.SetShaderParameter(param, value);
    }

    private Variant GetParam(string param)
    {
        return SkyMaterial?.GetShaderParameter(param) ?? new Variant();
    }
    #endregion

    #region Gradient Computation
    private void ComputeGradients()
    {
        if (SkyMaterial == null) return;

        var cloudHeight = (GetParam("cloud_bottom").AsDouble() +
            GetParam("cloud_top").AsDouble()) * 0.5 + GetParam("Height").AsDouble();
        var sunMinAngleMult = 1.0;
        var earthRadius = GetParam("earthRadius").AsDouble();

        var minSunY = sunMinAngleMult * Mathf.Sin(Mathf.Acos(earthRadius / (earthRadius + SunGroundHeight)));
        var minCloudSunY = sunMinAngleMult * Mathf.Sin(Mathf.Acos(earthRadius / (earthRadius + cloudHeight)));

        SunGradient = ComputeSunGradient(SunGroundHeight, minSunY);
        SunCloudGradient = ComputeSunGradient(cloudHeight, minCloudSunY);
        SunCloudAmbientGradient = ComputeSunGradient(GetParam("cloud_top").AsDouble(), minCloudSunY, true);
        SunCloudGroundGradient = ComputeSunGradient(GetParam("cloud_bottom").AsDouble(), minCloudSunY);
    }

    private GradientTexture1D ComputeSunGradient(double height, double minSunY, bool ambient = false)
    {
        var gradient = new GradientTexture1D();
        gradient.Gradient = new Gradient();

        const int sampleCount = 256;
        var maxCol = 0.0;
        var colors = new Color[sampleCount];
        var positions = new float[sampleCount];

        Vector4 maxSky = ambient
            ? SampleSky(Basis.FromEuler(new Vector3(Mathf.Pi * 0.5, Mathf.Pi * 0.5, 0.0)).Z, Vector3.Up * height, Vector3.Up)
            : SampleSky(Vector3.Up, Vector3.Up * height, Vector3.Up);

        for (int i = 0; i < sampleCount; i++)
        {
            var normalizedI = i / (sampleCount + 1.0);
            var dir = Mathf.Lerp(-0.5 * Mathf.Pi, 0.5 * Mathf.Pi, normalizedI);

            var sunRot = new Vector3(Mathf.Min(dir, Mathf.Asin(minSunY)), 0.0, 0.0);
            var sunBasis = Basis.FromEuler(sunRot);

            var sampleBasis = ambient
                ? Basis.FromEuler(new Vector3(Mathf.Pi * 0.5, Mathf.Pi * 0.5, 0.0))
                : Basis.FromEuler(new Vector3(dir, 0.0, 0.0));

            Vector4 sky = SampleSky(sampleBasis.Z, Vector3.Up * height, sunBasis.Z);
            Color col = new Color((float)sky.X, (float)sky.Y, (float)sky.Z).SrgbToLinear();

            if (!ambient)
            {
                var saturationFactor = Mathf.Clamp((SunDesaturationHeight - sunBasis.Z.Y) / SunDesaturationHeight, 0.0, 1.0);
                col = SaturateColor(col, saturationFactor);
            }

            maxCol = Mathf.Max(Mathf.Max(maxCol, col.R), Mathf.Max(col.G, col.B));
            colors[i] = col;
            positions[i] = (float)normalizedI;
        }

        // Normalize and apply light color
        for (int i = 0; i < sampleCount; i++)
        {
            colors[i] /= (float)maxCol;
            colors[i] = new Color(
                colors[i].R * LightColor.R,
                colors[i].G * LightColor.G,
                colors[i].B * LightColor.B
            );

            if (i > 0 && colors[i] == colors[i - 1])
                continue;

            gradient.Gradient.AddPoint(positions[i], colors[i]);
        }

        // Remove first and last points
        if (gradient.Gradient.Offsets.Length > 0)
        {
            gradient.Gradient.RemovePoint(gradient.Gradient.Offsets.Length - 1);
            gradient.Gradient.RemovePoint(0);
        }

        return gradient;
    }
    #endregion

    #region Utility Methods
    private static double RotToGradient(double rot)
    {
        return (1.0 - rot) * 0.5;
    }

    private static Vector4 NormalizedColor(Vector4 col)
    {
        var maxComponent = Mathf.Max(Mathf.Max(col.X, col.Y), col.Z);
        return maxComponent == 0.0 ? Vector4.Zero : col / maxComponent;
    }

    private Color SaturateColor(Color col, double saturation)
    {
        var newSaturation = Mathf.Clamp(
            Mathf.Log(col.S * saturation * SunSaturationScale + 1.0) * SunSaturationMult,
            0.0, 1.0
        );
        return Color.FromHsv(col.H, (float)newSaturation, col.V);
    }

    private static double Loop(double val, double valRange)
    {
        if (val > valRange)
            return val % valRange - valRange;
        if (val < -valRange)
            return val % -valRange + valRange;
        return val;
    }

    private static Vector3 SolveQuadratic(Vector3 origin, Vector3 dir, double radius)
    {
        var b = 2.0f * dir.Dot(origin);
        var c = origin.Dot(origin) - radius * radius;
        var d = b * b - 4.0f * c;
        var det = Mathf.Sqrt(d);
        return new Vector3((-b + det) * 0.5, (-b - det) * 0.5, d);
    }

    private static Vector3 V3Exp(Vector3 input)
    {
        return new Vector3(Mathf.Exp(input.X), Mathf.Exp(input.Y), Mathf.Exp(input.Z));
    }
    #endregion

    #region Atmosphere Simulation
    private Vector3[] Atmosphere(Vector3 direction, Vector3 pos, Vector3 sunDirection, double intensity = 1.0)
    {
        var re = GetParam("earthRadius").AsDouble();
        var ra = GetParam("atmosphereRadius").AsDouble();
        var hr = GetParam("rayleighScaleHeight").AsDouble();
        var hm = GetParam("mieScaleHeight").AsDouble();
        var mieEccentricity = GetParam("mie_eccentricity").AsDouble();
        var turbidity = GetParam("turbidity").AsDouble();

        var ground = 0.0;
        var mu = direction.Dot(sunDirection);

        var phaseR = (3.0f / (16.0f * Mathf.Pi)) * (1.0 + mu * mu);
        var phaseM = (3.0f / (8.0f * Mathf.Pi)) *
            ((1.0 - mieEccentricity * mieEccentricity) * (1.0 + mu * mu) /
            ((2.0f + mieEccentricity * mieEccentricity) *
            Mathf.Pow(1.0 + mieEccentricity * mieEccentricity - 2.0f * mieEccentricity * mu, 1.5f)));

        var sumR = Vector3.Zero;
        var sumM = Vector3.Zero;

        var cameraPos = new Vector3(0, re + SunGroundHeight + Mathf.Max(0.0, pos.Y), 0);
        Vector3 begin = cameraPos;
        Vector3 end;

        Vector3 d1 = SolveQuadratic(cameraPos, direction, ra);
        if (d1.X > d1.Y && d1.X > 0.0)
        {
            end = cameraPos + direction * d1.X;
            if (d1.Y > 0.0)
                begin = cameraPos + direction * d1.Y;
        }
        else
        {
            return [Vector3.Zero, Vector3.One, Vector3.One];
        }

        Vector3 d2 = SolveQuadratic(cameraPos, direction, re);
        if (d2.X > 0.0 && d2.Y > 0.0)
        {
            end = begin + direction * d2.Y;
            ground = 1.0;
        }

        var segmentLength = begin.DistanceTo(end) / NumSamples;
        var opticalDepthR = 0.0;
        var opticalDepthM = 0.0;
        var atmosphereAtten = Vector3.Zero;

        Vector3 betaR = GetParam("rayleigh_color").AsVector3() * 22.4e-6f * GetParam("rayleigh").AsDouble();
        Vector3 betaM = GetParam("mie_color").AsVector3() * 20e-6f * GetParam("mie").AsDouble();

        for (int i = 0; i < NumSamples; i++)
        {
            Vector3 px = begin + direction * segmentLength * (i + 0.5);
            var sampleHeight = px.Length() - re;

            var hrSample = Mathf.Exp(-sampleHeight / (hr * turbidity)) * segmentLength;
            var hmSample = Mathf.Exp(-sampleHeight / (hm * turbidity)) * segmentLength;

            opticalDepthR += hrSample;
            opticalDepthM += hmSample;

            var opticalDepthLR = 0.0;
            var opticalDepthLM = 0.0;

            Vector3 d3 = SolveQuadratic(px, sunDirection, ra);
            Vector3 d4 = SolveQuadratic(px, sunDirection, re);

            if (d4.X > 0.0 && d4.Y > 0.0)
                continue;

            int j2 = 0;
            var segmentLengthL = Mathf.Max(d3.X, d3.Y) / NumSamplesL;

            for (int j = 0; j < NumSamplesL; j++)
            {
                Vector3 pl = px + sunDirection * segmentLengthL * (j + 0.5);
                var sampleHeightL = pl.Length() - re;
                if (sampleHeightL < 0.0)
                    break;

                opticalDepthLR += Mathf.Exp(-sampleHeightL / (hr * turbidity));
                opticalDepthLM += Mathf.Exp(-sampleHeightL / (hm * turbidity));
                j2++;
            }

            if (j2 == NumSamplesL)
            {
                opticalDepthLR *= segmentLengthL;
                opticalDepthLM *= segmentLengthL;
                Vector3 tau = betaR * (opticalDepthR + opticalDepthLR) + betaM * 1.1f * (opticalDepthM + opticalDepthLM);
                Vector3 attenuation = V3Exp(-tau);
                atmosphereAtten += tau;

                sumR += hrSample * attenuation;
                sumM += hmSample * attenuation;
            }
        }

        Vector3 sky = sumR * phaseR * betaR + sumM * phaseM * betaM;
        return new Vector3[]
        {
            sky,
            atmosphereAtten * (1.0 - ground),
            V3Exp(-(opticalDepthR * betaR + opticalDepthM * betaM))
        };
    }

    private Vector4 SampleSky(Vector3 dir, Vector3 pos, Vector3 sunDir, Vector3 light0Energy = default, Vector3 light0Color = default)
    {
        if (light0Energy == default) light0Energy = Vector3.One;
        if (light0Color == default) light0Color = Vector3.One;

        var sunObject = GetNode(SunObjectPath) as DirectionalLight3D;
        if (sunObject == null) return Vector4.Zero;

        Vector3[] sky = Atmosphere(dir, pos, sunDir);
        Vector3 skyxyz = sky[0];

        Vector3 sun = (Vector3.One - V3Exp(-sky[1])) *
            ((Vector3.One * Mathf.Max(Mathf.Max(dir.Dot(sunDir), 0.0) -
            Mathf.Cos(Mathf.DegToRad(sunObject.LightAngularDistance)), 0.0) * GetParam("sun_brightness").AsDouble()) +
            ((Vector3.One - V3Exp(-sky[2])) * groundColor * Mathf.Max(sunDir.Y, 0.0) * sky[2].X * GroundBrightness)) *
            light0Energy;

        Vector3 col = skyxyz + sun;
        return new Vector4(col.X, col.Y, col.Z, 1.0);
    }
    #endregion

    #region Main Process Loop
    public override void _Process(double delta)
    {
        if (SkyMaterial == null) return;

        var sunObject = GetNode(SunObjectPath) as DirectionalLight3D;
        var moonObject = GetNode(MoonObjectPath) as Node3D;

        if (sunObject == null || moonObject == null) return;

        UpdateRotation();
        UpdateMoonParameters(moonObject);
        UpdateSunParameters(sunObject, moonObject);
        UpdateCloudLighting();
    }

    private void UpdateRotation()
    {
        Rotation = new Vector3(
            Loop(Rotation.X, Mathf.Pi),
            Loop(Rotation.Y, Mathf.Pi),
            Loop(Rotation.Z, Mathf.Pi)
        );
    }

    private void UpdateMoonParameters(Node3D moonObject)
    {
        SetParam("precomputed_moon_dir", moonObject.GlobalTransform.Basis);
    }

    private void UpdateSunParameters(DirectionalLight3D sunObject, Node3D moonObject)
    {
        var cloudHeight = (GetParam("cloud_bottom").AsDouble() + GetParam("cloud_top").AsDouble()) * 0.5 + GetParam("Height").AsDouble();
        Vector3 sunDir = GlobalTransform.Basis.Z;

        var earthRadius = GetParam("earthRadius").AsDouble();
        var sunMinAngleMult = 1.0;
        var minSunY = sunMinAngleMult * Mathf.Sin(Mathf.Acos(earthRadius / (earthRadius + SunGroundHeight)));

        SetParam("precomputed_sun_size", Mathf.DegToRad(sunObject.LightAngularDistance));

        // Calculate moon eclipse effect
        var sunPassthrough = CalculateMoonEclipse(sunObject, moonObject, sunDir);

        // Update sun light energy based on clouds and eclipse
        sunObject.LightEnergy = sunPassthrough * Mathf.Lerp(1.0, 0.0,
            Mathf.Pow(Mathf.Clamp((GetParam("cloud_coverage").AsDouble() - 0.25f) / 0.75f, 0.0, 1.0), 0.5));

        SetParam("precomputed_sun_energy", sunObject.LightIntensityLux / GetWorld3D().Environment.BackgroundIntensity);
        SetParam("precomputed_background_intensity", GetWorld3D().Environment.BackgroundIntensity);

        // Update sun rotation
        sunObject.Rotation = Rotation;
        sunObject.Rotation = new Vector3(
            Rotation.X > Mathf.Pi * 0.5
                ? Mathf.Max(Rotation.X, Mathf.Pi - Mathf.Asin(minSunY))
                : Mathf.Min(Rotation.X, Mathf.Asin(minSunY)),
            sunObject.Rotation.Y,
            sunObject.Rotation.Z
        );

        UpdateSunVisibility(sunObject, sunDir);
        UpdateSunColor(sunObject, sunDir);
    }

    private double CalculateMoonEclipse(DirectionalLight3D sunObject, Node3D moonObject, Vector3 sunDir)
    {
        var precomputedSunSize = Mathf.DegToRad(sunObject.LightAngularDistance);
        var moonRadius = GetParam("moonRadius").AsDouble();
        var moonDistance = GetParam("moonDistance").AsDouble();
        var earthRadius = GetParam("earthRadius").AsDouble();
        Vector3 moonDir = moonObject.GlobalTransform.Basis.Z;

        var moonSize = (moonRadius /
            ((moonDistance + earthRadius) * moonDir -
            Vector3.Up * (GetViewport().GetCamera3D().GlobalPosition.Y + earthRadius + GetParam("Height").AsDouble())).Length() *
            2.0f) * GetParam("moon_size_mult").AsDouble();

        var sunPassthrough = 1.0;
        if (moonSize > 0.0)
        {
            var sunAttenRange = Mathf.Sin(precomputedSunSize);
            var moonAttenRange = Mathf.Sin(Mathf.DegToRad(moonSize)) * 0.5;
            var eclipse = Mathf.Clamp(
                1.0 - Mathf.Clamp(
                    Mathf.Min(moonObject.GlobalTransform.Basis.Z.Dot(sunDir), 1.0) - (1.0 - moonAttenRange),
                    0.0, 1.0
                ) / moonAttenRange,
                0.0, 1.0
            );
            sunPassthrough = Mathf.Pow(eclipse, 2.0f);
        }

        return sunPassthrough;
    }

    private void UpdateSunVisibility(DirectionalLight3D sunObject, Vector3 sunDir)
    {
        if (SunEnabled)
        {
            var earthRadius = GetParam("earthRadius").AsDouble();
            var cloudTop = GetParam("cloud_top").AsDouble();
            bool clouds = GetParam("clouds").AsBool();

            bool visible = sunDir.Y > -Mathf.Sin(
                Mathf.DegToRad(sunObject.LightAngularDistance) +
                Mathf.Acos(earthRadius / (earthRadius + (cloudTop * (clouds ? 1.0 : 0.0))))
            );

            sunObject.Visible = visible;
            SetParam("precomputed_sun_visible", visible);
            SetParam("precomputed_sun_enabled", SunEnabled);
        }
        else
        {
            sunObject.Visible = false;
            SetParam("precomputed_sun_visible", false);
            SetParam("precomputed_sun_enabled", false);
        }
    }

    private void UpdateSunColor(DirectionalLight3D sunObject, Vector3 sunDir)
    {
        if (SunGradient?.Gradient == null) return;

        var gradientPos = RotToGradient(sunDir.Y);
        var sunRatio = Mathf.Asin(Mathf.DegToRad(sunObject.LightAngularDistance)) / Mathf.Pi;
        var sunGradientOffset = -Mathf.Clamp(1.0 - sunDir.Y / sunRatio, 0.0, 1.0) * sunRatio;

        sunObject.LightColor = SunGradient.Gradient.Sample((float)(gradientPos + sunGradientOffset));
        SetParam("precomputed_sun_dir", sunDir);
        SetParam("precomputed_sun_color", LightColor);
    }

    private void UpdateCloudLighting()
    {
        if (!GetParam("clouds").AsBool()) return;

        Vector3 sunDir = GlobalTransform.Basis.Z;
        var gradientPos = RotToGradient(sunDir.Y);
        var sunRatio = 0.0; // You may need to get this from sun object
        var sunGradientOffset = -Mathf.Clamp(1.0 - sunDir.Y / sunRatio, 0.0, 1.0) * sunRatio;

        if (SunCloudGradient?.Gradient != null)
            SetParam("precomputed_Atmosphere_sun", SunCloudGradient.Gradient.Sample((float)(gradientPos + sunGradientOffset)));

        if (SunCloudAmbientGradient?.Gradient != null)
            SetParam("precomputed_Atmosphere_ambient", SunCloudAmbientGradient.Gradient.Sample((float)gradientPos));

        if (SunCloudGroundGradient?.Gradient != null)
            SetParam("precomputed_Atmosphere_ground", SunCloudGroundGradient.Gradient.Sample((float)gradientPos));
    }
    #endregion
}
