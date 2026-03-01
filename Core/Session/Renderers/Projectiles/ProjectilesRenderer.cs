using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SwarmKC.Core.Session.Renderers.Shaders;

namespace SwarmKC.Core.Session.Renderers.Projectiles;

public sealed class ProjectilesRenderer(
    GraphicsDevice graphicsDevice,
    SpriteBatch spriteBatch,
    ProjectileShader shader,
    Texture2D pixel, 
    bool ownsShader = false) : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
    private readonly SpriteBatch _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
    private readonly ProjectileShader _shader = shader ?? throw new ArgumentNullException(nameof(shader));
    private readonly bool _ownsShader = ownsShader;
    private readonly Texture2D _pixel = pixel ?? throw new ArgumentNullException(nameof(pixel));

    private ProjectileProfile? _playerProfile;
    private ProjectileProfile? _enemyProfile;

    private readonly Dictionary<int, Texture2D> _circleCache = new();


    public ProjectilesRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content, Texture2D pixel)
        : this(graphicsDevice, spriteBatch, ProjectileShader.Load(content), pixel, ownsShader: true)
    {
    }

    public void ApplyPlayerProfile(ProjectileProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _playerProfile = profile;
    }

    public void ApplyEnemyProfile(ProjectileProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _enemyProfile = profile;
    }

    public void Draw(Vector2 position, float radius, float rotationRadians, float timeSeconds, bool ownedByPlayer)
    {
        var profile = ownedByPlayer ? _playerProfile : _enemyProfile;
        if (profile is null)
        {
            DrawCircle(position, (int)MathF.Round(radius), Color.OrangeRed);
            return;
        }

        _shader.SetSymbolColor(profile.SymbolColor, profile.SymbolColorIntensity);
        _shader.SetSymbolType(profile.SymbolType);

        float safeRadius = Math.Max(0.5f, radius);
        int diameter = Math.Max(1, (int)MathF.Ceiling(safeRadius * 2f));
        int x = (int)MathF.Round(position.X - safeRadius);
        int y = (int)MathF.Round(position.Y - safeRadius);
        var destination = new Rectangle(x, y, diameter, diameter);

        _shader.SetTexture(_pixel);
        _shader.SetTime(timeSeconds);
        _shader.SetSymbolRadius(safeRadius);
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
        if (_ownsShader)
            _shader.Dispose();
    }

    private Texture2D GetCircle(int radius)
    {
        if (_circleCache.TryGetValue(radius, out var tex)) return tex;

        int diam = radius * 2 + 1;
        var data = new Color[diam * diam];
        int r2 = radius * radius;

        for (int y = 0; y < diam; y++)
        {
            for (int x = 0; x < diam; x++)
            {
                int dx = x - radius;
                int dy = y - radius;
                data[y * diam + x] = (dx * dx + dy * dy) <= r2 ? Color.White : Color.Transparent;
            }
        }

        var t = new Texture2D(_graphicsDevice, diam, diam);
        t.SetData(data);
        _circleCache[radius] = t;
        return t;
    }

    private void DrawCircle(Vector2 center, int radius, Color color)
    {
        var tex = GetCircle(radius);
        var pos = new Vector2(center.X - radius, center.Y - radius);
        _spriteBatch.Begin();
        _spriteBatch.Draw(tex, pos, color);
        _spriteBatch.End();

    }


}
