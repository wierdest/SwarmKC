using Microsoft.Xna.Framework;

namespace SwarmKC.Core.Session.Renderers.Player;

public static class PlayerProfiles
{
    public static readonly PlayerProfile Light = new(
        BaseColor: Color.White,
        BaseColorIntensity: 1.00f,
        RadianceColor: Color.LightCyan,
        RadianceColorIntensity: 0.95f,
        ParticleCount: 14f,
        ParticleSpeed: -0.5f
    );

    public static readonly PlayerProfile Dark = new(
        BaseColor: Color.AliceBlue,
        BaseColorIntensity: 0.90f,
        RadianceColor: Color.DeepSkyBlue,
        RadianceColorIntensity: 1.10f,
        ParticleCount: 18f,
        ParticleSpeed: -0.5f

    );
}
