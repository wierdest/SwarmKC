using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SwarmKC.Core.Session.Renderers.Shaders;

public sealed class DepthIllusionWithRuggedSurfacesShader : IDisposable
{
    public const string DefaultAssetName = "Shaders/DepthIllusionWithRuggedSurfaces";

    private readonly Effect _effect;
    public Effect Effect => _effect;

    private readonly EffectParameter _targetTexture;
    private readonly EffectParameter _time;
    // TODO add control for the fog intensity to be able to change the relationship 
    // between surface and background color
    private readonly EffectParameter _surfaceColor;        // float4: rgb=color, a=intensity
    private readonly EffectParameter _fogColor;        // float4: rgb=color, a=intensity
    
    private readonly EffectParameter _backgroundColor;     // float4: rgb=clear/background color
    private readonly EffectParameter _cameraTiltIntensity; // float: 0..1
    private readonly EffectParameter _cameraZMotionSpeed; // float: >= 0
    private readonly EffectParameter _screenSize;          // float2

    private readonly bool _ownsEffect;

    public DepthIllusionWithRuggedSurfacesShader(Effect effect, bool cloneEffect = true)
    {
        ArgumentNullException.ThrowIfNull(effect);

        _effect = cloneEffect ? effect.Clone() : effect;
        _targetTexture = GetRequiredParameter(_effect, "TargetTexture");
        _time = GetRequiredParameter(_effect, "Time");
        _surfaceColor = GetRequiredParameter(_effect, "SurfaceColor");
        _fogColor = GetRequiredParameter(_effect, "FogColor");
        _backgroundColor = GetRequiredParameter(_effect, "BackgroundColor");
        _cameraTiltIntensity = GetRequiredParameter(_effect, "CameraTiltIntensity");
        _cameraZMotionSpeed = GetRequiredParameter(_effect, "CameraZMotionSpeed");
        _screenSize = GetRequiredParameter(_effect, "ScreenSize");
        _ownsEffect = cloneEffect;
    }

    public static DepthIllusionWithRuggedSurfacesShader Load(
        ContentManager content,
        string assetName = DefaultAssetName,
        bool cloneEffect = true)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new DepthIllusionWithRuggedSurfacesShader(content.Load<Effect>(assetName), cloneEffect);
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

    public void SetSurfaceColor(Color color, float intensity = 1f)
    {
        var v = color.ToVector4();
        v.W = Math.Max(0f, intensity);
        _surfaceColor.SetValue(v);
    }

    public void SetFogColor(Color color, float intensity = 1f)
    {
        var v = color.ToVector4();
        v.W = Math.Max(0f, intensity);
        _fogColor.SetValue(v);
    }

    public void SetBackgroundColor(Color color)
    {
        _backgroundColor.SetValue(color.ToVector4());
    }

    public void SetCameraTiltIntensity(float value)
    {
        _cameraTiltIntensity.SetValue(Math.Clamp(value, 0f, 1f));
    }

    public void SetCameraZMotionSpeed(float value)
    {
        _cameraZMotionSpeed.SetValue(Math.Max(0f, value));
    }

    public void SetScreenSize(int width, int height)
    {
        _screenSize.SetValue(new Vector2(Math.Max(1, width), Math.Max(1, height)));
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
