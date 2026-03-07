using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SwarmKC.Core.Session.Renderers.Shaders;

public sealed class BackgroundShader : IDisposable
{
    public const string DefaultAssetName = "Shaders/SideScrollOrganicCave";

    private readonly Effect _effect;
    public Effect Effect => _effect;

    private readonly EffectParameter? _targetTexture;
    private readonly EffectParameter _time;
    private readonly EffectParameter _surfaceColor;
    private readonly EffectParameter _fogColor;
    private readonly EffectParameter _backgroundColor;
    private readonly EffectParameter _cameraXMotionSpeed;
    private readonly EffectParameter _screenSize;
    private readonly EffectParameter _lightDirection;
    private readonly EffectParameter _lightingStrength;
    private readonly EffectParameter _lightingPower;
    private readonly EffectParameter _lightColor;
    private readonly EffectParameter _wispScreenPos;
    private readonly EffectParameter _wispLightColor;
    private readonly EffectParameter _wispLightParams;
    private readonly EffectParameter _playerAreaLight;
    private readonly EffectParameter _playerAreaLightColor;
    private readonly EffectParameter _targetAreaLight;
    private readonly EffectParameter _targetAreaOpenLightColor;
    private readonly EffectParameter _targetAreaClosedLightColor;
    private readonly EffectParameter _targetAreaOpenFactor;

    private readonly bool _ownsEffect;

    public BackgroundShader(Effect effect, bool cloneEffect = true)
    {
        ArgumentNullException.ThrowIfNull(effect);

        _effect = cloneEffect ? effect.Clone() : effect;
        _targetTexture = _effect.Parameters["TargetTexture"];
        _time = GetRequiredParameter(_effect, "Time");
        _surfaceColor = GetRequiredParameter(_effect, "SurfaceColor");
        _fogColor = GetRequiredParameter(_effect, "FogColor");
        _backgroundColor = GetRequiredParameter(_effect, "BackgroundColor");
        _cameraXMotionSpeed = GetRequiredParameter(_effect, "CameraXMotionSpeed");
        _screenSize = GetRequiredParameter(_effect, "ScreenSize");
        _lightDirection = GetRequiredParameter(_effect, "LightDirection");
        _lightingStrength = GetRequiredParameter(_effect, "LightingStrength");
        _lightingPower = GetRequiredParameter(_effect, "LightingPower");
        _lightColor = GetRequiredParameter(_effect, "LightColor");
        _wispScreenPos = GetRequiredParameter(_effect, "WispScreenPos");
        _wispLightColor = GetRequiredParameter(_effect, "WispLightColor");
        _wispLightParams = GetRequiredParameter(_effect, "WispLightParams");
        _playerAreaLight = GetRequiredParameter(_effect, "PlayerAreaLight");
        _playerAreaLightColor = GetRequiredParameter(_effect, "PlayerAreaLightColor");
        _targetAreaLight = GetRequiredParameter(_effect, "TargetAreaLight");
        _targetAreaOpenLightColor = GetRequiredParameter(_effect, "TargetAreaOpenLightColor");
        _targetAreaClosedLightColor = GetRequiredParameter(_effect, "TargetAreaClosedLightColor");
        _targetAreaOpenFactor = GetRequiredParameter(_effect, "TargetAreaOpenFactor");

        _ownsEffect = cloneEffect;
    }

    public static BackgroundShader Load(
        ContentManager content,
        string assetName = DefaultAssetName,
        bool cloneEffect = true)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new BackgroundShader(content.Load<Effect>(assetName), cloneEffect);
    }

    public void SetTexture(Texture2D texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        _targetTexture?.SetValue(texture);
    }

    public void SetTime(float seconds)
    {
        _time?.SetValue(seconds);
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
        _backgroundColor?.SetValue(color.ToVector4());
    }

    public void SetCameraXMotionSpeed(float value)
    {
        _cameraXMotionSpeed?.SetValue(Math.Max(0f, value));
    }

    public void SetScreenSize(int width, int height)
    {
        _screenSize?.SetValue(new Vector2(Math.Max(1, width), Math.Max(1, height)));
    }

    public void SetLightDirection(Vector3 direction)
    {
        if (direction.LengthSquared() <= 1e-6f)
            direction = Vector3.Up;
        else
            direction.Normalize();

        _lightDirection.SetValue(direction);
    }

    public void SetLightingStrength(float ambient, float diffuse, float specular, float rim)
    {
        _lightingStrength?.SetValue(new Vector4(
            Math.Max(0f, ambient),
            Math.Max(0f, diffuse),
            Math.Max(0f, specular),
            Math.Max(0f, rim)));
    }

    public void SetLightingPower(float specularPower, float rimPower)
    {
        _lightingPower?.SetValue(new Vector2(
            Math.Max(1f, specularPower),
            Math.Max(1f, rimPower)));
    }

    public void SetLightColor(Color color, float intensity = 1f)
    {
        if (_lightColor is null)
            return;

        var v = color.ToVector4();
        v.W = Math.Max(0f, intensity);
        _lightColor.SetValue(v);
    }

    public void SetWispScreenPosition(Vector2 position)
    {
        _wispScreenPos.SetValue(position);
    }

    public void SetWispLightColor(Color color)
    {
        _wispLightColor.SetValue(color.ToVector4());
    }

    public void SetWispLight(float radiusPx, float intensity)
    {
        _wispLightParams.SetValue(new Vector2(Math.Max(1f, radiusPx), Math.Max(0f, intensity)));
    }

    public void SetWispLightRadiusPx(float radiusPx)
    {
        Vector2 current = _wispLightParams.GetValueVector2();
        _wispLightParams.SetValue(new Vector2(Math.Max(1f, radiusPx), current.Y));
    }

    public void SetWispLightIntensity(float intensity)
    {
        Vector2 current = _wispLightParams.GetValueVector2();
        _wispLightParams.SetValue(new Vector2(current.X, Math.Max(0f, intensity)));
    }

    public void SetPlayerAreaLight(Vector2 position, float radiusPx)
    {
        _playerAreaLight.SetValue(new Vector3(position, Math.Max(1f, radiusPx)));
    }

    public void SetPlayerAreaLightColor(Color color, float intensity = 1f)
    {
        var v = color.ToVector4();
        v.W = Math.Max(0f, intensity);
        _playerAreaLightColor.SetValue(v);
    }

    public void SetTargetAreaLight(Vector2 position, float radiusPx)
    {
        _targetAreaLight.SetValue(new Vector3(position, Math.Max(1f, radiusPx)));
    }

    public void SetTargetAreaOpenLightColor(Color color, float intensity = 1f)
    {
        var v = color.ToVector4();
        v.W = Math.Max(0f, intensity);
        _targetAreaOpenLightColor.SetValue(v);
    }

    public void SetTargetAreaClosedLightColor(Color color, float intensity = 1f)
    {
        var v = color.ToVector4();
        v.W = Math.Max(0f, intensity);
        _targetAreaClosedLightColor.SetValue(v);
    }

    public void SetTargetAreaOpenFactor(float value)
    {
        _targetAreaOpenFactor.SetValue(Math.Clamp(value, 0f, 1f));
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
