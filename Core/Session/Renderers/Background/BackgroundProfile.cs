using Microsoft.Xna.Framework;

namespace SwarmKC.Core.Session.Renderers.Background;

public sealed record BackgroundProfile(
    Color SurfaceColor,
    float SurfaceColorIntensity,
    Color FogColor,
    float FogColorIntensity,
    Color BackgroundColor,
    float CameraTiltIntensity,
    float CameraXMotionSpeed,
    Vector3 LightDirection,
    Vector4 LightingStrength,
    Vector2 LightingPower,
    Color LightColor,
    float LightColorIntensity,
    Color WispLightColor,
    float WispLightRadiusPx,
    float WispLightIntensity,
    Color PlayerAreaLightColor,
    float PlayerAreaLightIntensity,
    Color TargetAreaOpenLightColor,
    float TargetAreaOpenLightIntensity,
    Color TargetAreaClosedLightColor,
    float TargetAreaClosedLightIntensity
);
