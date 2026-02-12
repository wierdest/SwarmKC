using Microsoft.Xna.Framework.Input;

namespace SwarmKC.Common;

public static class InputHelpers
{
     public static bool JustPressed(Keys key, KeyboardState current, KeyboardState prev)
        => current.IsKeyDown(key) && !prev.IsKeyDown(key);

    public static bool JustPressed(Buttons button, GamePadState current, GamePadState prev)
        => current.IsButtonDown(button) && !prev.IsButtonDown(button);

    public static bool JustClicked(MouseState current, MouseState prev)
        => current.LeftButton == ButtonState.Pressed && prev.LeftButton == ButtonState.Released;

    public static bool JustReleased(MouseState current, MouseState prev)
        => current.LeftButton == ButtonState.Released && prev.LeftButton == ButtonState.Pressed;
}
