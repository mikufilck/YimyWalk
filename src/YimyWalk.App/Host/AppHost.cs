using YimyWalk.App.Rendering;
using YimyWalk.App.Settings;
using YimyWalk.App.Startup;
using YimyWalk.App.UI;
using YimyWalk.App.Windows;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Display;
using YimyWalk.Core.Settings;
using YimyWalk.Core.Startup;
using YimyWalk.Core.Simulation;
using System.Numerics;

namespace YimyWalk.App.Host;

internal sealed class AppHost : ApplicationContext
{
    private readonly DesktopWorldSurface _surface;
    private readonly IMonotonicClock _clock;
    private readonly IAppLog _log;
    private readonly PetWindowRegistry _windows = new();
    private readonly FrameSurfaceCache _surfaceCache = new();
    private readonly CharacterCatalog _catalog = new();
    private readonly SettingsStore _settingsStore;
    private readonly SettingsSaveCoordinator _settingsSave;
    private readonly PetSession _session;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly BatchCreationQueue _batch;
    private readonly ManagerForm _manager;
    private readonly TrayIconHost _tray;
    private readonly DialogueScheduler _dialogue = new();
    private readonly Dictionary<Guid, BubbleWindow> _bubbleWindows = new();
    private readonly VirtualFoodSystem _virtualFood;
    private readonly Dictionary<Guid, FoodWindow> _foodWindows = new();
    private readonly HashSet<string> _registeredDefinitionIds = new(StringComparer.OrdinalIgnoreCase);
    private SettingsDocument _settings = SettingsDocument.Defaults;
    private SettingsLoadResult _settingsLoad = new(SettingsDocument.Defaults, UsedDefaults: true, UsedBackup: false, null);
    private bool _disposed;
    private double _lastTickSeconds;
    private double _dialogueClock;
    private uint _lastForegroundProcess;
    private double _nextForegroundCheckSeconds;
    internal bool IsExiting { get; private set; }

    public AppHost() : this(null) { }

    public AppHost(string? settingsRoot) : this(settingsRoot, StopwatchClock.Instance, null) { }

    internal AppHost(string? settingsRoot, IMonotonicClock clock, IAppLog? log)
    {
        _clock = clock;
        _settingsStore = new SettingsStore(settingsRoot);
        _log = log ?? new AppLog(Path.Combine(_settingsStore.RootDirectory, "logs"), clock);
        _surface = new DesktopWorldSurface(clock, _log);
        _session = new PetSession(_surface, new SystemRandomSource());
        _virtualFood = _session.VirtualFood;
        _session.InstanceAdded += OnInstanceAdded;
        _session.InstanceRemoved += OnInstanceRemoved;
        _session.InstanceFaulted += (_, error) => ReportRuntimeFailure("SIMULATION", error);
        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += (_, _) =>
        {
            try { Tick(); }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                _timer.Stop();
                _session.Pause();
                ReportRuntimeFailure("FRAME_LOOP_STOPPED", error);
            }
        };
        _settingsSave = new SettingsSaveCoordinator(SaveSettingsCore);
        _batch = new BatchCreationQueue(() =>
        {
            if (!_catalog.TryGet(_settings.SelectedRoleId, out var definition))
                definition = _catalog.Definitions.Values.FirstOrDefault()
                    ?? throw new InvalidOperationException("no character role is loaded");
            return _session.Create(definition.Id);
        });
        _manager = new ManagerForm(this);
        _batch.Failed += error => ReportRuntimeFailure("CREATION_CANCELLED", error);
        // ApplicationContext owns the message loop independently of manager visibility.
        _settingsSave.Changed += OnSettingsSaveChanged;
        _tray = new TrayIconHost(this);
    }

    public PetSession Session => _session;
    public PetWindowRegistry Windows => _windows;
    public CharacterCatalog Catalog => _catalog;
    internal FrameSurfaceCache SurfaceCache => _surfaceCache;
    public string CharacterRoot { get; private set; } = string.Empty;
    internal string? CharacterRootOverride { get; set; }
    public BatchCreationQueue Batch => _batch;
    public SettingsDocument Settings => _settings;
    public SettingsLoadResult SettingsLoad => _settingsLoad;
    public string SelectedRoleId => _settings.SelectedRoleId;
    public float GlobalScale
    {
        get => _settings.Scale;
        set
        {
            if (!float.IsFinite(value)) return;
            var bounded = Math.Clamp(value, SettingsDocument.MinScale, SettingsDocument.MaxScale);
            if (Math.Abs(_settings.Scale - bounded) < 0.0001f) return;
            _settings = _settings with { Scale = bounded };
            _session.GlobalScale = bounded;
            foreach (var window in _windows.Windows) RenderSafely(window, () => window.SetGlobalScale(bounded));
            if (!_disposed && CharacterRoot.Length > 0) SaveSettings(debounce: true);
        }
    }
    public string? PreferredDisplayId => _settings.PreferredDisplayId;
    public SettingsSaveState SettingsSaveState => _settingsSave.State;
    public string? SettingsSaveError => _settingsSave.Error;
    public int ActivePetCount => _session.ActiveCount;
    public int PendingCreationCount => _batch.PendingCount;
    public bool IsPaused => _session.IsPaused;
    public string? RuntimeDiagnostic { get; private set; }
    private void ReportRuntimeFailure(string code, Exception error)
    {
        _log.Record(code, error);
        RuntimeDiagnostic = code == "FRAME_LOOP_STOPPED"
            ? "运行已暂停；可清空后继续或重新启动。诊断已写入本地日志。"
            : "部分操作失败，已保留其他桌宠；可调整大小或重试创建。诊断已写入本地日志。";
    }

    private void RenderSafely(PetWindow window, Action render)
    {
        try { render(); }
        catch (RenderBudgetExceededException error) { ReportRuntimeFailure("RENDER_BUDGET", error); }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            ReportRuntimeFailure("PET_RENDER_REMOVED", error);
            _session.Remove(window.Instance.InstanceId);
        }
    }
    public IReadOnlyCollection<PetInstance> ActivePets => _session.Instances;
    // Simulation, creation, deletion and UI sampling all run serially on the WinForms thread.
    // Copy values before controls are updated; no queued or mutable snapshots reach the page.
    internal PetListItem[] CapturePets() => _session.Instances.Select(PetListItem.Capture).ToArray();
    public bool DefaultWalkEnabled
    {
        get => _session.DefaultWalkEnabled;
        set
        {
            if (_session.DefaultWalkEnabled == value) return;
            _session.DefaultWalkEnabled = value;
            if (!_disposed && CharacterRoot.Length > 0)
            {
                _settings = _settings with { DefaultWalkEnabled = value };
                SaveSettings();
            }
        }
    }
    public float ActivityRate
    {
        get => _session.ActivityRate;
        set
        {
            var bounded = Math.Clamp(value, SettingsDocument.MinActivityRate, SettingsDocument.MaxActivityRate);
            if (!float.IsFinite(bounded)) return;
            if (Math.Abs(_session.ActivityRate - bounded) < 0.0001f) return;
            _session.ActivityRate = bounded;
            if (!_disposed && CharacterRoot.Length > 0)
            {
                _settings = _settings with { ActivityRate = bounded };
                SaveSettings(debounce: true);
            }
        }
    }
    public float JumpDesire
    {
        get => _session.JumpDesire;
        set
        {
            var bounded = ClampDesire(value, SettingsDocument.DefaultJumpDesire);
            if (Math.Abs(_session.JumpDesire - bounded) < 0.0001f) return;
            _session.JumpDesire = bounded;
            if (!_disposed && CharacterRoot.Length > 0)
            {
                _settings = _settings with { JumpDesire = bounded };
                SaveSettings(debounce: true);
            }
        }
    }
    public float ClimbDesire
    {
        get => _session.ClimbDesire;
        set
        {
            var bounded = ClampDesire(value, SettingsDocument.DefaultClimbDesire);
            if (Math.Abs(_session.ClimbDesire - bounded) < 0.0001f) return;
            _session.ClimbDesire = bounded;
            if (!_disposed && CharacterRoot.Length > 0)
            {
                _settings = _settings with { ClimbDesire = bounded };
                SaveSettings(debounce: true);
            }
        }
    }
    public float WindowJumpDesire
    {
        get => _session.WindowJumpDesire;
        set
        {
            var bounded = ClampDesire(value, SettingsDocument.DefaultWindowJumpDesire);
            if (Math.Abs(_session.WindowJumpDesire - bounded) < 0.0001f) return;
            _session.WindowJumpDesire = bounded;
            if (!_disposed && CharacterRoot.Length > 0)
            {
                _settings = _settings with { WindowJumpDesire = bounded };
                SaveSettings(debounce: true);
            }
        }
    }
    public bool DefaultDialogueEnabled
    {
        get => _settings.DefaultDialogueEnabled;
        set
        {
            if (_settings.DefaultDialogueEnabled == value) return;
            _settings = _settings with { DefaultDialogueEnabled = value };
            if (!_disposed && CharacterRoot.Length > 0) SaveSettings();
        }
    }
    public IDisplayProvider DisplayProvider => _surface.DisplayProvider;
    public IReadOnlyList<DisplayInfo> Displays => _surface.Snapshot.Displays;
    public bool WindowLandingEnabled
    {
        get => _surface.WindowLandingEnabled;
        set
        {
            if (_surface.WindowLandingEnabled == value) return;
            _surface.WindowLandingEnabled = value;
            if (!_disposed && CharacterRoot.Length > 0)
            {
                _settings = _settings with { WindowLandingEnabled = value };
                SaveSettings();
            }
        }
    }
    public bool HungerEnabled
    {
        get => _session.HungerEnabled;
        set
        {
            if (_session.HungerEnabled == value) return;
            _session.HungerEnabled = value;
            if (!_disposed && CharacterRoot.Length > 0)
            {
                _settings = _settings with { HungerEnabled = value };
                SaveSettings();
            }
        }
    }
    public bool WindowSurfaceSnapshotValid => _surface.WindowSurfaceSnapshotValid;
    public IReadOnlyList<WindowSurfaceCandidate> WindowSurfaceCandidates => _surface.WindowSurfaceCandidates;
    public int CachedSurfaceCount => _surfaceCache.Count;
    public long CachedSurfaceBytes => _surfaceCache.AllocatedBytes;
    public long SurfaceBudgetBytes => _surfaceCache.BudgetBytes;
    public int DialogueBubbleCount => _dialogue.Active.Count;
    public int VirtualFoodCount => _virtualFood.Targets.Count;
    public bool DialogueEnabled
    {
        get => _settings.DialogueEnabled;
        set
        {
            if (_settings.DialogueEnabled == value) return;
            _settings = _settings with { DialogueEnabled = value };
            if (!value) ClearDialogues();
            if (!_disposed && CharacterRoot.Length > 0) SaveSettings();
        }
    }
    public bool ExternalEventsEnabled
    {
        get => _settings.ExternalEventsEnabled;
        set
        {
            if (_settings.ExternalEventsEnabled == value) return;
            _settings = _settings with { ExternalEventsEnabled = value };
            if (!value) _lastForegroundProcess = 0;
            if (!_disposed && CharacterRoot.Length > 0) SaveSettings();
        }
    }

    public void RefreshWindowSurfaces() => _surface.RefreshWindowSurfaces();

    public void SetPreferredDisplay(string? displayId)
    {
        var normalized = string.IsNullOrWhiteSpace(displayId) ? null : displayId;
        if (string.Equals(_settings.PreferredDisplayId, normalized, StringComparison.OrdinalIgnoreCase)) return;
        _surface.SetPreferredDisplay(normalized);
        _settings = _settings with { PreferredDisplayId = normalized };
        if (!_disposed && CharacterRoot.Length > 0) SaveSettings();
    }

    public void Start()
    {
        _surface.ExcludeWindow = IsHostWindow;
        _surface.RefreshNow();
        _surface.RefreshWindowSurfaces();
        _settingsLoad = _settingsStore.Load();
        _settings = _settingsLoad.Settings;
        _surface.SetPreferredDisplay(_settings.PreferredDisplayId);
        _surface.WindowLandingEnabled = _settings.WindowLandingEnabled;
        _session.HungerEnabled = _settings.HungerEnabled;
        _session.NeedRate = _settings.HungerRate;
        _session.DefaultWalkEnabled = _settings.DefaultWalkEnabled;
        _session.ActivityRate = _settings.ActivityRate;
        _session.JumpDesire = _settings.JumpDesire;
        _session.ClimbDesire = _settings.ClimbDesire;
        _session.WindowJumpDesire = _settings.WindowJumpDesire;
        _session.GlobalScale = _settings.Scale;
        var root = CharacterRootOverride;
        if (string.IsNullOrWhiteSpace(root)) root = Path.Combine(AppContext.BaseDirectory, "Characters");
        if (!Directory.Exists(root)) root = Path.Combine(Environment.CurrentDirectory, "Characters");
        CharacterRoot = root;
        var definitions = _catalog.Scan(root);
        var startupDiagnostic = StartupDiagnostics.ValidateCharacters(
            root,
            definitions,
            _catalog.LastFailures);
        if (startupDiagnostic is not null)
            throw new StartupFailureException(startupDiagnostic);
        ApplyDefinitions(definitions);
        EnsureSelectedRole();
        _timer.Start();
        _lastTickSeconds = _clock.Seconds;
        _dialogueClock = 0;
        _nextForegroundCheckSeconds = _clock.Seconds;
        _manager.RefreshCharacters();
        _manager.SetReloadStatus(definitions.Count, _catalog.LastFailures.Count);
        _manager.SetSettingsStatus(GetSettingsStatus());
        // The tray is the primary entry. Do not show and then hide a startup window.
    }

    public IReadOnlyList<CharacterScanFailure> ReloadCharacters()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(CharacterRoot))
            throw new InvalidOperationException("The host has not been started.");

        var definitions = _catalog.Scan(CharacterRoot);
        ApplyDefinitions(definitions);
        EnsureSelectedRole();
        _manager.RefreshCharacters();
        _manager.SetReloadStatus(definitions.Count, _catalog.LastFailures.Count);
        return _catalog.LastFailures;
    }

    public void CreateMany(string id, int count)
    {
        _session.CreateMany(_catalog.TryGet(id, out var definition) ? definition : throw new KeyNotFoundException(id), count);
    }

    public void ClearPets()
    {
        _batch.Cancel();
        _session.Clear();
        ClearVirtualFood();
    }

    public PetInstance? FindPet(Guid instanceId)
        => _session.Instances.FirstOrDefault(instance => instance.InstanceId == instanceId);

    public bool RemovePet(Guid instanceId)
        => _session.Remove(instanceId);

    public bool FeedPet(Guid instanceId)
    {
        var pet = FindPet(instanceId);
        if (pet is null) return false;
        var fed = pet.Feed();
        if (fed) PublishDialogue(pet, "ate");
        return fed;
    }

    public bool SetPetBehaviorOverrides(Guid instanceId, PetBehaviorOverrides overrides)
    {
        var pet = FindPet(instanceId);
        if (pet is null) return false;
        pet.BehaviorOverrides = overrides;
        return true;
    }

    public bool ResetPetBehaviorOverrides(Guid instanceId)
        => SetPetBehaviorOverrides(instanceId, PetBehaviorOverrides.Inherit);

    public bool SetPetScaleOverride(Guid instanceId, float? scale)
    {
        var pet = FindPet(instanceId);
        if (pet is null) return false;
        pet.ScaleOverride = scale;
        if (_windows.TryGet(instanceId, out var window) && window is not null)
            RenderSafely(window, window.RefreshFrame);
        return true;
    }

    public bool ResetPetScaleOverride(Guid instanceId)
        => SetPetScaleOverride(instanceId, null);

    private void ClearVirtualFood()
    {
        _virtualFood.Clear();
        foreach (var foodWindow in _foodWindows.Values) foodWindow.Dispose();
        _foodWindows.Clear();
    }

    public int FeedAll()
    {
        var fed = _session.FeedAll();
        foreach (var instance in _session.Instances.Where(instance => instance.State == PetState.Eat))
            PublishDialogue(instance, "ate");
        return fed;
    }

    public void SpawnVirtualFood()
    {
        if (_virtualFood.Targets.Count >= _virtualFood.MaxTargets)
        {
            RuntimeDiagnostic = "虚拟食物已达上限，请等待消耗后再试。";
            return;
        }
        var area = _surface.SpawnArea;
        _virtualFood.Spawn(new System.Numerics.Vector2(area.Left + area.Width * 0.5f, area.Bottom));
    }

    public void QueueCreate(string id, int count)
    {
        if (!_catalog.TryGet(id, out var definition)) return;
        try { _batch.Enqueue(() => _session.Create(definition.Id), count); }
        catch (Exception error) when (error is InvalidOperationException or OverflowException)
        { ReportRuntimeFailure("CREATION_REQUEST_REJECTED", error); }
    }

    public void SetSelectedRole(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !_catalog.TryGet(id, out _)) return;
        if (_settings.SelectedRoleId.Equals(id, StringComparison.OrdinalIgnoreCase)) return;
        _settings = _settings with { SelectedRoleId = id };
        SaveSettings();
    }

    public void RetrySettingsSave() => _settingsSave.Flush();

    public void ShowManager()
    {
        if (IsExiting || _disposed) return;
        if (_manager.WindowState == FormWindowState.Minimized)
            _manager.WindowState = FormWindowState.Normal;
        _manager.Show();
        _manager.RefreshAllPages();
        _manager.BringToFront();
        _manager.Activate();
    }

    public void HideManagerToTray()
    {
        if (!IsExiting && !_disposed) _manager.HideToTray();
    }

    public void SetPetsVisible(bool visible)
    {
        foreach (var window in _windows.Windows)
            window.SetVisibleWithoutActivation(visible);
    }

    public void ExitApplication()
    {
        if (_disposed) return;
        MarkExiting();
        _timer.Stop();
        _manager.StopRefreshing();
        _batch.Cancel();
        _manager.AllowClose();
        if (!_manager.IsDisposed) _manager.Close();
        ExitThread();
    }

    internal void MarkExiting() => IsExiting = true;

    public void PausePets() => _session.Pause();

    public void ResumePets()
    {
        _session.Resume();
        _lastTickSeconds = _clock.Seconds;
        _timer.Start();
    }

    private void Tick()
    {
        var now = _clock.Seconds;
        var delta = Math.Clamp(now - _lastTickSeconds, 0, PetEngine.MaxDeltaSeconds);
        _lastTickSeconds = now;
        _session.Tick(Math.Clamp(delta, 0, PetEngine.MaxDeltaSeconds));
        var activeFoodIds = _virtualFood.Targets.Select(target => target.Id).ToHashSet();
        foreach (var stale in _foodWindows.Keys.Where(id => !activeFoodIds.Contains(id)).ToArray())
        {
            _foodWindows[stale].Dispose();
            _foodWindows.Remove(stale);
        }
        foreach (var target in _virtualFood.Targets)
        {
            if (!_foodWindows.TryGetValue(target.Id, out var foodWindow))
            {
                foodWindow = new FoodWindow(Point.Empty);
                _foodWindows[target.Id] = foodWindow;
                foodWindow.Show();
            }
            foodWindow.UpdateTarget(
                new Point((int)Math.Round(target.Position.X - 10), (int)Math.Round(target.Position.Y - 10)),
                target.VisibleScale);
        }
        _dialogueClock += Math.Clamp(delta, 0, PetEngine.MaxDeltaSeconds);
        _dialogue.Advance(_dialogueClock);
        if (ExternalEventsEnabled && now >= _nextForegroundCheckSeconds)
        {
            _nextForegroundCheckSeconds = now + 0.5;
            PollForegroundApplication();
        }
        foreach (var expired in _bubbleWindows.Keys.Where(id => !_dialogue.Active.Any(bubble => bubble.Id == id)).ToArray())
        {
            _bubbleWindows[expired].Dispose();
            _bubbleWindows.Remove(expired);
        }
        foreach (var window in _windows.Windows)
            RenderSafely(window, window.RefreshFrame);
        foreach (var bubble in _dialogue.Active)
            if (_bubbleWindows.TryGetValue(bubble.Id, out var bubbleWindow)
                && _windows.TryGet(bubble.PetId, out var petWindow) && petWindow is not null)
                bubbleWindow.MoveAbove(petWindow.Bounds);
    }

    private void OnInstanceAdded(PetInstance instance)
    {
        var window = new PetWindow(instance, _session, _surfaceCache, _settings.Scale, _clock);
        window.Interaction += group => PublishDialogue(instance, group);
        _windows.Add(window);
        try { PublishDialogue(instance, "created"); }
        catch { _windows.Remove(instance.InstanceId); _dialogue.RemovePet(instance.InstanceId); throw; }
    }

    private void OnInstanceRemoved(PetInstance instance)
    {
        _windows.Remove(instance.InstanceId);
        _dialogue.RemovePet(instance.InstanceId);
        foreach (var bubbleId in _bubbleWindows.Where(pair => pair.Value is not null && _dialogue.Active.All(bubble => bubble.Id != pair.Key)).Select(pair => pair.Key).ToArray())
        {
            _bubbleWindows[bubbleId].Dispose();
            _bubbleWindows.Remove(bubbleId);
        }
        _batch.NotifyRemoved();
    }

    private void PublishDialogue(PetInstance instance, string group)
    {
        if (!DialogueEnabled) return;
        if (!instance.BehaviorOverrides.ResolveDialogueEnabled(DefaultDialogueEnabled)) return;
        var result = _dialogue.TryPublish(instance, group, _dialogueClock);
        if (!result.Accepted || result.Bubble is not { } bubble) return;
        if (!_windows.TryGet(instance.InstanceId, out var petWindow) || petWindow is null) return;
        var bubbleWindow = new BubbleWindow(bubble.Text, new Point(petWindow.Bounds.Left, Math.Max(0, petWindow.Bounds.Top - 40)));
        _bubbleWindows[bubble.Id] = bubbleWindow;
        bubbleWindow.Show();
        bubbleWindow.MoveAbove(petWindow.Bounds);
    }

    private void ClearDialogues()
    {
        _dialogue.Clear();
        foreach (var bubble in _bubbleWindows.Values) bubble.Dispose();
        _bubbleWindows.Clear();
    }

    private void PollForegroundApplication()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero || NativeMethods.GetWindowThreadProcessId(foreground, out var processId) == 0)
            return;
        if (processId == (uint)Environment.ProcessId || processId == _lastForegroundProcess)
            return;
        _lastForegroundProcess = processId;
        var instance = _session.Instances.FirstOrDefault();
        if (instance is not null) PublishDialogue(instance, "program_opened");
    }

    private bool IsHostWindow(IntPtr handle)
    {
        if (_manager.IsHandleCreated && _manager.Handle == handle) return true;
        if (_bubbleWindows.Values.Any(window => window.IsHandleCreated && window.Handle == handle)) return true;
        if (_foodWindows.Values.Any(window => window.IsHandleCreated && window.Handle == handle)) return true;
        return _windows.Windows.Any(window => window.IsHandleCreated && window.Handle == handle);
    }

    private void ApplyDefinitions(IReadOnlyDictionary<string, CharacterDefinition> definitions)
    {
        foreach (var definition in definitions.Values)
            _session.ReplaceDefinition(definition.Id, definition);

        foreach (var removedId in _registeredDefinitionIds.Where(id => !definitions.ContainsKey(id)).ToArray())
            _session.RemoveDefinition(removedId);

        _registeredDefinitionIds.Clear();
        foreach (var id in definitions.Keys)
            _registeredDefinitionIds.Add(id);
    }

    private void EnsureSelectedRole()
    {
        if (_catalog.TryGet(_settings.SelectedRoleId, out _)) return;
        var fallback = _catalog.Definitions.Values
            .OrderByDescending(definition => definition.Id.Equals("slime", StringComparison.OrdinalIgnoreCase))
            .ThenBy(definition => definition.Id)
            .FirstOrDefault();
        if (fallback is null) return;
        _settings = _settings with { SelectedRoleId = fallback.Id };
        SaveSettings();
    }

    private string GetSettingsStatus()
    {
        if (_settingsLoad.UsedBackup) return "Settings: recovered from backup";
        if (_settingsLoad.UsedDefaults)
            return string.IsNullOrWhiteSpace(_settingsLoad.Diagnostic)
                ? "Settings: using defaults"
                : $"Settings: using defaults ({_settingsLoad.Diagnostic})";
        return "Settings: loaded";
    }

    private void SaveSettings(bool debounce = false)
    {
        if (!_disposed && CharacterRoot.Length > 0)
            _settingsSave.Request(debounce);
    }

    private void SaveSettingsCore()
    {
        try { _settingsStore.Save(_settings); }
        catch (Exception error) { _log.Record("SETTINGS_SAVE_FAILED", error); throw; }
        _settingsLoad = new SettingsLoadResult(_settings, UsedDefaults: false, UsedBackup: false, null);
    }

    private void OnSettingsSaveChanged()
    {
        if (_disposed) return;
        var status = _settingsSave.State switch
        {
            SettingsSaveState.Applied => "Settings: applied; saving",
            SettingsSaveState.Saving => "Settings: saving",
            SettingsSaveState.Saved => "Settings: saved",
            SettingsSaveState.Failed => $"Settings: applied for this run; save failed ({_settingsSave.Error})",
            _ => "Settings: unknown"
        };
        _manager.SetSettingsStatus(status);
    }

    private static float ClampDesire(float value, float fallback)
        => float.IsFinite(value) ? Math.Clamp(value, SettingsDocument.MinDesire, SettingsDocument.MaxDesire) : fallback;

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            if (CharacterRoot.Length > 0) _settingsSave.Flush();
            _disposed = true;
            _timer.Stop();
            _timer.Dispose();
            _batch.Dispose();
            ClearDialogues();
            ClearVirtualFood();
            _tray.Dispose();
            _manager.Dispose();
            _session.Dispose();
            _catalog.Dispose();
            _registeredDefinitionIds.Clear();
            _windows.Dispose();
            _surfaceCache.Dispose();
            _surface.Dispose();
            _settingsSave.Dispose();
        }
        base.Dispose(disposing);
    }
}
