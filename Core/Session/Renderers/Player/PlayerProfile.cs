using Microsoft.Xna.Framework;

namespace SwarmKC.Core.Session.Renderers.Player;

public sealed record PlayerProfile(
    Color BaseColor,
    float BaseColorIntensity,
    Color NucleusColor,
    float NucleusColorIntensity,
    Color SymbolColor,
    float SymbolColorIntensity,
    string SymbolType,
    Color NeonLightColor,
    float NeonLightIntensity
);
