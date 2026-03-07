using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SwarmKC.Core.Session.Renderers.Shaders;

public sealed class ProjectileShader : IDisposable
{
    public const string DefaultAssetName = "Shaders/LightParticle";

    private readonly Effect _effect;
    public Effect Effect => _effect;

    private readonly EffectParameter _targetTexture;
    private readonly EffectParameter _time;
    private readonly EffectParameter _particleRadius;
    private readonly EffectParameter _rotation;
    private readonly EffectParameter _particleColor;

    private readonly bool _ownsEffect;

    public ProjectileShader(Effect effect, bool cloneEffect = true)
    {
        ArgumentNullException.ThrowIfNull(effect);

        _effect = cloneEffect ? effect.Clone() : effect;
        _targetTexture = GetRequiredParameter(_effect, "TargetTexture");
        _time = GetRequiredParameter(_effect, "Time");
        _particleRadius = GetRequiredParameter(_effect, "ParticleRadius");
        _rotation = GetRequiredParameter(_effect, "Rotation");
        _particleColor = GetRequiredParameter(_effect, "ParticleColor");
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

    public void SetParticleRadius(float radius)
    {
        _particleRadius.SetValue(Math.Max(0f, radius));
    }

    public void SetRotation(float radians)
    {
        _rotation.SetValue(radians);
    }

    public void SetParticleColor(Color color, float intensity = 1f)
    {
        var v = color.ToVector4();
        v.W = Math.Max(0f, intensity);
        _particleColor.SetValue(v);
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
