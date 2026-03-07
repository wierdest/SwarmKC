using Microsoft.Xna.Framework;

namespace SwarmKC.Core.Session.Renderers.Player;

public sealed record PlayerProfile(
    Color BaseColor,
    float BaseColorIntensity,
    Color RadianceColor,
    float RadianceColorIntensity,
    float ParticleCount,
    float ParticleSpeed
);
