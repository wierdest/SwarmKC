using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SwarmKC.UI.Components;

public class ProgressBar(Rectangle bounds, float value = 0f, bool indeterminate = false, bool reversed = false)
{
    public Rectangle Bounds { get; private set; } = bounds;
    public float Value { get; private set; } = MathHelper.Clamp(value, 0f, 1f);
    public bool IsIndeterminate { get; private set; } = indeterminate;
    public float IndeterminateSpeed { get; set; } = 0.8f;
    public float IndeterminateSegmentFraction { get; set; } = 0.25f;
    public bool IsReversed { get; private set; } = reversed;
    public void SetBounds(Rectangle bounds) => Bounds = bounds;
    public void SetValue(float value) => Value = MathHelper.Clamp(value, 0f, 1f);

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, GameTime gameTime)
    {
        spriteBatch.Draw(pixel, Bounds, Theme.ProgressBarTrack);

        if (IsIndeterminate)
        {
            DrawIndeterminate(spriteBatch, pixel, gameTime);
        }
        else
        {
            DrawDeterminate(spriteBatch, pixel);
        }

        DrawOutline(spriteBatch, pixel, Bounds, Theme.ProgressBarOutline);
    }

    private void DrawDeterminate(SpriteBatch spriteBatch, Texture2D pixel)
    {
        int fillWidth = (int)(Bounds.Width * Value);
        if (fillWidth <= 0) return;

        var fillRect = IsReversed
            ? new Rectangle(Bounds.Right - fillWidth, Bounds.Y, fillWidth, Bounds.Height)
            : new Rectangle(Bounds.X, Bounds.Y, fillWidth, Bounds.Height);
        
        spriteBatch.Draw(pixel, fillRect, Theme.ProgressBarFill);
    }

    private void DrawIndeterminate(SpriteBatch spriteBatch, Texture2D pixel, GameTime gameTime)
    {
        float t = (float)gameTime.TotalGameTime.TotalSeconds;
        float cycle = (t * IndeterminateSpeed) % 1f;

        int segWidth = (int)(Bounds.Width * IndeterminateSegmentFraction);
        if (segWidth < 6) segWidth = 6;

        int travel = Bounds.Width + segWidth;
        int x = Bounds.X + (int)(cycle * travel) - segWidth;

        int left = Math.Max(Bounds.X, x);
        int right = Math.Min(Bounds.X + Bounds.Width, x + segWidth);
        int w = right - left;

        if (w > 0)
        {
            var segRect = new Rectangle(left, Bounds.Y, w, Bounds.Height);
            spriteBatch.Draw(pixel, segRect, Theme.ProgressBarIndeterminate);
        }
    }

    private static void DrawOutline(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y + rect.Height - 1, rect.Width, 1), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X + rect.Width - 1, rect.Y, 1, rect.Height), color);
    }

}
