using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwarmKC.UI.Components;

namespace SwarmKC.UI.Screens;

public sealed class Loading(SpriteFont font, GraphicsDevice graphicsDevice)
{
    private readonly SpriteFont _font = font;
    private readonly GraphicsDevice _graphicsDevice = graphicsDevice;
    private readonly ProgressBar _bar = new(Rectangle.Empty, value: 0f, indeterminate: true);

    private int _lastW = -1;
    private int _lastH = -1;

    private bool _started;
    private bool _completed;
    private float _elapsed;
    private float _minDuration = 1.25f;

    public bool IsCompleted => _completed;
    public string Message { get; private set; } = "Loading...";

    public void Begin(string? message = null, float minDurationSeconds = 1.25f)
    {
        _started = true;
        _completed = false;
        _elapsed = 0f;
        _minDuration = Math.Max(0.1f, minDurationSeconds);
        Message = string.IsNullOrWhiteSpace(message) ? "Loading session..." : message;
    }

    public void Update(GameTime gameTime, bool backendLoadFinished)
    {
        if (!_started || _completed) return;

        _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (backendLoadFinished && _elapsed >= _minDuration)
            _completed = true;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, GameTime gameTime)
    {
        EnsureLayout();

        spriteBatch.Draw(pixel, _graphicsDevice.Viewport.Bounds, Theme.Background);

        const string title = "Preparing Battlefield";
        var titleSize = _font.MeasureString(title);
        var msgSize = _font.MeasureString(Message);

        float cx = _graphicsDevice.Viewport.Width * 0.5f;
        spriteBatch.DrawString(_font, title, new Vector2(cx - titleSize.X * 0.5f, 160f), Theme.Text);
        spriteBatch.DrawString(_font, Message, new Vector2(cx - msgSize.X * 0.5f, 210f), Theme.TextMuted);

        _bar.Draw(spriteBatch, pixel, gameTime);
    }

    private void EnsureLayout()
    {
        int w = _graphicsDevice.Viewport.Width;
        int h = _graphicsDevice.Viewport.Height;
        if (w == _lastW && h == _lastH) return;

        _lastW = w;
        _lastH = h;

        const int barWidth = 320;
        const int barHeight = 16;
        int x = (w - barWidth) / 2;
        int y = (int)(h * 0.55f);

        _bar.SetBounds(new Rectangle(x, y, barWidth, barHeight));
    }


}
