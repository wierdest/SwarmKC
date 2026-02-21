using Microsoft.Xna.Framework;

namespace SwarmKC.Core.Session.Renderers.Background;

public sealed record BackgroundProfile(
    Color SurfaceColor,
    float SurfaceColorIntensity,
    Color FogColor,
    float FogColorIntensity,
    Color BackgroundColor,
    float CameraTiltIntensity,
    float CameraZMotionSpeed,
    Vector3 LightDirection,
    Vector4 LightingStrength,
    Vector2 LightingPower,
    Color LightColor,
    float LightColorIntensity
);
