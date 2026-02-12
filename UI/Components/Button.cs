using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SwarmKC.UI.Components;

public sealed class Button(string text, Rectangle bounds)
{
    public string Text { get; } = text;
    public Rectangle Bounds { get; private set; } = bounds;
    public bool IsHovered { get; private set; }
    public bool IsPressed { get; private set; }
    public void SetBounds(Rectangle bounds) => Bounds = bounds;

    public bool Update(Point mousePos, bool mouseDown, bool mouseClicked)
    {
        IsHovered = Bounds.Contains(mousePos);

        if (!IsHovered)
        {
            IsPressed = false;
            return false;
        }

        if (mouseDown) IsPressed = true;

        if (mouseClicked)
        {
            IsPressed = false;
            return true;
        }

        return false;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel)
    {
        var bg = IsPressed ? Theme.ButtonPressed : (IsHovered ? Theme.ButtonHovered : Theme.ButtonNormal);
        var fg = IsHovered ? Theme.ButtonTextHover : Theme.ButtonText;

        spriteBatch.Draw(pixel, Bounds, bg);

        var size = font.MeasureString(Text);
        var pos = new Vector2(
            Bounds.X + (Bounds.Width - size.X) / 2f,
            Bounds.Y + (Bounds.Height - size.Y) / 2f
        );

        spriteBatch.DrawString(font, Text, pos, fg);
    }
}
