using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SwarmKC.Core.Session.Renderers.Shaders;

public sealed class HelloWorldShader : IDisposable
{
    public const string DefaultAssetName = "Shaders/HelloWorld";

    private readonly Effect _effect;
    public Effect Effect => _effect;
    private readonly EffectParameter? targetTexture;
    private readonly EffectParameter? _time;
    private readonly bool _ownsEffect;

    public HelloWorldShader(Effect effect, bool cloneEffect = true)
    {
        ArgumentNullException.ThrowIfNull(effect);
        _effect = cloneEffect ? effect.Clone() : effect;
        targetTexture = _effect.Parameters["TargetTexture"];
        _time = _effect.Parameters["Time"];
        _ownsEffect = cloneEffect;
    }

    public static HelloWorldShader Load(ContentManager content, string assetName = DefaultAssetName, bool cloneEffect = true)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new HelloWorldShader(content.Load<Effect>(assetName), cloneEffect);
    }

    public void SetTexture(Texture2D texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        targetTexture?.SetValue(texture);
    }

    public void SetTime(float seconds)
    {
        _time?.SetValue(seconds);
    }
    
    public void Dispose()
    {
        if (_ownsEffect)
            _effect.Dispose();
    }
}
