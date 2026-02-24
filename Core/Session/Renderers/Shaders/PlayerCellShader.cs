using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SwarmKC.Core.Session.Renderers.Shaders;

public sealed class PlayerCellShader : IDisposable
{
    public const string DefaultAssetName = "Shaders/PlayerCell";

    private readonly Effect _effect;
    public Effect Effect => _effect;

    private readonly EffectParameter _targetTexture;
    private readonly EffectParameter _time;
    private readonly EffectParameter _cellPosition;
    private readonly EffectParameter _cellRadius;
    private readonly EffectParameter _rotation;
    private readonly EffectParameter _baseColor;
    private readonly EffectParameter _nucleusColor;
    private readonly EffectParameter _symbolColor;
    private readonly EffectParameter _symbolType;
    private readonly EffectParameter _neonLight;

    private readonly bool _ownsEffect;

    public PlayerCellShader(Effect effect, bool cloneEffect = true)
    {
        ArgumentNullException.ThrowIfNull(effect);

        _effect = cloneEffect ? effect.Clone() : effect;
        _targetTexture = GetRequiredParameter(_effect, "TargetTexture");
        _time = GetRequiredParameter(_effect, "Time");
        _cellPosition = GetRequiredParameter(_effect, "CellPosition");
        _cellRadius = GetRequiredParameter(_effect, "CellRadius");
        _rotation = GetRequiredParameter(_effect, "Rotation");
        _baseColor = GetRequiredParameter(_effect, "BaseColor");
        _nucleusColor = GetRequiredParameter(_effect, "NucleusColor");
        _symbolColor = GetRequiredParameter(_effect, "SymbolColor");
        _symbolType = GetRequiredParameter(_effect, "SymbolType");
        _neonLight = GetRequiredParameter(_effect, "NeonLight");
        _ownsEffect = cloneEffect;
    }

    public static PlayerCellShader Load(
        ContentManager content,
        string assetName = DefaultAssetName,
        bool cloneEffect = true)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new PlayerCellShader(content.Load<Effect>(assetName), cloneEffect);
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

    public void SetCellPosition(Vector2 position)
    {
        _cellPosition.SetValue(position);
    }

    public void SetCellRadius(float radius)
    {
        _cellRadius.SetValue(Math.Max(0f, radius));
    }

    public void SetRotation(float radians)
    {
        _rotation.SetValue(radians);
    }

    public void SetBaseColor(Color color, float intensity = 1f)
    {
        var v = color.ToVector4();
        v.W = Math.Max(0f, intensity);
        _baseColor.SetValue(v);
    }

    public void SetNucleusColor(Color color, float intensity = 1f)
    {
        var v = color.ToVector4();
        v.W = Math.Max(0f, intensity);
        _nucleusColor.SetValue(v);
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

    public void SetNeonLight(Color color, float intensity = 1f)
    {
        var v = color.ToVector4();
        v.W = Math.Max(0f, intensity);
        _neonLight.SetValue(v);
    }

    private static EffectParameter GetRequiredParameter(Effect effect, string name)
    {
        var parameter = effect.Parameters[name];
        if (parameter is null)
            throw new InvalidOperationException($"Effect is missing required parameter '{name}'.");

        return parameter;
    }

    public void Dispose()
    {
        if (_ownsEffect)
            _effect.Dispose();
    }
}
