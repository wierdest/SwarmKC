using Microsoft.Xna.Framework;

namespace SwarmKC.Core.Session.Renderers.Areas;

public static class AreaProfiles
{
    public static readonly AreaProfile PlayerAreaLight = new(
        BaseColor: Color.SeaGreen,
        BaseColorIntensity: 0.60f,
        NeonLightColor: Color.MediumSpringGreen,
        NeonLightIntensity: 0.35f,
        RotationRadians: 0f
    );

    public static readonly AreaProfile TargetAreaLight = new(
        BaseColor: Color.Purple,
        BaseColorIntensity: 0.60f,
        NeonLightColor: Color.MediumSpringGreen,
        NeonLightIntensity: 0.35f,
        RotationRadians: 0f
    );

    public static readonly AreaProfile Dark = new(
        BaseColor: Color.DarkSlateBlue,
        BaseColorIntensity: 0.65f,
        NeonLightColor: Color.DeepSkyBlue,
        NeonLightIntensity: 0.40f,
        RotationRadians: 0f
    );
}
