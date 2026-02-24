using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SwarmKC.Core.Session.Renderers.Shaders;

namespace SwarmKC.Core.Session.Renderers.Player;

public sealed class PlayerRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly PlayerCellShader _shader;
    private readonly bool _ownsShader;
    private readonly Texture2D _pixel;

    public PlayerRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
        : this(graphicsDevice, spriteBatch, PlayerCellShader.Load(content), ownsShader: true)
    {
    }

    public PlayerRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, PlayerCellShader shader, bool ownsShader = false)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        _shader = shader ?? throw new ArgumentNullException(nameof(shader));
        _ownsShader = ownsShader;

        _pixel = new Texture2D(_graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);

        ApplyPlayerProfile(PlayerProfiles.LightHeart);
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
