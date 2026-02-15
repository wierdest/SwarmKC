using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SwarmKC.Common.Graphics;

public sealed class PixelTexture(GraphicsDevice graphicsDevice)
{
    private readonly GraphicsDevice _graphicsDevice = graphicsDevice;
    private Texture2D? _pixel;

    public Texture2D Value
    {
        get
        {
            if (_pixel is null)
            {
                _pixel = new Texture2D(_graphicsDevice, 1, 1);
                _pixel.SetData([Color.White]);
            }
            return _pixel;
        }
    }
}
