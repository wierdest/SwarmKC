using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SwarmKC.Core.Session.Renderers.Shaders;

namespace SwarmKC.Core.Session.Renderers.Areas;

public sealed class TargetAreaRenderer(SpriteBatch spriteBatch, AreaShader shader, Texture2D pixel, bool ownsShader = false) : IDisposable
{
    private readonly SpriteBatch _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
    private readonly AreaShader _shader = shader ?? throw new ArgumentNullException(nameof(shader));
    private readonly bool _ownsShader = ownsShader;
    private readonly Texture2D _pixel = pixel ?? throw new ArgumentNullException(nameof(pixel));
    private AreaProfile? _profile = null;
    private Color _openBaseColor = Color.SeaGreen;

    public TargetAreaRenderer(SpriteBatch spriteBatch, ContentManager content, Texture2D pixel)
        : this(spriteBatch, AreaShader.Load(content), pixel, ownsShader: true)
    {
    }

    public void ApplyProfile(AreaProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _profile = profile;
    }

    public void SetOpenBaseColor(Color color)
    {
        _openBaseColor = color;
    }

    public void Draw(Vector2 position, float radius, float timeSeconds, bool isOpenToPlayer)
    {
            ArgumentNullException.ThrowIfNull(_profile);

        var baseColor = isOpenToPlayer ? _openBaseColor : _profile.BaseColor;

        float safeRadius = Math.Max(0.5f, radius);
        int diameter = Math.Max(1, (int)MathF.Ceiling(safeRadius * 2f));
        int x = (int)MathF.Round(position.X - safeRadius);
        int y = (int)MathF.Round(position.Y - safeRadius);
        var destination = new Rectangle(x, y, diameter, diameter);

        _shader.SetTexture(_pixel);
        _shader.SetTime(timeSeconds);
        _shader.SetCellPosition(position);
        _shader.SetCellRadius(safeRadius);
        _shader.SetRotation(_profile.RotationRadians);
        _shader.SetBaseColor(baseColor, _profile.BaseColorIntensity);
        _shader.SetNeonLight(_profile.NeonLightColor, _profile.NeonLightIntensity);

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
        if (_ownsShader)
            _shader.Dispose();
    }
}
