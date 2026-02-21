using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SwarmKC.Common;
using SwarmKC.Common.Graphics;
using SwarmKC.Core;
using SwarmKC.Core.Session;
using SwarmKC.Core.Session.Renderers;
using SwarmKC.Core.Session.Renderers.Background;
using SwarmKC.UI;
using SwarmKC.UI.Screens;

namespace SwarmKC;

public class SwarmKC : Game
{ 
    private readonly GraphicsDeviceManager _graphics;
    private readonly GameSessionManager _sessionManager;
    private int _appliedPresentationVersion = -1;

    private SpriteBatch _spriteBatch = null!;
    private PixelTexture _pixelTexture = null!;
    private SpriteFont _font = null!;

    private Title _titleScreen = null!;
    private Loading _loadingScreen = null!;
    private GameSessionRenderer _gameSessionRenderer = null!;
    private BackgroundRenderer _backgroundRenderer = null!;

    private States _state = States.TITLE;

    private KeyboardState _prevKb;

    public SwarmKC(GameSessionManager sessionManager)
    {
        Content.RootDirectory = "Content";
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _graphics = new GraphicsDeviceManager(this);
    }
    
    protected override void Initialize()
    {
        _sessionManager.LoadActiveConfig(Content.RootDirectory);

        _graphics.PreferredBackBufferWidth = (int)_sessionManager.StageWidth;
        _graphics.PreferredBackBufferHeight = (int)_sessionManager.StageHeight;
        _graphics.IsFullScreen = true;
        _graphics.ApplyChanges();

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _font = Content.Load<SpriteFont>("DefaultFont");
        _pixelTexture = new PixelTexture(GraphicsDevice);

        _titleScreen = new Title(_font, GraphicsDevice);
        _loadingScreen = new Loading(_font, GraphicsDevice);
        
        _backgroundRenderer = new BackgroundRenderer(GraphicsDevice, _spriteBatch, Content);
        _backgroundRenderer.ApplyBackgroundProfile(BackgroundProfiles.Light);

        Window.ClientSizeChanged += (_, __) => _gameSessionRenderer?.OnViewportChanged();
        
        SetState(States.TITLE);

        TryApplySessionPresentationConfig();

        base.LoadContent();
    }

    private void BeginSessionLoad()
    {
        _loadingScreen.Begin("Loading session config...");
        _sessionManager.BeginLoad(Content.RootDirectory);
    }
    
    protected override void Update(GameTime gameTime)
    {
        if (HandleGlobalEsc()) return;

        switch (_state)
        {
            case States.TITLE:
                _titleScreen.Update();

                if (_titleScreen.QuitRequested)
                {
                    Exit();
                    return;
                }

                if (_titleScreen.GoToLoadingRequested)
                {
                    _titleScreen.ResetFlags();
                    BeginSessionLoad();
                    SetState(States.LOADING);
                }
                return;

            case States.LOADING:
                _sessionManager.PollLoad();

                if (_sessionManager.HasLoadError)
                {
                    _titleScreen.ResetFlags();
                    SetState(States.TITLE);
                    return;
                }
                
                _loadingScreen.Update(gameTime, _sessionManager.IsLoadCompleted);

                if (_loadingScreen.IsCompleted)
                {
                    TryApplySessionPresentationConfig();
                    SetState(States.PLAYING);
                }
                return;

            case States.PLAYING:
            {
                var dt = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.05f);
                _sessionManager.UpdatePlaying(dt, Content.RootDirectory);
                TryApplySessionPresentationConfig();
                return;
            }
      
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        switch (_state)
        {
            case States.TITLE:

                _spriteBatch.Begin();
                _titleScreen.Draw(_spriteBatch, _pixelTexture.Value);
                _spriteBatch.End();
                return;

            case States.LOADING:
                _spriteBatch.Begin();
                _loadingScreen.Draw(_spriteBatch, _pixelTexture.Value, gameTime);
                _spriteBatch.End();
                return;

            case States.PLAYING:
                if (!_sessionManager.HasSession)
                {
                    GraphicsDevice.Clear(Color.Black);
                    return;
                }
                _backgroundRenderer.Draw((float)gameTime.TotalGameTime.TotalSeconds);
                _gameSessionRenderer.Draw(_sessionManager.GetSnapshot());
                return;
        }
       
        base.Draw(gameTime);
    }

    private void TryApplySessionPresentationConfig()
    {
        if (_appliedPresentationVersion == _sessionManager.PresentationConfigVersion)
            return;

        ApplySessionPresentationConfig();
        _appliedPresentationVersion = _sessionManager.PresentationConfigVersion;
    }

    private void ApplySessionPresentationConfig()
    {
        int w = (int)_sessionManager.StageWidth;
        int h = (int)_sessionManager.StageHeight;

        if (_graphics.PreferredBackBufferWidth != w || _graphics.PreferredBackBufferHeight != h)
        {
            _graphics.PreferredBackBufferWidth = w;
            _graphics.PreferredBackBufferHeight = h;
            _graphics.ApplyChanges();
        }

        CreateOrRecreateSessionRenderer();
    }

    private void CreateOrRecreateSessionRenderer()
    {
        _gameSessionRenderer = new GameSessionRenderer(
            _spriteBatch,
            GraphicsDevice,
            _font,
            _pixelTexture,
            _sessionManager.StageWidth,
            _sessionManager.StageHeight,
            _sessionManager.BorderSize);

        _gameSessionRenderer.Initialize();
    }

    private void SetState(States next)
    {
        _state = next;
        IsMouseVisible = next == States.TITLE || next == States.LOADING;
    }

    private bool HandleGlobalEsc()
    {
        var kb = Keyboard.GetState();
        bool escPressed = InputHelpers.JustPressed(Keys.Escape, kb, _prevKb);
        _prevKb = kb;

        if (!escPressed) return false;

        if (_state == States.PLAYING)
        {
            _titleScreen.ResetFlags();
            SetState(States.TITLE);
            return true;
        }

        if (_state == States.LOADING)
        {
            SetState(States.TITLE);
            return true;
        }

        if (_state == States.TITLE)
        {
            Exit();
            return true;
        }

        return false;
    }

    protected override void UnloadContent()
    {
        _backgroundRenderer?.Dispose();
        base.UnloadContent();
    }

}
