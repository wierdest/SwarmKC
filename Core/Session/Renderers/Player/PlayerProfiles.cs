using Microsoft.Xna.Framework;

namespace SwarmKC.Core.Session.Renderers.Player;

public static class PlayerProfiles
{
    public static readonly PlayerProfile LightHeart = new(
        BaseColor: Color.GhostWhite,
        BaseColorIntensity: 0.7f,
        NucleusColor: Color.SeaGreen,
        NucleusColorIntensity: 0.35f,
        SymbolColor: Color.HotPink,
        SymbolColorIntensity: 0.95f,
        SymbolType: "heart",
        NeonLightColor: Color.HotPink,
        NeonLightIntensity: 0.20f
    );

    public static readonly PlayerProfile DarkHeart = new(
        BaseColor: Color.DarkBlue,
        BaseColorIntensity: 0.78f,
        NucleusColor: Color.DarkOrange,
        NucleusColorIntensity: 0.60f,
        SymbolColor: Color.DodgerBlue,
        SymbolColorIntensity: 0.95f,
        SymbolType: "heart",
        NeonLightColor: Color.DeepSkyBlue,
        NeonLightIntensity: 0.64f
    );

    public static readonly PlayerProfile LightStar = new(
        BaseColor: Color.GhostWhite,
        BaseColorIntensity: 0.7f,
        NucleusColor: Color.SeaGreen,
        NucleusColorIntensity: 0.35f,
        SymbolColor: Color.Gold,
        SymbolColorIntensity: 0.95f,
        SymbolType: "star",
        NeonLightColor: Color.HotPink,
        NeonLightIntensity: 0.20f
    );

    public static readonly PlayerProfile DarkStar = new(
        BaseColor: Color.DarkBlue,
        BaseColorIntensity: 0.78f,
        NucleusColor: Color.DarkOrange,
        NucleusColorIntensity: 0.60f,
        SymbolColor: Color.LightSkyBlue,
        SymbolColorIntensity: 0.95f,
        SymbolType: "star",
        NeonLightColor: Color.DeepSkyBlue,
        NeonLightIntensity: 0.64f
    );
}
