using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SwarmKC.Core.Session.Renderers.Shaders;

namespace SwarmKC.Core.Session.Renderers.Background;

public sealed class BackgroundRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly OrganicDepthShader _shader;
    private readonly bool _ownsShader;
    private readonly Texture2D _pixel;

    public BackgroundRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
        : this(graphicsDevice, spriteBatch, OrganicDepthShader.Load(content), ownsShader: true)
    {
    }

    public BackgroundRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, OrganicDepthShader shader, bool ownsShader = false)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        _shader = shader ?? throw new ArgumentNullException(nameof(shader));
        _ownsShader = ownsShader;

        _pixel = new Texture2D(_graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
    }

    public void Draw(float timeSeconds)
    {
        _shader.SetTexture(_pixel);
        _shader.SetTime(timeSeconds);
        _shader.SetScreenSize(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
        
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
        _shader.SetCameraTiltIntensity(profile.CameraTiltIntensity);
        _shader.SetCameraZMotionSpeed(profile.CameraZMotionSpeed);

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
    }


    public void Dispose()
    {
        _pixel.Dispose();
        if (_ownsShader)
            _shader.Dispose();
    }
}
