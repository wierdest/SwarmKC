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
using SwarmKC.Core.Session.Renderers.Player;
using SwarmKC.UI.Screens;

namespace SwarmKC;

public class SwarmKC : Game
{ 
    private readonly GraphicsDeviceManager _graphics;
    private readonly GameSessionManager _sessionManager;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;
    private Title _titleScreen = null!;
    private Loading _loadingScreen = null!;
    private GameSessionRenderer _gameSessionRenderer = null!;
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
        _pixel = new PixelTexture(GraphicsDevice).Value;

        _titleScreen = new Title(_font, GraphicsDevice);
        _loadingScreen = new Loading(_font, GraphicsDevice);
        

        Window.ClientSizeChanged += (_, __) => _gameSessionRenderer?.OnViewportChanged();
        
        SetState(States.TITLE);

        ApplySessionPresentationConfig();

        // Profiles decision should come from game session configs or from saved settings.
        // these should be passed in a yet to be created data structure GameSessionSettings
        _gameSessionRenderer = new GameSessionRenderer(
            _spriteBatch,
            GraphicsDevice,
            _font,
            _pixel,
            new BackgroundRenderer(GraphicsDevice, _spriteBatch, Content, _pixel),
            new PlayerRenderer(GraphicsDevice, _spriteBatch, Content, _pixel),
            _sessionManager.StageWidth,
            _sessionManager.StageHeight,
            _sessionManager.BorderSize);

        _gameSessionRenderer.Initialize();

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
                    SetState(States.PLAYING);
                }
                return;

            case States.PLAYING:
            {
                var dt = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.05f);
                _sessionManager.UpdatePlaying(dt, Content.RootDirectory);
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
                _titleScreen.Draw(_spriteBatch, _pixel);
                _spriteBatch.End();
                return;

            case States.LOADING:
                _spriteBatch.Begin();
                _loadingScreen.Draw(_spriteBatch, _pixel, gameTime);
                _spriteBatch.End();
                return;

            case States.PLAYING:
                if (!_sessionManager.HasSession)
                {
                    GraphicsDevice.Clear(Color.Black);
                    return;
                }
                _gameSessionRenderer.Draw(_sessionManager.GetSnapshot(), (float)gameTime.TotalGameTime.TotalSeconds);
                return;
        }
       
        base.Draw(gameTime);
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
        _gameSessionRenderer.Dispose();
        base.UnloadContent();
    }

}
