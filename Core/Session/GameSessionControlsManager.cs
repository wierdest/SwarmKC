using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Swarm.Application.DTOs;
using SwarmKC.Common;

namespace SwarmKC.Core.Session;

public sealed class GameSessionControlsManager
{
    private KeyboardState _prevKb;
    private MouseState _prevMouse;
    private GamePadState _prevPad;
    private float? _aimAngleRadians = null;
    public float _aimMagnitude = 0f;
    private Vector2 _smoothedRightStick = Vector2.Zero;
    private const float _aimSmoothness = 0.2f;

    public GameSessionControlsDTO Update()
    {
        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();
        var pad = GamePad.GetState(PlayerIndex.One);

        float dx = (kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.D) ? 1f : 0f)
                 - (kb.IsKeyDown(Keys.Left) || kb.IsKeyDown(Keys.A) ? 1f : 0f);

        float dy = (kb.IsKeyDown(Keys.Down) || kb.IsKeyDown(Keys.S) ? 1f : 0f)
                 - (kb.IsKeyDown(Keys.Up) || kb.IsKeyDown(Keys.W) ? 1f : 0f);

        dx += pad.ThumbSticks.Left.X;
        dy -= pad.ThumbSticks.Left.Y;

        bool firePressed =
            InputHelpers.JustPressed(Keys.Space, kb, _prevKb) ||
            (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released) ||
            InputHelpers.JustPressed(Buttons.RightTrigger, pad, _prevPad);

        bool fireHeld =
            kb.IsKeyDown(Keys.Space) ||
            mouse.LeftButton == ButtonState.Pressed ||
            pad.Triggers.Right > 0.3f;

        bool dropBomb =
              InputHelpers.JustPressed(Keys.Q, kb, _prevKb) ||
              InputHelpers.JustPressed(Buttons.A, pad, _prevPad);

        bool reload =
              InputHelpers.JustPressed(Keys.E, kb, _prevKb) ||
              InputHelpers.JustPressed(Buttons.X, pad, _prevPad);

        bool pause =
            InputHelpers.JustPressed(Keys.P, kb, _prevKb) ||
            InputHelpers.JustPressed(Buttons.Start, pad, _prevPad);

        bool next =
            InputHelpers.JustPressed(Keys.R, kb, _prevKb) ||
            InputHelpers.JustPressed(Buttons.Y, pad, _prevPad);

        bool replay =
            InputHelpers.JustPressed(Keys.F6, kb, _prevKb) ||
            InputHelpers.JustPressed(Buttons.LeftShoulder, pad, _prevPad);
        
        bool reset =
            InputHelpers.JustPressed(Keys.F8, kb, _prevKb) ||
            InputHelpers.JustPressed(Buttons.RightShoulder, pad, _prevPad);
        
        bool navigateNextConfig = InputHelpers.JustPressed(Keys.F10, kb, _prevKb);

        bool navigatePrevConfig = InputHelpers.JustPressed(Keys.F9, kb, _prevKb);

            
        if (pad.IsConnected)
        {
            Vector2 rightStick = pad.ThumbSticks.Right;
            rightStick.Y *= -1f; // invert Y for screen coordinates

            _smoothedRightStick = Vector2.Lerp(_smoothedRightStick, rightStick, _aimSmoothness);

            if (_smoothedRightStick.LengthSquared() > 0.15f)
            {
                _aimMagnitude = MathHelper.Clamp(_smoothedRightStick.Length(), 0f, 1f);
                _aimAngleRadians = (float)Math.Atan2(_smoothedRightStick.Y, _smoothedRightStick.X);
            }
            else
            {
                _aimMagnitude = 0f;
                _aimAngleRadians = null;
            }
        }

        var state = new GameSessionControlsDTO(
            dx,
            dy,
            mouse.X,
            mouse.Y,
            _aimAngleRadians,
            _aimMagnitude,
            firePressed,
            fireHeld,
            dropBomb,
            reload,
            pause,
            false,
            false,
            next,
            reset,
            replay,
            navigateNextConfig,
            navigatePrevConfig
        );

        _prevKb = kb;
        _prevMouse = mouse;
        _prevPad = pad;

        return state;
    }

}
