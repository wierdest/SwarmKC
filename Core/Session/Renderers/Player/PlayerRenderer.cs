using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SwarmKC.Core.Session.Renderers.Shaders;

namespace SwarmKC.Core.Session.Renderers.Player;

public sealed class PlayerRenderer(
    GraphicsDevice graphicsDevice,
    SpriteBatch spriteBatch,
    PlayerShader shader,
    Texture2D pixel, 
    bool ownsShader = false) : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
    private readonly SpriteBatch _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
    private readonly PlayerShader _shader = shader ?? throw new ArgumentNullException(nameof(shader));
    private readonly bool _ownsShader = ownsShader;
    private readonly Texture2D _pixel = pixel;

    public PlayerRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content, Texture2D pixel)
        : this(graphicsDevice, spriteBatch, PlayerShader.Load(content), pixel, ownsShader: true)
    {
    }

    public void ApplyPlayerProfile(PlayerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _shader.SetBaseColor(profile.BaseColor, profile.BaseColorIntensity);
        _shader.SetNucleusColor(profile.NucleusColor, profile.NucleusColorIntensity);
        _shader.SetSymbolColor(profile.SymbolColor, profile.SymbolColorIntensity);
        _shader.SetSymbolType(profile.SymbolType);
        _shader.SetNeonLight(profile.NeonLightColor, profile.NeonLightIntensity);
    }

    public void Draw(Vector2 position, float radius, float rotationRadians, float timeSeconds)
    {
        float safeRadius = Math.Max(0.5f, radius);
        int diameter = Math.Max(1, (int)MathF.Ceiling(safeRadius * 2f));
        int x = (int)MathF.Round(position.X - safeRadius);
        int y = (int)MathF.Round(position.Y - safeRadius);
        var destination = new Rectangle(x, y, diameter, diameter);

        _shader.SetTexture(_pixel);
        _shader.SetTime(timeSeconds);
        _shader.SetCellPosition(position);
        _shader.SetCellRadius(safeRadius);
        _shader.SetRotation(rotationRadians);

        _spriteBatch.Begin(
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp,
            depthStencilState: DepthStencilState.None,
            rasterizerState: RasterizerState.CullNone,
            effect: _shader.Effect);

        _spriteBatch.Draw(_pixel, destination, Color.White);
        _spriteBatch.End();
    }

    public void Dispose()
    {
        _pixel.Dispose();
        if (_ownsShader)
            _shader.Dispose();
    }
}
