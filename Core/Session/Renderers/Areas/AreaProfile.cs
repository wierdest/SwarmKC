using Microsoft.Xna.Framework;

namespace SwarmKC.Core.Session.Renderers.Areas;

public sealed record AreaProfile(
    Color BaseColor,
    float BaseColorIntensity,
    Color NeonLightColor,
    float NeonLightIntensity,
    float RotationRadians = 0f
);
