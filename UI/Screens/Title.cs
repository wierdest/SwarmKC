using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SwarmKC.Common;
using SwarmKC.UI.Components;

namespace SwarmKC.UI.Screens;

public sealed class Title(SpriteFont font, GraphicsDevice graphicsDevice)
{
   private readonly SpriteFont _font = font;
    private readonly GraphicsDevice _graphicsDevice = graphicsDevice;
    private readonly Button _start = new("Start", Rectangle.Empty);
    private readonly Button _quit = new("Quit", Rectangle.Empty);

    private KeyboardState _prevKb;
    private MouseState _prevMouse;

    private int _lastW = -1;
    private int _lastH = -1;

    public bool GoToLoadingRequested { get; private set; }
    public bool QuitRequested { get; private set; }

    public void ResetFlags()
    {
        GoToLoadingRequested = false;
        QuitRequested = false;
    }

    public void Update()
    {
        EnsureLayout();

        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();

        bool down = mouse.LeftButton == ButtonState.Pressed;
        bool clicked = InputHelpers.JustClicked(mouse, _prevMouse);

        if (_start.Update(mouse.Position, down, clicked) || InputHelpers.JustPressed(Keys.Enter, kb, _prevKb))
        {
            GoToLoadingRequested = true;
        }

        if (_quit.Update(mouse.Position, down, clicked) || InputHelpers.JustPressed(Keys.Escape, kb, _prevKb))
            QuitRequested = true;

        _prevKb = kb;
        _prevMouse = mouse;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        EnsureLayout();

        spriteBatch.Draw(pixel, _graphicsDevice.Viewport.Bounds, Theme.Background);

        const string title = "SWARM: KILL CANCER";
        var size = _font.MeasureString(title);
        float cx = _graphicsDevice.Viewport.Width * 0.5f;
        spriteBatch.DrawString(_font, title, new Vector2(cx - size.X * 0.5f, 120f), Theme.Text);

        _start.Draw(spriteBatch, _font, pixel);
        _quit.Draw(spriteBatch, _font, pixel);
    }

    private void EnsureLayout()
    {
        int w = _graphicsDevice.Viewport.Width;
        int h = _graphicsDevice.Viewport.Height;
        if (w == _lastW && h == _lastH) return;

        _lastW = w;
        _lastH = h;

        const int bw = 240;
        const int bh = 50;
        const int gap = 12;

        int x = (w - bw) / 2;
        int y = (int)(h * 0.45f);

        _start.SetBounds(new Rectangle(x, y, bw, bh));
        _quit.SetBounds(new Rectangle(x, y + bh + gap, bw, bh));
    }
}
