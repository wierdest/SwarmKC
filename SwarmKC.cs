using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Swarm.Application.Contracts;
using SwarmKC.Common.Graphics;
using SwarmKC.Core;
using SwarmKC.Core.Session;
using SwarmKC.Core.Session.Renderers;
using SwarmKC.UI;
using SwarmKC.UI.Screens;

namespace SwarmKC;

public class SwarmKC : Game
{ 
    private readonly GraphicsDeviceManager _graphics;
    private PixelTexture _pixelTexture = null!;
    private SpriteBatch _spriteBatch = null!;
    private readonly IGameSessionService _service;
    private States _state = States.TITLE;
    private Title _titleScreen = null!;
    private Loading _loadingScreen = null!;
    private Task? _sessionLoadTask;
    private Exception? _loadError;
    private readonly float _moveSpeed = 360f;
    private SpriteFont _font = null!;
    private readonly GameSessionControlsManager _input;
    private string? _gameConfigJson;
    private bool _manifestSaved;
    private readonly float WIDTH = 960f;
    private readonly float HEIGHT = 540f;
    private readonly int BORDER = 40;
    private GameSessionRenderer _gameSessionRenderer = null!;
    private readonly IGameSessionConfigSource _configSource;
    public SwarmKC(IGameSessionService service, IGameSessionConfigSource configSource)
    {
        Content.RootDirectory = "Content";
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = (int)WIDTH,
            PreferredBackBufferHeight = (int)HEIGHT
        };
        _service = service;
        _configSource = configSource ?? throw new ArgumentNullException(nameof(configSource));
        _input = new GameSessionControlsManager();
        _graphics.IsFullScreen = true;
        _graphics.ApplyChanges();

    }
    
    protected override void Initialize()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _font = Content.Load<SpriteFont>("DefaultFont");

        _pixelTexture = new PixelTexture(GraphicsDevice);

        _titleScreen = new Title(_font, GraphicsDevice);

        _loadingScreen = new Loading(_font, GraphicsDevice);

        _gameSessionRenderer = new GameSessionRenderer(_spriteBatch, GraphicsDevice, _font, _pixelTexture, WIDTH, HEIGHT, BORDER);
        
        _gameSessionRenderer.Initialize();
        
        Window.ClientSizeChanged += (_, __) => _gameSessionRenderer.OnViewportChanged();

        SetState(States.TITLE);

        base.LoadContent();
    }

    private void SetState(States next)
    {
        _state = next;
        IsMouseVisible = next == States.TITLE || next == States.LOADING;
    }

    private void BeginSessionLoad()
    {
        _sessionLoadTask = null;
        _loadError = null;
        _gameConfigJson = _configSource.LoadConfigJson(Content.RootDirectory);
        _manifestSaved = false;

        _loadingScreen.Begin("Loading session config...");
        _sessionLoadTask = Task.Run(() => _service.StartNewSession(_gameConfigJson!).GetAwaiter().GetResult());
    }

    private bool IsSessionReady() => _service != null && _service.HasSession;
    
    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape) && _state == States.TITLE)
            Exit();
        
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
                bool backendFinished = _sessionLoadTask is { IsCompleted: true };

                if (backendFinished && _sessionLoadTask!.IsFaulted)
                {
                    _loadError = _sessionLoadTask.Exception;
                    _titleScreen.ResetFlags();
                    SetState(States.TITLE);
                    return;
                }

                _loadingScreen.Update(gameTime, backendFinished && _loadError is null);

                if (_loadingScreen.IsCompleted)
                    SetState(States.PLAYING);

                return;

            case States.PLAYING:
                UpdatePlaying(gameTime); // move your current gameplay Update body here
                return;
        }

        base.Update(gameTime);
    }
    private void UpdatePlaying(GameTime gameTime)
    {
        if (_service is null) return;

        if (!IsSessionReady())
            return;

        var kb = Keyboard.GetState();
        if (kb.IsKeyDown(Keys.Escape)) Exit();

        var snap = _service.GetSnapshot();

        var state = _input.Update();

        if (state.Reset)
        {
            ResetManifestProgress();
            _gameConfigJson = _configSource.LoadConfigJson(Content.RootDirectory);
            _manifestSaved = false;
            _service.Restart(_gameConfigJson!);
            return;
        }

        if (state.NavigateNextConfig || state.NavgatePrevConfig)
        {
            NavigateConfig(state.NavigateNextConfig ? 1 : -1);
            return;
        }
        
        if (state.Replay)
        {
            if (!string.IsNullOrWhiteSpace(_gameConfigJson))
                _service.Restart(_gameConfigJson);
            return;
        }

        if (state.Pause)
        {
            if (snap.IsPaused)
                _service.Resume();
            else
                _service.Pause();

            snap = _service.GetSnapshot();
        }

        if (snap.IsPaused || snap.IsTimeUp || snap.IsCompleted || snap.IsInterrupted)
        {
            if (snap.IsCompleted && !_manifestSaved)
            {
                var manifest = _configSource.LoadManifest(Content.RootDirectory);
                var index = _configSource.SelectEntryIndex(manifest);
                if (index >= 0 && index < manifest.Entries.Count)
                {
                    manifest.Entries[index].Completed = true;
                    _configSource.SaveManifest(Content.RootDirectory, manifest);
                }
                _manifestSaved = true;
            }
            if (state.Next)
            {
                _gameConfigJson = _configSource.LoadConfigJson(Content.RootDirectory);
                _manifestSaved = false;
                _service.Restart(_gameConfigJson!);
            }
            return;
        }

        _service.ApplyInput(state.DirX, state.DirY, (state.DirX == 0f && state.DirY == 0f) ? 0f : _moveSpeed);

        _service.Fire(state.FirePressed, state.FireHeld);

        if (state.DropBomb) _service.DropBomb();

        if (state.Reload) _service.Reload();

        _service.RotateTowards(state.MouseX, state.MouseY, state.AimRadians, state.AimMagnitude);

        var dt = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.05f);

        if (dt > 0f) _service.Tick(dt);
    }

    protected override void Draw(GameTime gameTime)
    {
        switch (_state)
        {
            case States.TITLE:
                GraphicsDevice.Clear(Theme.Background);
                _spriteBatch.Begin();
                _titleScreen.Draw(_spriteBatch, _pixelTexture.Value);
                _spriteBatch.End();
                return;

            case States.LOADING:
                GraphicsDevice.Clear(Theme.Background);
                _spriteBatch.Begin();
                _loadingScreen.Draw(_spriteBatch, _pixelTexture.Value, gameTime);
                _spriteBatch.End();
                return;

            case States.PLAYING:
                if (!IsSessionReady())
                {
                    GraphicsDevice.Clear(Color.Black);
                    return;
                }
                _gameSessionRenderer.Draw(_service.GetSnapshot());
                return;
        }
       
        base.Draw(gameTime);
    }


    private void ResetManifestProgress()
    {
        var manifest = _configSource.LoadManifest(Content.RootDirectory);
        manifest.ActiveIndex = null;
        for (int i = 0; i < manifest.Entries.Count; i++)
        {
            manifest.Entries[i].Completed = false;
        }
        _configSource.SaveManifest(Content.RootDirectory, manifest);
    }

    private void NavigateConfig(int delta)
    {
        var manifest = _configSource.LoadManifest(Content.RootDirectory);
        if (manifest.Entries.Count == 0) return;

        int currentIndex = manifest.ActiveIndex is int idx && idx >= 0 && idx < manifest.Entries.Count
            ? idx
            : _configSource.SelectEntryIndex(manifest);

        int nextIndex = (currentIndex + delta) % manifest.Entries.Count;
        if (nextIndex < 0) nextIndex += manifest.Entries.Count;

        manifest.ActiveIndex = nextIndex;
        _configSource.SaveManifest(Content.RootDirectory, manifest);

        _gameConfigJson = LoadConfigJsonFromManifestEntry(manifest, nextIndex);
        _manifestSaved = false;
        _service.Restart(_gameConfigJson!);
    }

    private string LoadConfigJsonFromManifestEntry(GameSessionConfigManifest manifest, int index)
    {
        var entry = manifest.Entries[index];
        if (string.IsNullOrWhiteSpace(entry.File))
            throw new InvalidOperationException("Manifest entry must include a file path.");

        var contentRoot = Path.Combine(AppContext.BaseDirectory, Content.RootDirectory);
        var configPath = Path.Combine(contentRoot, entry.File);
        return File.ReadAllText(configPath);
    }

}
