using Microsoft.Xna.Framework;

namespace SwarmKC.Core.Session.Renderers.Projectiles;

public sealed record ProjectileProfile(
    Color SymbolColor,
    float SymbolColorIntensity,
    string SymbolType
);
