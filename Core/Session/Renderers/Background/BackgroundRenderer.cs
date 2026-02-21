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
    public Color SurfaceColor { get; set; } = Color.HotPink;
    public float SurfaceColorIntensity { get; set; } = 0.8f;
    public Color FogColor { get; set; } = Color.DarkCyan;
    public float FogColorIntensity { get; set; } = 0.3f;
    public float CameraTiltIntensity { get; set; } = 0.15f;
    public float CameraZMotionSpeed { get; set; } = 0.5f;

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

    public void Draw(Color tint, float timeSeconds)
    {
        _shader.SetTexture(_pixel);
        _shader.SetTime(timeSeconds);
        _shader.SetSurfaceColor(SurfaceColor, SurfaceColorIntensity);
        _shader.SetFogColor(FogColor, FogColorIntensity);
        _shader.SetBackgroundColor(tint);
        _shader.SetCameraTiltIntensity(CameraTiltIntensity);
        _shader.SetCameraZMotionSpeed(CameraZMotionSpeed);
        _shader.SetScreenSize(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);

        _spriteBatch.Begin(
            blendState: BlendState.Opaque,
            samplerState: SamplerState.LinearClamp,
            depthStencilState: DepthStencilState.None,
            rasterizerState: RasterizerState.CullNone,
            effect: _shader.Effect);

        _spriteBatch.Draw(_pixel, _graphicsDevice.Viewport.Bounds, tint);
        _spriteBatch.End();
    }

    public void Dispose()
    {
        _pixel.Dispose();
        if (_ownsShader)
            _shader.Dispose();
    }
}
