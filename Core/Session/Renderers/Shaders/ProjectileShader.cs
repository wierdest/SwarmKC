using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SwarmKC.Core.Session.Renderers.Shaders;

public sealed class ProjectileShader : IDisposable
{
    public const string DefaultAssetName = "Shaders/Symbol";

    private readonly Effect _effect;
    public Effect Effect => _effect;

    private readonly EffectParameter _targetTexture;
    private readonly EffectParameter _time;
    private readonly EffectParameter _symbolRadius;
    private readonly EffectParameter _rotation;
    private readonly EffectParameter _symbolColor;
    private readonly EffectParameter _symbolType;

    private readonly bool _ownsEffect;

    public ProjectileShader(Effect effect, bool cloneEffect = true)
    {
        ArgumentNullException.ThrowIfNull(effect);

        _effect = cloneEffect ? effect.Clone() : effect;
        _targetTexture = GetRequiredParameter(_effect, "TargetTexture");
        _time = GetRequiredParameter(_effect, "Time");
        _symbolRadius = GetRequiredParameter(_effect, "SymbolRadius");
        _rotation = GetRequiredParameter(_effect, "Rotation");
        _symbolColor = GetRequiredParameter(_effect, "SymbolColor");
        _symbolType = GetRequiredParameter(_effect, "SymbolType");
        _ownsEffect = cloneEffect;
    }

    public static ProjectileShader Load(
        ContentManager content,
        string assetName = DefaultAssetName,
        bool cloneEffect = true)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new ProjectileShader(content.Load<Effect>(assetName), cloneEffect);
    }

    public void SetTexture(Texture2D texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        _targetTexture.SetValue(texture);
    }

    public void SetTime(float seconds)
    {
        _time.SetValue(seconds);
    }

    public void SetSymbolRadius(float radius)
    {
        _symbolRadius.SetValue(Math.Max(0f, radius));
    }

    public void SetRotation(float radians)
    {
        _rotation.SetValue(radians);
    }

    public void SetSymbolColor(Color color, float intensity = 1f)
    {
        var v = color.ToVector4();
        v.W = Math.Max(0f, intensity);
        _symbolColor.SetValue(v);
    }

    public void SetSymbolType(string? symbolType)
    {
        _symbolType.SetValue(SymbolTypeToValue(symbolType));
    }

    public void SetSymbolType(float symbolType)
    {
        _symbolType.SetValue(symbolType >= 0.5f ? 1f : 0f);
    }

    private static float SymbolTypeToValue(string? symbolType)
    {
        if (string.Equals(symbolType, "star", StringComparison.OrdinalIgnoreCase))
            return 1f;

        return 0f; // default: heart
    }

    private static EffectParameter GetRequiredParameter(Effect effect, string name)
    {
        var parameter = effect.Parameters[name] ?? throw new InvalidOperationException($"Effect is missing required parameter '{name}'.");
        return parameter;
    }

    public void Dispose()
    {
        if (_ownsEffect)
            _effect.Dispose();
    }
}
