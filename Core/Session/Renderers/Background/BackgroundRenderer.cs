using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SwarmKC.Core.Session.Renderers.Shaders;

namespace SwarmKC.Core.Session.Renderers.Background;

public sealed class BackgroundRenderer(
    GraphicsDevice graphicsDevice, 
    SpriteBatch spriteBatch, 
    BackgroundShader shader, 
    Texture2D pixel, 
    bool ownsShader = false) : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
    private readonly SpriteBatch _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
    private readonly BackgroundShader _shader = shader ?? throw new ArgumentNullException(nameof(shader));
    private readonly bool _ownsShader = ownsShader;
    private readonly Texture2D _pixel = pixel;

    public BackgroundRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content, Texture2D pixel)
        : this(graphicsDevice, spriteBatch, BackgroundShader.Load(content), pixel, ownsShader: true)
    {
    }

    public void Draw(
        float timeSeconds,
        Vector2 wispScreenPos,
        Vector2 playerAreaPos,
        float playerAreaRadius,
        Vector2 targetAreaPos,
        float targetAreaRadius,
        bool targetAreaIsOpen)
    {
        _shader.SetTime(timeSeconds);
        _shader.SetScreenSize(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
        _shader.SetWispScreenPosition(wispScreenPos);
        _shader.SetPlayerAreaLight(playerAreaPos, playerAreaRadius);
        _shader.SetTargetAreaLight(targetAreaPos, targetAreaRadius);
        _shader.SetTargetAreaOpenFactor(targetAreaIsOpen ? 1f : 0f);

        _spriteBatch.Begin(
            blendState: BlendState.Opaque,
            samplerState: SamplerState.LinearClamp,
            depthStencilState: DepthStencilState.None,
            rasterizerState: RasterizerState.CullNone,
            effect: _shader.Effect);

        _spriteBatch.Draw(_pixel, _graphicsDevice.Viewport.Bounds, Color.White);
        _spriteBatch.End();
    }

    public void ApplyBackgroundProfile(BackgroundProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        _shader.SetSurfaceColor(profile.SurfaceColor, profile.SurfaceColorIntensity);
        _shader.SetFogColor(profile.FogColor, profile.FogColorIntensity);
        _shader.SetBackgroundColor(profile.BackgroundColor);
        _shader.SetCameraXMotionSpeed(profile.CameraXMotionSpeed);

        _shader.SetLightDirection(profile.LightDirection);
        _shader.SetLightingStrength(
            profile.LightingStrength.X,
            profile.LightingStrength.Y,
            profile.LightingStrength.Z,
            profile.LightingStrength.W);

        _shader.SetLightingPower(
            profile.LightingPower.X,
            profile.LightingPower.Y);

        _shader.SetLightColor(profile.LightColor, profile.LightColorIntensity);
        _shader.SetWispLightColor(profile.WispLightColor);
        _shader.SetWispLightRadiusPx(profile.WispLightRadiusPx);
        _shader.SetWispLightIntensity(profile.WispLightIntensity);
        _shader.SetPlayerAreaLightColor(profile.PlayerAreaLightColor, profile.PlayerAreaLightIntensity);
        _shader.SetTargetAreaOpenLightColor(profile.TargetAreaOpenLightColor, profile.TargetAreaOpenLightIntensity);
        _shader.SetTargetAreaClosedLightColor(profile.TargetAreaClosedLightColor, profile.TargetAreaClosedLightIntensity);
    }

    public void Dispose()
    {
        _pixel.Dispose();
        if (_ownsShader)
            _shader.Dispose();
    }
}
