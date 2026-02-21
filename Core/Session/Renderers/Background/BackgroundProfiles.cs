using Microsoft.Xna.Framework;

namespace SwarmKC.Core.Session.Renderers.Background;

public static class BackgroundProfiles
{
    public static readonly BackgroundProfile Light = new(
        SurfaceColor: Color.Silver,
        SurfaceColorIntensity: 0.8f,
        FogColor: Color.HotPink,
        FogColorIntensity: 0.3f,
        BackgroundColor: Color.HotPink,
        CameraTiltIntensity: 0.15f,
        CameraZMotionSpeed: 0.5f,
        LightDirection: new Vector3(-0.34f, 0.76f, -0.30f),
        LightingStrength: new Vector4(0.46f, 0.54f, 1.00f, 1.00f),
        LightingPower: new Vector2(14.0f, 2.0f),
        LightColor: Color.GhostWhite,
        LightColorIntensity: 1.00f
    );

    public static readonly BackgroundProfile Dark = new(
        SurfaceColor: Color.DarkCyan,
        SurfaceColorIntensity: 0.8f,
        FogColor: new Color(140, 165, 230),
        FogColorIntensity: 0.3f,
        BackgroundColor: Color.Black,
        CameraTiltIntensity: 0.15f,
        CameraZMotionSpeed: 0.5f,
        LightDirection: new Vector3(-0.20f, 0.62f, -0.75f),
        LightingStrength: new Vector4(0.16f, 0.30f, 0.35f, 0.20f),
        LightingPower: new Vector2(18.0f, 2.4f),
        LightColor: new Color(140, 165, 230),
        LightColorIntensity: 0.42f
    );
}
