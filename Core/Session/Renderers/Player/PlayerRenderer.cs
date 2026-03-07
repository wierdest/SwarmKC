using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SwarmKC.Core.Session.Renderers.Shaders;

namespace SwarmKC.Core.Session.Renderers.Player;

public sealed class PlayerRenderer(
    SpriteBatch spriteBatch,
    PlayerShader shader,
    Texture2D pixel, 
    bool ownsShader = false) : IDisposable
{
    private readonly SpriteBatch _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
    private readonly PlayerShader _shader = shader ?? throw new ArgumentNullException(nameof(shader));
    private readonly bool _ownsShader = ownsShader;
    private readonly Texture2D _pixel = pixel;
    private Vector2 _lastPosition;
    private float _lastTimeSeconds;
    private bool _hasLastFrame;

    public PlayerRenderer(SpriteBatch spriteBatch, ContentManager content, Texture2D pixel)
        : this(spriteBatch, PlayerShader.Load(content), pixel, ownsShader: true)
    {
    }

    public void ApplyPlayerProfile(PlayerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _shader.SetBaseColor(profile.BaseColor, profile.BaseColorIntensity);
        _shader.SetRadianceColor(profile.RadianceColor, profile.RadianceColorIntensity);
        _shader.SetParticleCount(profile.ParticleCount);
        _shader.SetParticleSpinSpeed(profile.ParticleSpeed);
    }

    public void SetParticleCount(float count)
    {
        _shader.SetParticleCount(count);
    }

    public void Draw(Vector2 position, float radius, float rotationRadians, float timeSeconds)
    {
        float safeRadius = Math.Max(0.5f, radius);
        int diameter = Math.Max(1, (int)MathF.Ceiling(safeRadius * 2f));
        int x = (int)MathF.Round(position.X - safeRadius);
        int y = (int)MathF.Round(position.Y - safeRadius);
        var destination = new Rectangle(x, y, diameter, diameter);

        _shader.SetTexture(_pixel);
        _shader.SetTime(timeSeconds);
        _shader.SetPosition(position);
        _shader.SetRadius(safeRadius);
        _shader.SetRotation(rotationRadians);
        _shader.SetVelocity(ComputeVelocity(position, timeSeconds));

        _spriteBatch.Begin(
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp,
            depthStencilState: DepthStencilState.None,
            rasterizerState: RasterizerState.CullNone,
            effect: _shader.Effect);

        _spriteBatch.Draw(_pixel, destination, Color.White);
        _spriteBatch.End();
    }

    private Vector2 ComputeVelocity(Vector2 position, float timeSeconds)
    {
        if (!_hasLastFrame)
        {
            _lastPosition = position;
            _lastTimeSeconds = timeSeconds;
            _hasLastFrame = true;
            return Vector2.Zero;
        }

        float dt = timeSeconds - _lastTimeSeconds;
        if (dt <= 0.0001f || dt > 0.25f)
        {
            _lastPosition = position;
            _lastTimeSeconds = timeSeconds;
            return Vector2.Zero;
        }

        Vector2 velocity = (position - _lastPosition) / dt;
        _lastPosition = position;
        _lastTimeSeconds = timeSeconds;
        return velocity;
    }

    public void Dispose()
    {
        _pixel.Dispose();
        if (_ownsShader)
            _shader.Dispose();
    }
}
