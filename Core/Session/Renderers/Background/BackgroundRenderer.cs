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
    private readonly DepthIllusionWithGridBoxShader _shader;
    private readonly bool _ownsShader;
    private readonly Texture2D _pixel;

    public float DepthStrength { get; set; } = 0.95f;
    public float ParallaxScale { get; set; } = 10.6f;
    public Color FogColor { get; set; } = new(40, 18, 48);
    public float FogStrength { get; set; } = 0.34f;
    public Color LineColor { get; set; } = Color.HotPink;
    public float LineIntensity { get; set; } = 1.0f;
    public float CameraTiltIntensity { get; set; } = 0.22f;

    public BackgroundRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
        : this(graphicsDevice, spriteBatch, DepthIllusionWithGridBoxShader.Load(content), ownsShader: true)
    {
    }

    public BackgroundRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, DepthIllusionWithGridBoxShader shader, bool ownsShader = false)
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
        _shader.SetDepthStrength(DepthStrength);
        _shader.SetParallaxScale(ParallaxScale);
        _shader.SetFog(FogColor, FogStrength);
        _shader.SetLineColor(LineColor, LineIntensity);
        _shader.SetCameraTiltIntensity(CameraTiltIntensity);
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
