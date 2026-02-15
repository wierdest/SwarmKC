using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Swarm.Application.Contracts;

namespace SwarmKC.Core.Session;

public class GameSessionManager(
    IGameSessionService service,
    IGameSessionConfigSource configSource
)
{
    private readonly IGameSessionService _service = service;
    private readonly IGameSessionConfigSource _configSource = configSource;
    private readonly GameSessionControlsManager _controls = new();

    private Task? _loadTask;
    private Exception? _loadError;
    private string? _gameConfigJson;
    private bool _manifestSaved;

    public float MoveSpeed { get; private set; }
    public float StageWidth { get; private set; }
    public float StageHeight { get; private set; }
    public int BorderSize { get; private set; }
    public int PresentationConfigVersion { get; private set; }

    public bool IsLoadCompleted => _loadTask is { IsCompleted: true } && _loadError is null;
    public bool HasLoadError => _loadError is not null;
    public Exception? LoadError => _loadError;
    public bool HasSession => _service.HasSession;

    public GameSnapshot GetSnapshot() => _service.GetSnapshot();

    public void BeginLoad(string contentRoot)
    {
        _loadTask = null;
        _loadError = null;

        _gameConfigJson = _configSource.LoadConfigJson(contentRoot);
        _manifestSaved = false;

        ApplyPresentationConfig(_gameConfigJson);

        _loadTask = Task.Run(() => _service.StartNewSession(_gameConfigJson).GetAwaiter().GetResult());
    }

    public void PollLoad()
    {
        if (_loadTask is not { IsCompleted: true }) return;
        if (_loadTask.IsFaulted) _loadError = _loadTask.Exception;
    }

    public void UpdatePlaying(float dt, string contentRoot)
    {
        if (!HasSession) return;

        var input = _controls.Update();

        var snap = _service.GetSnapshot();

        if (input.Reset)
        {
            ResetManifestProgress(contentRoot);
            _gameConfigJson = _configSource.LoadConfigJson(contentRoot);
            _manifestSaved = false;
            ApplyPresentationConfig(_gameConfigJson);
            _service.Restart(_gameConfigJson);
            return;
        }

        if (input.NavigateNextConfig || input.NavgatePrevConfig)
        {
            NavigateConfig(contentRoot, input.NavigateNextConfig ? 1 : -1);
            return;
        }

        if (input.Replay)
        {
            if (!string.IsNullOrWhiteSpace(_gameConfigJson))
                _service.Restart(_gameConfigJson);
            return;
        }

        if (input.Pause)
        {
            if (snap.IsPaused) _service.Resume();
            else _service.Pause();

            snap = _service.GetSnapshot();
        }

        if (snap.IsPaused || snap.IsTimeUp || snap.IsCompleted || snap.IsInterrupted)
        {
            if (snap.IsCompleted && !_manifestSaved)
            {
                var manifest = _configSource.LoadManifest(contentRoot);
                var index = _configSource.SelectEntryIndex(manifest);
                if (index >= 0 && index < manifest.Entries.Count)
                {
                    manifest.Entries[index].Completed = true;
                    _configSource.SaveManifest(contentRoot, manifest);
                }
                _manifestSaved = true;
            }

            if (input.Next)
            {
                _gameConfigJson = _configSource.LoadConfigJson(contentRoot);
                _manifestSaved = false;
                ApplyPresentationConfig(_gameConfigJson);
                _service.Restart(_gameConfigJson);
            }

            return;
        }

        _service.ApplyInput(input.DirX, input.DirY, (input.DirX == 0f && input.DirY == 0f) ? 0f : MoveSpeed);
        _service.Fire(input.FirePressed, input.FireHeld);
        if (input.DropBomb) _service.DropBomb();
        if (input.Reload) _service.Reload();
        _service.RotateTowards(input.MouseX, input.MouseY, input.AimRadians, input.AimMagnitude);

        if (dt > 0f) _service.Tick(dt);
    }

    public void LoadActiveConfig(string contentRoot)
    {
        _gameConfigJson = _configSource.LoadConfigJson(contentRoot);
        if (string.IsNullOrWhiteSpace(_gameConfigJson))
            throw new InvalidOperationException("Active game session config is empty.");

        ApplyPresentationConfig(_gameConfigJson);
    }

    private void ApplyPresentationConfig(string configJson)
    {
        using var doc = JsonDocument.Parse(configJson);
        var root = doc.RootElement;

        float newMoveSpeed = root.GetProperty("PlayerSpeed").GetSingle();

        var stage = root.GetProperty("StageConfig");
        float left = stage.GetProperty("Left").GetSingle();
        float top = stage.GetProperty("Top").GetSingle();
        float right = stage.GetProperty("Right").GetSingle();
        float bottom = stage.GetProperty("Bottom").GetSingle();

        float newStageWidth = right + left;
        float newStageHeight = bottom + top;
        int newBorderSize = (int)MathF.Min(left, top);

        bool changed =
            newMoveSpeed != MoveSpeed ||
            newStageWidth != StageWidth ||
            newStageHeight != StageHeight ||
            newBorderSize != BorderSize;

        MoveSpeed = newMoveSpeed;
        StageWidth = newStageWidth;
        StageHeight = newStageHeight;
        BorderSize = newBorderSize;

        if (changed)
            PresentationConfigVersion++;
    }
    
    private void ResetManifestProgress(string contentRoot)
    {
        var manifest = _configSource.LoadManifest(contentRoot);
        manifest.ActiveIndex = null;
        for (int i = 0; i < manifest.Entries.Count; i++)
            manifest.Entries[i].Completed = false;

        _configSource.SaveManifest(contentRoot, manifest);
    }

    private void NavigateConfig(string contentRoot, int delta)
    {
        var manifest = _configSource.LoadManifest(contentRoot);
        if (manifest.Entries.Count == 0) return;

        int currentIndex = manifest.ActiveIndex is int idx && idx >= 0 && idx < manifest.Entries.Count
            ? idx
            : _configSource.SelectEntryIndex(manifest);

        int nextIndex = (currentIndex + delta) % manifest.Entries.Count;
        if (nextIndex < 0) nextIndex += manifest.Entries.Count;

        manifest.ActiveIndex = nextIndex;
        _configSource.SaveManifest(contentRoot, manifest);

        var entry = manifest.Entries[nextIndex];
        var full = Path.Combine(AppContext.BaseDirectory, contentRoot, entry.File);
        _gameConfigJson = File.ReadAllText(full);

        _manifestSaved = false;
        ApplyPresentationConfig(_gameConfigJson);
        _service.Restart(_gameConfigJson);
    }
}
