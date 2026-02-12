using Microsoft.Xna.Framework;

namespace SwarmKC.UI;

public static class Theme
{
   public static readonly Color Background = new(8, 8, 8);

    public static readonly Color Text = Color.White;
    public static readonly Color TextMuted = new(180, 180, 180);
    public static readonly Color TextHighlight = Color.Yellow;

    public static readonly Color ButtonNormal = new(30, 30, 30);
    public static readonly Color ButtonHovered = new(60, 60, 60);
    public static readonly Color ButtonPressed = new(90, 90, 90);
    public static readonly Color ButtonText = Color.White;
    public static readonly Color ButtonTextHover = Color.Yellow;

    public static readonly Color ProgressBarTrack = new(25, 25, 25);
    public static readonly Color ProgressBarFill = new(120, 210, 140);
    public static readonly Color ProgressBarOutline = new(90, 90, 90);

    public static readonly Color ProgressBarIndeterminate = new(120, 180, 230);

}
