using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Swarm.Application.Contracts;
using SwarmKC.Common.Graphics;
using SwarmKC.Core.Session.Renderers.Background;
using SwarmKC.Core.Session.Renderers.Player;
using SwarmKC.UI.Components.Hud;

namespace SwarmKC.Core.Session.Renderers;

public sealed class GameSessionRenderer(
   SpriteBatch spriteBatch,
   GraphicsDevice graphicsDevice,
   SpriteFont font,
   PixelTexture pixelTexture,
   BackgroundRenderer backgroundRenderer,
   PlayerRenderer playerRenderer,
   float width,
   float height,
   int border) : IDisposable
{
    private readonly SpriteBatch _spriteBatch = spriteBatch;
    private readonly GraphicsDevice _graphicsDevice = graphicsDevice;
    private readonly SpriteFont _font = font;
    private readonly HudRenderer _hud = new(spriteBatch, font, graphicsDevice);
    private readonly CrosshairRenderer _crosshairRenderer = new(spriteBatch, graphicsDevice);
    private readonly BackgroundRenderer _backgroundRenderer = backgroundRenderer;
    private readonly PlayerRenderer _playerRenderer = playerRenderer;
    private readonly Texture2D _pixel = pixelTexture.Value;
    private readonly Dictionary<int, Texture2D> _circleCache = new();
    private Rectangle _drawDestination;
    private readonly float _width = width;
    private readonly float _height = height;
    private readonly int _border = border;

    public void Initialize()
    {
        RecalculateDestination();
        _backgroundRenderer.ApplyBackgroundProfile(BackgroundProfiles.Dark);
        _playerRenderer.ApplyPlayerProfile(PlayerProfiles.LightHeart);
    }

    public void OnViewportChanged() => RecalculateDestination();

    public void Draw(GameSnapshot snap, float gameTime)
    {
        _backgroundRenderer.Draw(gameTime);

        _spriteBatch.Begin();

        foreach (var wall in snap.Walls)
        {
            DrawRect(new Rectangle(
                (int)(wall.X - wall.Radius),
                (int)(wall.Y - wall.Radius),
                (int)(wall.Radius * 2),
                (int)(wall.Radius * 2)),
                Color.Gray);
        }

        var pa = snap.PlayerArea;
        DrawRect(new Rectangle(
            (int)(pa.X - pa.Radius),
            (int)(pa.Y - pa.Radius),
            (int)(pa.Radius * 2),
            (int)(pa.Radius * 2)),
            Color.Blue);

        var ta = snap.TargetArea;
        DrawRect(new Rectangle(
            (int)(ta.X - ta.Radius),
            (int)(ta.Y - ta.Radius),
            (int)(ta.Radius * 2),
            (int)(ta.Radius * 2)),
            snap.TargetAreaIsOpenToPlayer ? Color.SeaGreen : Color.OrangeRed);

        foreach (var p in snap.Projectiles)
            DrawCircle(new Vector2(p.X, p.Y), (int)p.Radius, Color.OrangeRed);

        foreach (var e in snap.Enemies)
            DrawEnemies(
                new Vector2(e.X, e.Y),
                (int)e.Radius,
                e.RotationAngle,
                GetColorForNonPlayerEntityType(e.Type));

        DrawOverlayTextIfNeeded(snap);

        _spriteBatch.End();

        DrawPlayer(
            new Vector2(snap.Player.X, snap.Player.Y),
            snap.Player.Radius,
            snap.Player.RotationAngle,
            gameTime
        );

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawBorder(_drawDestination, _border, Color.Black);

        _hud.Draw(snap.GameSessionData);
        _crosshairRenderer.Draw(snap.AimPositionX, snap.AimPositionY);

        _spriteBatch.End();

    }

    private void RecalculateDestination()
    {
        int screenW = _graphicsDevice.PresentationParameters.BackBufferWidth;
        int screenH = _graphicsDevice.PresentationParameters.BackBufferHeight;

        int drawW = (int)_width;
        int drawH = (int)_height;

        int drawX = (screenW - drawW) / 2;
        int drawY = (screenH - drawH) / 2;

        _drawDestination = new Rectangle(drawX, drawY, drawW, drawH);
    }

    private void DrawOverlayTextIfNeeded(GameSnapshot snap)
    {
        if (!(snap.IsPaused || snap.IsInterrupted || snap.IsTimeUp || snap.IsCompleted))
            return;

        string mainText = "";
        string continueText = "PRESS R TO RUN TO THE NEXT SESSION";
        string replayText = "PRESS F6 TO REPLAY THIS SESSION";
        string resetText = "PRESS F8 TO RESET PROGRESS";
        string navText = "PRESS F9/F10 TO PREV/NEXT CONFIG";

        if (snap.IsPaused) mainText = "PAUSED";
        else if (snap.IsInterrupted) mainText = "GAME OVER";
        else if (snap.IsTimeUp) mainText = "TIME UP";

        if (!string.IsNullOrEmpty(mainText))
        {
            Vector2 size = _font.MeasureString(mainText);
            Vector2 pos = new((_width - size.X) / 2f, (_height - size.Y) / 2f);
            _spriteBatch.DrawString(_font, mainText, pos, Color.White);
        }

        Vector2 mainSize = string.IsNullOrEmpty(mainText) ? Vector2.Zero : _font.MeasureString(mainText);

        Vector2 subSize = _font.MeasureString(continueText);
        Vector2 subPos = new((_width - subSize.X) / 2f, (_height - mainSize.Y) / 2f + mainSize.Y + 10);
        _spriteBatch.DrawString(_font, continueText, subPos, Color.White);

        Vector2 resetSize = _font.MeasureString(resetText);
        Vector2 resetPos = new((_width - resetSize.X) / 2f, subPos.Y + resetSize.Y + 10);
        _spriteBatch.DrawString(_font, resetText, resetPos, Color.White);

        Vector2 replaySize = _font.MeasureString(replayText);
        Vector2 replayPos = new((_width - replaySize.X) / 2f, resetPos.Y + replaySize.Y + 10);
        _spriteBatch.DrawString(_font, replayText, replayPos, Color.White);

        Vector2 navSize = _font.MeasureString(navText);
        Vector2 navPos = new((_width - navSize.X) / 2f, replayPos.Y + navSize.Y + 10);
        _spriteBatch.DrawString(_font, navText, navPos, Color.White);
    }

    private static Color GetColorForNonPlayerEntityType(string type)
    {
        return type switch
        {
            "Shooter" => Color.Purple,
            "Healthy" => Color.Orange,
            _ => Color.Yellow
        };
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
        _spriteBatch.Draw(tex, pos, color);
    }

    private void DrawPlayer(Vector2 pos, float radius, float rotation, float time)
    {
        _playerRenderer.Draw(pos, radius, rotation, time);
    }

    private void DrawEnemies(Vector2 pos, int radius, float rotation, Color color)
    {
        _spriteBatch.Draw(
            _pixel,
            position: pos,
            sourceRectangle: null,
            color: color,
            rotation: rotation,
            origin: new Vector2(0.5f, 0.5f),
            scale: new Vector2(radius * 2f, radius * 2f),
            effects: SpriteEffects.None,
            layerDepth: 0f);
    }

    private void DrawRect(Rectangle rect, Color color)
    {
        _spriteBatch.Draw(_pixel, rect, color);
    }

    private void DrawBorder(Rectangle rect, int baseThickness, Color color)
    {
        float scale = rect.Width / _width;
        int thickness = Math.Max(1, (int)(baseThickness * scale));

        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    public void Dispose()
    {
        _backgroundRenderer.Dispose();
        _playerRenderer.Dispose();
    }
}
