using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SwarmKC.Core.Session.Renderers.Shaders;

public sealed class PlayerShader : IDisposable
{
    public const string DefaultAssetName = "Shaders/Wisp";

    private readonly Effect _effect;
    public Effect Effect => _effect;

    private readonly EffectParameter _targetTexture;
    private readonly EffectParameter _time;
    private readonly EffectParameter _cellPosition;
    private readonly EffectParameter _velocity;
    private readonly EffectParameter _radius;
    private readonly EffectParameter _rotation;
    private readonly EffectParameter _coreColor;
    private readonly EffectParameter _radianceColor;
    private readonly EffectParameter _particleCount;
    private readonly EffectParameter _particleSpinSpeed;

    private readonly bool _ownsEffect;

    public PlayerShader(Effect effect, bool cloneEffect = true)
    {
        ArgumentNullException.ThrowIfNull(effect);

        _effect = cloneEffect ? effect.Clone() : effect;
        _targetTexture = GetRequiredParameter(_effect, "TargetTexture");
        _time = GetRequiredParameter(_effect, "Time");
        _cellPosition = GetRequiredParameter(_effect, "CellPosition");
        _velocity = GetRequiredParameter(_effect, "Velocity");
        _radius = GetRequiredParameter(_effect, "Radius");
        _rotation = GetRequiredParameter(_effect, "Rotation");
        _coreColor = GetRequiredParameter(_effect, "CoreColor");
        _radianceColor = GetRequiredParameter(_effect, "RadianceColor");
        _particleCount = GetRequiredParameter(_effect, "ParticleCount");
        _particleSpinSpeed = GetRequiredParameter(_effect, "ParticleSpinSpeed");
        _ownsEffect = cloneEffect;
    }

    public static PlayerShader Load(
        ContentManager content,
        string assetName = DefaultAssetName,
        bool cloneEffect = true)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new PlayerShader(content.Load<Effect>(assetName), cloneEffect);
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

    public void SetPosition(Vector2 position)
    {
        _cellPosition.SetValue(position);
    }

    public void SetVelocity(Vector2 velocity)
    {
        _velocity.SetValue(velocity);
    }

    public void SetRadius(float radius)
    {
        _radius.SetValue(Math.Max(0f, radius));
    }

    public void SetRotation(float radians)
    {
        _rotation.SetValue(radians);
    }

    public void SetBaseColor(Color color, float intensity = 1f)
    {
        var v = color.ToVector4();
        v.W = Math.Max(0f, intensity);
        _coreColor.SetValue(v);
    }

    public void SetRadianceColor(Color color, float intensity = 1f)
    {
        var v = color.ToVector4();
        v.W = Math.Max(0f, intensity);
        _radianceColor.SetValue(v);
    }

    public void SetParticleCount(float count)
    {
        _particleCount.SetValue(Math.Clamp(count, 0f, 24f));
    }

    public void SetParticleSpinSpeed(float speed)
    {
        _particleSpinSpeed.SetValue(speed);
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
