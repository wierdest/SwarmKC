using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SwarmKC.Core.Session.Renderers.Shaders;

public sealed class GridBoxDepthShader : IDisposable
{
    public const string DefaultAssetName = "Shaders/GridBoxDepth";

    private readonly Effect _effect;
    public Effect Effect => _effect;

    private readonly EffectParameter _targetTexture;
    private readonly EffectParameter _time;
    private readonly EffectParameter _depthStrength;
    private readonly EffectParameter _parallaxScale;
    private readonly EffectParameter _fog; // float4: rgb=color, a=strength
    private readonly EffectParameter _lineColor; // float4: rgb=color, a=intensity
    private readonly EffectParameter _cameraTiltIntensity;
    private readonly EffectParameter _screenSize;

    private readonly bool _ownsEffect;

    public GridBoxDepthShader(Effect effect, bool cloneEffect = true)
    {
        ArgumentNullException.ThrowIfNull(effect);

        _effect = cloneEffect ? effect.Clone() : effect;
        _targetTexture = GetRequiredParameter(_effect, "TargetTexture");
        _time = GetRequiredParameter(_effect, "Time");
        _depthStrength = GetRequiredParameter(_effect, "DepthStrength");
        _parallaxScale = GetRequiredParameter(_effect, "ParallaxScale");
        _fog = GetRequiredParameter(_effect, "Fog");
        _lineColor = GetRequiredParameter(_effect, "LineColor");
        _cameraTiltIntensity = GetRequiredParameter(_effect, "CameraTiltIntensity");
        _screenSize = GetRequiredParameter(_effect, "ScreenSize");
        _ownsEffect = cloneEffect;
    }

    public static GridBoxDepthShader Load(
        ContentManager content,
        string assetName = DefaultAssetName,
        bool cloneEffect = true)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new GridBoxDepthShader(content.Load<Effect>(assetName), cloneEffect);
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

    public void SetDepthStrength(float value)
    {
        _depthStrength.SetValue(MathHelper.Clamp(value, 0f, 1f));
    }

    public void SetParallaxScale(float value)
    {
        _parallaxScale.SetValue(Math.Max(0f, value));
    }

    public void SetFog(Color color, float strength)
    {
        var v = color.ToVector4();
        v.W = MathHelper.Clamp(strength, 0f, 1f);
        _fog.SetValue(v);
    }

    public void SetLineColor(Color color, float intensity = 1f)
    {
        var v = color.ToVector4();
        v.W = Math.Max(0f, intensity);
        _lineColor.SetValue(v);
    }

    public void SetCameraTiltIntensity(float value)
    {
        _cameraTiltIntensity.SetValue(MathHelper.Clamp(value, 0f, 1f));
    }

    public void SetScreenSize(int width, int height)
    {
        _screenSize.SetValue(new Vector2(Math.Max(1, width), Math.Max(1, height)));
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
