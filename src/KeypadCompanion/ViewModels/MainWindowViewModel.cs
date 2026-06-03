using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeypadCompanion.Domain;
using KeypadCompanion.Services;

namespace KeypadCompanion.ViewModels;

public enum WindowInteractionRequest
{
    Show,
    Exit,
}

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private const string ActionTypeNone = "None";
    private const string ActionTypeHotkey = "Hotkey";
    private const string ActionTypeText = "TextSnippet";
    private const string ActionTypeLaunch = "LaunchApp";
    private const string ActionTypeUrl = "OpenUrl";
    private const string ActionTypeShell = "ShellCommand";
    private const string ActionTypeVolume = "SetVolumeDelta";
    private const string ActionTypeMute = "ToggleMute";

    private readonly ISerialDeviceService _serialDeviceService;
    private readonly TcpDeviceService _tcpDeviceService;
    private readonly IGestureResolver _gestureResolver;
    private readonly IMacroExecutor _macroExecutor;
    private readonly IConfigStore _configStore;

    private AppConfig _config = AppConfig.CreateDefault();

    public MainWindowViewModel(
        ISerialDeviceService serialDeviceService,
        TcpDeviceService tcpDeviceService,
        IGestureResolver gestureResolver,
        IMacroExecutor macroExecutor,
        IConfigStore configStore)
    {
        _serialDeviceService = serialDeviceService;
        _tcpDeviceService = tcpDeviceService;
        _gestureResolver = gestureResolver;
        _macroExecutor = macroExecutor;
        _configStore = configStore;

        AvailableActionTypes =
        [
            ActionTypeHotkey,
            ActionTypeText,
            ActionTypeLaunch,
            ActionTypeUrl,
            ActionTypeShell,
            ActionTypeVolume,
            ActionTypeMute,
        ];

        Key1 = new InputVisualViewModel(InputId.Key1, "KEY 1");
        Key2 = new InputVisualViewModel(InputId.Key2, "KEY 2");
        Key3 = new InputVisualViewModel(InputId.Key3, "KEY 3");
        Key4 = new InputVisualViewModel(InputId.Key4, "KEY 4");
        Key5 = new InputVisualViewModel(InputId.Key5, "KEY 5");
        Key6 = new InputVisualViewModel(InputId.Key6, "KEY 6");
        Key7 = new InputVisualViewModel(InputId.Key7, "KEY 7");
        Key8 = new InputVisualViewModel(InputId.Key8, "KEY 8");
        EncoderButton = new InputVisualViewModel(InputId.EncoderButton, "PRESS");
        EncoderClockwise = new InputVisualViewModel(InputId.EncoderClockwise, "CW");
        EncoderCounterClockwise = new InputVisualViewModel(InputId.EncoderCounterClockwise, "CCW");

        _serialDeviceService.RawDeviceEventReceived += OnRawDeviceEventReceived;
        _serialDeviceService.ConnectionStatusChanged += OnConnectionStatusChanged;
        _serialDeviceService.WifiIpDiscovered += OnWifiIpDiscovered;
        _tcpDeviceService.RawDeviceEventReceived += OnRawDeviceEventReceived;
        _tcpDeviceService.ConnectionStatusChanged += OnTcpConnectionStatusChanged;
        _gestureResolver.TriggerResolved += OnTriggerResolved;
    }

    public event EventHandler<WindowInteractionRequest>? WindowInteractionRequested;
    public event EventHandler? AssignmentMenusChanged;
    public event EventHandler? WifiSettingsRequested;

    public ObservableCollection<string> AvailablePorts { get; } = [];
    public ObservableCollection<string> EventLogEntries { get; } = [];
    public ObservableCollection<BindingSlotViewModel> BindingSlots { get; } = [];
    public ObservableCollection<ActionPresetViewModel> ActionPresets { get; } = [];
    public IReadOnlyList<string> AvailableActionTypes { get; }

    public static IReadOnlyList<string> AvailableIconOptions { get; } =
    [
        "Auto",
        // ── action types ──────────────────────────────────────
        "Keyboard",
        "Document",
        "Rocket",
        "Globe",
        "Terminal",
        // ── volume ────────────────────────────────────────────
        "Volume Up",
        "Volume Down",
        "Mute",
        // ── editing ───────────────────────────────────────────
        "Copy",
        "Paste",
        "Cut",
        "Undo",
        "Redo",
        "Save",
        "Edit",
        "Trash",
        // ── media ─────────────────────────────────────────────
        "Play",
        "Pause",
        "Stop",
        "Next Track",
        "Prev Track",
        "Camera",
        "Music",
        "Microphone",
        "Mic Off",
        "Headset",
        "Image",
        // ── navigation / system ───────────────────────────────
        "Home",
        "Power",
        "Lock",
        "Settings",
        "Search",
        "Zoom In",
        "Zoom Out",
        "Refresh",
        "Expand",
        "Compress",
        // ── files / data ──────────────────────────────────────
        "Folder",
        "Download",
        "Upload",
        "Print",
        "Link",
        // ── communication ─────────────────────────────────────
        "Mail",
        "Comment",
        "Share",
        // ── misc ──────────────────────────────────────────────
        "Code",
        "Calendar",
        "Clock",
        "Star",
        "Heart",
        "Bolt",
        "Robot",
        "Bug",
        "Wi-Fi",
        "Desktop",
        "Cloud",
        "Key",
        "Eye",
        "Filter",
        "Flag",
    ];

    private static readonly Dictionary<string, string> IconDisplayToCode = new()
    {
        ["Auto"]         = "",
        ["Keyboard"]     = "HOTKEY",
        ["Document"]     = "TEXT",
        ["Rocket"]       = "LAUNCH",
        ["Globe"]        = "URL",
        ["Terminal"]     = "SHELL",
        ["Volume Up"]    = "VOLUP",
        ["Volume Down"]  = "VOLDOWN",
        ["Mute"]         = "MUTE",
        ["Copy"]         = "COPY",
        ["Paste"]        = "PASTE",
        ["Cut"]          = "CUT",
        ["Undo"]         = "UNDO",
        ["Redo"]         = "REDO",
        ["Save"]         = "SAVE",
        ["Edit"]         = "EDIT",
        ["Trash"]        = "TRASH",
        ["Play"]         = "PLAY",
        ["Pause"]        = "PAUSE",
        ["Stop"]         = "STOP",
        ["Next Track"]   = "NEXT",
        ["Prev Track"]   = "PREV",
        ["Camera"]       = "CAMERA",
        ["Music"]        = "MUSIC",
        ["Microphone"]   = "MIC",
        ["Mic Off"]      = "MIC_OFF",
        ["Headset"]      = "HEADSET",
        ["Image"]        = "IMAGE",
        ["Home"]         = "HOME",
        ["Power"]        = "POWER",
        ["Lock"]         = "LOCK",
        ["Settings"]     = "SETTINGS",
        ["Search"]       = "SEARCH",
        ["Zoom In"]      = "ZOOM_IN",
        ["Zoom Out"]     = "ZOOM_OUT",
        ["Refresh"]      = "REFRESH",
        ["Expand"]       = "EXPAND",
        ["Compress"]     = "COMPRESS",
        ["Folder"]       = "FOLDER",
        ["Download"]     = "DOWNLOAD",
        ["Upload"]       = "UPLOAD",
        ["Print"]        = "PRINT",
        ["Link"]         = "LINK",
        ["Mail"]         = "MAIL",
        ["Comment"]      = "COMMENT",
        ["Share"]        = "SHARE",
        ["Code"]         = "CODE",
        ["Calendar"]     = "CALENDAR",
        ["Clock"]        = "CLOCK",
        ["Star"]         = "STAR",
        ["Heart"]        = "HEART",
        ["Bolt"]         = "BOLT",
        ["Robot"]        = "ROBOT",
        ["Bug"]          = "BUG",
        ["Wi-Fi"]        = "WIFI",
        ["Desktop"]      = "DESKTOP",
        ["Cloud"]        = "CLOUD",
        ["Key"]          = "KEY",
        ["Eye"]          = "EYE",
        ["Filter"]       = "FILTER",
        ["Flag"]         = "FLAG",
    };

    private static readonly Dictionary<string, string> IconCodeToDisplay =
        IconDisplayToCode.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    public InputVisualViewModel Key1 { get; }
    public InputVisualViewModel Key2 { get; }
    public InputVisualViewModel Key3 { get; }
    public InputVisualViewModel Key4 { get; }
    public InputVisualViewModel Key5 { get; }
    public InputVisualViewModel Key6 { get; }
    public InputVisualViewModel Key7 { get; }
    public InputVisualViewModel Key8 { get; }
    public InputVisualViewModel EncoderButton { get; }
    public InputVisualViewModel EncoderClockwise { get; }
    public InputVisualViewModel EncoderCounterClockwise { get; }

    public bool IsExitRequested { get; private set; }

    [ObservableProperty]
    private string connectionStatus = "Disconnected";

    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private bool isWifiConnected;

    [ObservableProperty]
    private string wifiIp = string.Empty;

    [ObservableProperty]
    private string? selectedPort;

    [ObservableProperty]
    private bool autoConnect = true;

    [ObservableProperty]
    private bool minimizeToTray = true;

    [ObservableProperty]
    private string longPressMillisecondsText = "500";

    [ObservableProperty]
    private string doubleClickMillisecondsText = "275";

    [ObservableProperty]
    private InputId selectedInputId = InputId.Key1;

    [ObservableProperty]
    private string selectedInputLabel = "KEY 1";

    [ObservableProperty]
    private ActionPresetViewModel? selectedActionPreset;

    [ObservableProperty]
    private string actionPresetName = string.Empty;

    [ObservableProperty]
    private string selectedActionType = ActionTypeHotkey;

    [ObservableProperty]
    private string selectedIconDisplay = "Auto";

    [ObservableProperty]
    private string hotkeyChord = string.Empty;

    [ObservableProperty]
    private string textSnippet = string.Empty;

    [ObservableProperty]
    private string launchFilePath = string.Empty;

    [ObservableProperty]
    private string launchArguments = string.Empty;

    [ObservableProperty]
    private string launchWorkingDirectory = string.Empty;

    [ObservableProperty]
    private string url = string.Empty;

    [ObservableProperty]
    private string shellFileName = "powershell.exe";

    [ObservableProperty]
    private string shellArguments = string.Empty;

    [ObservableProperty]
    private string shellWorkingDirectory = string.Empty;

    [ObservableProperty]
    private string volumeDeltaPercentText = "2";

    public string AssignmentHint => "Create or edit an action on the left, then right-click a key or encoder control on the right to assign it.";
    public string PresetEditorTitle => SelectedActionPreset is null ? "New Action" : $"Editing: {SelectedActionPreset.Name}";

    public bool IsHotkeySelected => SelectedActionType == ActionTypeHotkey;
    public bool IsTextSelected => SelectedActionType == ActionTypeText;
    public bool IsLaunchSelected => SelectedActionType == ActionTypeLaunch;
    public bool IsUrlSelected => SelectedActionType == ActionTypeUrl;
    public bool IsShellSelected => SelectedActionType == ActionTypeShell;
    public bool IsVolumeSelected => SelectedActionType == ActionTypeVolume;
    public bool IsMuteSelected => SelectedActionType == ActionTypeMute;

    public async Task InitializeAsync()
    {
        _config = await _configStore.LoadAsync();
        _config.Normalize();

        AutoConnect = _config.Connection.AutoConnect;
        MinimizeToTray = _config.Window.MinimizeToTray;
        LongPressMillisecondsText = _config.Gesture.LongPressMilliseconds.ToString();
        DoubleClickMillisecondsText = _config.Gesture.DoubleClickMilliseconds.ToString();
        _gestureResolver.UpdateSettings(_config.Gesture);

        RefreshPortsCore();
        RefreshActionPresets();
        SelectedPort = _config.Connection.LastSuccessfulPort ?? AvailablePorts.FirstOrDefault();
        SelectInput(InputId.Key1);
        SelectedActionPreset = ActionPresets.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(_config.WiFi.LastKnownIp))
        {
            WifiIp = _config.WiFi.LastKnownIp;
        }

        // Prefer WiFi auto-connect if we have a saved IP; fall back to serial.
        if (AutoConnect && !string.IsNullOrWhiteSpace(_config.WiFi.LastKnownIp))
        {
            try
            {
                await _tcpDeviceService.ConnectAsync($"{_config.WiFi.LastKnownIp}:4242");
                return; // WiFi connected — skip serial auto-connect.
            }
            catch (Exception ex)
            {
                AppendLog($"WiFi auto-connect failed: {ex.Message}. Trying serial...");
            }
        }

        if (AutoConnect && !string.IsNullOrWhiteSpace(_config.Connection.LastSuccessfulPort))
        {
            try
            {
                await _serialDeviceService.ConnectAsync(_config.Connection.LastSuccessfulPort);
            }
            catch (Exception ex)
            {
                AppendLog($"Auto-connect failed: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void RefreshPorts()
    {
        RefreshPortsCore();
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        var port = SelectedPort;
        if (string.IsNullOrWhiteSpace(port))
        {
            AppendLog("Select a COM port before connecting.");
            return;
        }

        try
        {
            await _serialDeviceService.ConnectAsync(port);
            _config.Connection.LastSuccessfulPort = port;
            await PersistConfigAsync();
        }
        catch (Exception ex)
        {
            AppendLog($"Connection failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _serialDeviceService.DisconnectAsync();
    }

    [RelayCommand]
    private async Task ReconnectAsync()
    {
        var port = SelectedPort ?? _config.Connection.LastSuccessfulPort;
        if (string.IsNullOrWhiteSpace(port))
        {
            AppendLog("No saved COM port is available to reconnect.");
            return;
        }

        SelectedPort = port;
        await ConnectAsync();
    }

    [RelayCommand]
    private async Task ConnectWifiAsync()
    {
        var ip = WifiIp.Trim();
        if (string.IsNullOrWhiteSpace(ip))
        {
            AppendLog("No WiFi IP — provision the device first via WiFi Settings.");
            return;
        }

        try
        {
            await _tcpDeviceService.ConnectAsync($"{ip}:4242");
            _config.WiFi.LastKnownIp = ip;
            await PersistConfigAsync();
        }
        catch (Exception ex)
        {
            AppendLog($"WiFi connect failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DisconnectWifiAsync()
    {
        await _tcpDeviceService.DisconnectAsync();
    }

    [RelayCommand]
    private void OpenWifiSettings()
    {
        WifiSettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ShowWindow()
    {
        WindowInteractionRequested?.Invoke(this, WindowInteractionRequest.Show);
    }

    [RelayCommand]
    private async Task ExitAsync()
    {
        IsExitRequested = true;
        await PersistConfigAsync();
        await _serialDeviceService.DisconnectAsync();
        WindowInteractionRequested?.Invoke(this, WindowInteractionRequest.Exit);
    }

    [RelayCommand]
    private void SelectInput(InputId inputId)
    {
        SelectedInputId = inputId;
    }

    [RelayCommand]
    private void NewActionPreset()
    {
        SelectedActionPreset = null;
        ActionPresetName = string.Empty;
        SelectedActionType = ActionTypeHotkey;
        SelectedIconDisplay = "Auto";
        HotkeyChord = string.Empty;
        TextSnippet = string.Empty;
        LaunchFilePath = string.Empty;
        LaunchArguments = string.Empty;
        LaunchWorkingDirectory = string.Empty;
        Url = string.Empty;
        ShellFileName = "powershell.exe";
        ShellArguments = string.Empty;
        ShellWorkingDirectory = string.Empty;
        VolumeDeltaPercentText = "2";
    }

    [RelayCommand]
    private async Task SaveActionPresetAsync()
    {
        try
        {
            var action = BuildActionFromEditor();
            var name = GetActionPresetName(action);

            var existing = SelectedActionPreset is null
                ? null
                : _config.ActionPresets.FirstOrDefault(preset => preset.Id == SelectedActionPreset.Id);

            if (existing is null)
            {
                existing = new ActionPreset { Id = Guid.NewGuid() };
                _config.ActionPresets.Add(existing);
            }

            existing.Name = name;
            existing.Action = action;
            existing.IconCode = IconDisplayToCode.GetValueOrDefault(SelectedIconDisplay, "");

            RefreshActionPresets();
            SelectedActionPreset = ActionPresets.FirstOrDefault(preset => preset.Id == existing.Id);
            await PersistConfigAsync();
            AssignmentMenusChanged?.Invoke(this, EventArgs.Empty);
            AppendLog($"Saved action '{name}'.");
        }
        catch (Exception ex)
        {
            AppendLog($"Action save failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteActionPresetAsync()
    {
        if (SelectedActionPreset is null)
        {
            AppendLog("Select an action to delete.");
            return;
        }

        _config.ActionPresets.RemoveAll(preset => preset.Id == SelectedActionPreset.Id);
        var removedName = SelectedActionPreset.Name;
        RefreshActionPresets();
        SelectedActionPreset = ActionPresets.FirstOrDefault();
        await PersistConfigAsync();
        AssignmentMenusChanged?.Invoke(this, EventArgs.Empty);
        AppendLog($"Deleted action '{removedName}'.");
    }

    public IReadOnlyList<TriggerType> GetAvailableTriggersForInput(InputId inputId)
    {
        return GetAvailableTriggers(inputId);
    }

    public IReadOnlyList<ActionPresetViewModel> GetActionPresetsSnapshot()
    {
        return ActionPresets.ToList();
    }

    public string GetBindingSummary(InputId inputId, TriggerType triggerType)
    {
        return _config.ActiveProfile.Bindings
            .LastOrDefault(binding => binding.InputId == inputId && binding.TriggerType == triggerType)
            ?.Action.DisplayName
            ?? "No action assigned";
    }

    public async Task AssignPresetToTriggerAsync(InputId inputId, TriggerType triggerType, Guid? presetId)
    {
        _config.ActiveProfile.Bindings.RemoveAll(binding =>
            binding.InputId == inputId &&
            binding.TriggerType == triggerType);

        if (presetId is not null)
        {
            var preset = _config.ActionPresets.FirstOrDefault(existing => existing.Id == presetId.Value);
            if (preset is not null)
            {
                _config.ActiveProfile.Bindings.Add(new BindingEntry
                {
                    InputId = inputId,
                    TriggerType = triggerType,
                    Action = BindingActionCloner.Clone(preset.Action),
                    Label = preset.Name,
                    IconCode = preset.IconCode,
                });

                AppendLog($"Assigned '{preset.Name}' to {inputId} {triggerType}.");
            }
        }
        else
        {
            AppendLog($"Cleared {inputId} {triggerType}.");
        }

        if (SelectedInputId == inputId)
        {
            RefreshBindingSlots();
        }

        _ = PushIconForKeyAsync(inputId);
        await PersistConfigAsync();
        AssignmentMenusChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string GetShortLabel(BindingAction? action)
    {
        var raw = action switch
        {
            HotkeyAction h                                  => h.Chord,
            TextSnippetAction t                             => t.Text,
            LaunchAppAction l                               => Path.GetFileNameWithoutExtension(l.FilePath),
            OpenUrlAction u                                 => TryGetHost(u.Url),
            ShellCommandAction s                            => s.Arguments,
            SetVolumeDeltaAction { DeltaPercent: >= 0 } v  => $"+{v.DeltaPercent:0.#}%",
            SetVolumeDeltaAction v                          => $"{v.DeltaPercent:0.#}%",
            ToggleMuteAction                                => "Mute",
            _                                              => "",
        };
        return raw.Length > 8 ? raw[..8] : raw;
    }

    private static string TryGetHost(string url)
    {
        try { return new Uri(url).Host.Replace("www.", ""); }
        catch { return url.Length > 8 ? url[..8] : url; }
    }

    private static string GetIconCode(BindingAction? action) => action switch
    {
        HotkeyAction                                   => "HOTKEY",
        TextSnippetAction                              => "TEXT",
        LaunchAppAction                                => "LAUNCH",
        OpenUrlAction                                  => "URL",
        ShellCommandAction                             => "SHELL",
        SetVolumeDeltaAction { DeltaPercent: >= 0 }   => "VOLUP",
        SetVolumeDeltaAction                           => "VOLDOWN",
        ToggleMuteAction                               => "MUTE",
        _                                              => "",
    };

    private static readonly InputId[] KeyInputIds =
    [
        InputId.Key1, InputId.Key2, InputId.Key3, InputId.Key4,
        InputId.Key5, InputId.Key6, InputId.Key7, InputId.Key8,
    ];

    private BindingEntry? GetPressBinding(InputId inputId) =>
        _config.ActiveProfile.Bindings
            .LastOrDefault(b => b.InputId == inputId && b.TriggerType == TriggerType.Press);

    private (string code, string label) ResolveIconAndLabel(BindingEntry? binding)
    {
        if (binding is null) return ("", "");
        var code = !string.IsNullOrEmpty(binding.IconCode)
            ? binding.IconCode
            : GetIconCode(binding.Action);
        var label = !string.IsNullOrEmpty(binding.Label)
            ? binding.Label
            : GetShortLabel(binding.Action);
        return (code, label.Length > 8 ? label[..8] : label);
    }

    private async Task SendToDevicesAsync(string line)
    {
        if (_serialDeviceService.IsConnected)
            await _serialDeviceService.SendLineAsync(line);
        if (_tcpDeviceService.IsConnected)
            await _tcpDeviceService.SendLineAsync(line);
    }

    private async Task PushIconsToDeviceAsync()
    {
        for (var i = 0; i < KeyInputIds.Length; i++)
        {
            var (code, label) = ResolveIconAndLabel(GetPressBinding(KeyInputIds[i]));
            await SendToDevicesAsync($"ICON:{i + 1}:{code}:{label}");
        }
    }

    private Task PushIconForKeyAsync(InputId inputId)
    {
        var index = Array.IndexOf(KeyInputIds, inputId);
        if (index < 0) return Task.CompletedTask;
        var (code, label) = ResolveIconAndLabel(GetPressBinding(inputId));
        return SendToDevicesAsync($"ICON:{index + 1}:{code}:{label}");
    }

    public void Dispose()
    {
        _serialDeviceService.RawDeviceEventReceived -= OnRawDeviceEventReceived;
        _serialDeviceService.ConnectionStatusChanged -= OnConnectionStatusChanged;
        _serialDeviceService.WifiIpDiscovered -= OnWifiIpDiscovered;
        _tcpDeviceService.RawDeviceEventReceived -= OnRawDeviceEventReceived;
        _tcpDeviceService.ConnectionStatusChanged -= OnTcpConnectionStatusChanged;
        _gestureResolver.TriggerResolved -= OnTriggerResolved;
        _gestureResolver.Dispose();
        _serialDeviceService.Dispose();
        _tcpDeviceService.Dispose();
    }

    partial void OnSelectedInputIdChanged(InputId value)
    {
        foreach (var visual in GetAllVisuals())
        {
            visual.IsSelected = visual.InputId == value;
        }

        SelectedInputLabel = GetVisual(value).DisplayName;
        RefreshBindingSlots();
    }

    partial void OnSelectedActionPresetChanged(ActionPresetViewModel? value)
    {
        LoadEditorFromPreset(value);
        OnPropertyChanged(nameof(PresetEditorTitle));
    }

    partial void OnSelectedActionTypeChanged(string value)
    {
        NotifyActionTypeVisibilityChanged();
    }

    partial void OnAutoConnectChanged(bool value)
    {
        _config.Connection.AutoConnect = value;
        _ = PersistConfigAsync();
    }

    partial void OnMinimizeToTrayChanged(bool value)
    {
        _config.Window.MinimizeToTray = value;
        _ = PersistConfigAsync();
    }

    partial void OnLongPressMillisecondsTextChanged(string value)
    {
        if (int.TryParse(value, out var parsed) && parsed >= 100)
        {
            _config.Gesture.LongPressMilliseconds = parsed;
            _gestureResolver.UpdateSettings(_config.Gesture);
            _ = PersistConfigAsync();
        }
    }

    partial void OnDoubleClickMillisecondsTextChanged(string value)
    {
        if (int.TryParse(value, out var parsed) && parsed >= 100)
        {
            _config.Gesture.DoubleClickMilliseconds = parsed;
            _gestureResolver.UpdateSettings(_config.Gesture);
            _ = PersistConfigAsync();
        }
    }

    private void OnRawDeviceEventReceived(object? sender, RawDeviceEvent e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            AppendLog($"{e.Timestamp:HH:mm:ss.fff} RAW {FormatRawEvent(e)}");
            UpdateVisualForRawEvent(e);
        });

        _gestureResolver.HandleRawEvent(e);
    }

    private void OnConnectionStatusChanged(object? sender, SerialConnectionStatus e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsConnected = e.IsConnected || _tcpDeviceService.IsConnected;
            ConnectionStatus = e.Message;
            if (!string.IsNullOrWhiteSpace(e.PortName))
            {
                SelectedPort = e.PortName;
            }

            AppendLog($"{DateTimeOffset.Now:HH:mm:ss.fff} {e.Message}");
            RefreshPortsCore();

            if (e.IsConnected)
            {
                _ = PushIconsToDeviceAsync();
            }
        });
    }

    private void OnTcpConnectionStatusChanged(object? sender, SerialConnectionStatus e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsWifiConnected = e.IsConnected;
            IsConnected = _serialDeviceService.IsConnected || e.IsConnected;
            ConnectionStatus = e.Message;
            AppendLog($"{DateTimeOffset.Now:HH:mm:ss.fff} {e.Message}");

            if (e.IsConnected)
            {
                _ = PushIconsToDeviceAsync();
            }
        });
    }

    private void OnWifiIpDiscovered(object? sender, string ip)
    {
        Dispatcher.UIThread.Post(() =>
        {
            WifiIp = ip;
            _config.WiFi.LastKnownIp = ip;
            AppendLog($"{DateTimeOffset.Now:HH:mm:ss.fff} Device WiFi IP: {ip}");
            _ = PersistConfigAsync();
        });
    }

    private async void OnTriggerResolved(object? sender, ResolvedTriggerEvent e)
    {
        Dispatcher.UIThread.Post(() => AppendLog($"{e.Timestamp:HH:mm:ss.fff} {FormatResolvedEvent(e)}"));

        var action = _config.ActiveProfile.Bindings
            .LastOrDefault(binding => binding.InputId == e.InputId && binding.TriggerType == e.TriggerType)
            ?.Action;

        if (action is null)
        {
            return;
        }

        try
        {
            await _macroExecutor.ExecuteAsync(action);
            Dispatcher.UIThread.Post(() => AppendLog($"{DateTimeOffset.Now:HH:mm:ss.fff} Executed {action.DisplayName}"));
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => AppendLog($"{DateTimeOffset.Now:HH:mm:ss.fff} Action failed: {ex.Message}"));
        }
    }

    private void RefreshPortsCore()
    {
        var ports = _serialDeviceService.GetAvailablePorts();
        AvailablePorts.Clear();
        foreach (var port in ports)
        {
            AvailablePorts.Add(port);
        }

        if (!string.IsNullOrWhiteSpace(SelectedPort) && AvailablePorts.Contains(SelectedPort))
        {
            return;
        }

        SelectedPort = AvailablePorts.FirstOrDefault();
    }

    private void RefreshActionPresets()
    {
        ActionPresets.Clear();
        foreach (var preset in _config.ActionPresets.OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase))
        {
            ActionPresets.Add(new ActionPresetViewModel
            {
                Id = preset.Id,
                Name = preset.Name,
                Action = preset.Action,
                IconCode = preset.IconCode,
            });
        }
    }

    private void RefreshBindingSlots()
    {
        BindingSlots.Clear();

        foreach (var trigger in GetAvailableTriggers(SelectedInputId))
        {
            var action = _config.ActiveProfile.Bindings
                .LastOrDefault(binding => binding.InputId == SelectedInputId && binding.TriggerType == trigger)
                ?.Action;

            BindingSlots.Add(new BindingSlotViewModel
            {
                InputId = SelectedInputId,
                TriggerType = trigger,
                Action = action,
            });
        }
    }

    private BindingAction BuildActionFromEditor()
    {
        return SelectedActionType switch
        {
            ActionTypeHotkey => string.IsNullOrWhiteSpace(HotkeyChord)
                ? throw new InvalidOperationException("Hotkey chord cannot be empty.")
                : new HotkeyAction { Chord = HotkeyChord },
            ActionTypeText => new TextSnippetAction { Text = TextSnippet },
            ActionTypeLaunch => string.IsNullOrWhiteSpace(LaunchFilePath)
                ? throw new InvalidOperationException("Launch path cannot be empty.")
                : new LaunchAppAction
                {
                    FilePath = LaunchFilePath,
                    Arguments = LaunchArguments,
                    WorkingDirectory = LaunchWorkingDirectory,
                },
            ActionTypeUrl => string.IsNullOrWhiteSpace(Url)
                ? throw new InvalidOperationException("URL cannot be empty.")
                : new OpenUrlAction { Url = Url },
            ActionTypeShell => string.IsNullOrWhiteSpace(ShellFileName)
                ? throw new InvalidOperationException("Shell file name cannot be empty.")
                : new ShellCommandAction
                {
                    FileName = ShellFileName,
                    Arguments = ShellArguments,
                    WorkingDirectory = ShellWorkingDirectory,
                },
            ActionTypeVolume => !double.TryParse(VolumeDeltaPercentText, out var delta)
                ? throw new InvalidOperationException("Volume delta must be a valid number.")
                : new SetVolumeDeltaAction { DeltaPercent = delta },
            ActionTypeMute => new ToggleMuteAction(),
            ActionTypeNone => throw new InvalidOperationException("Select an action type."),
            _ => throw new InvalidOperationException("Unsupported action type."),
        };
    }

    private string GetActionPresetName(BindingAction action)
    {
        var name = ActionPresetName.Trim();
        return string.IsNullOrWhiteSpace(name)
            ? action.DisplayName
            : name;
    }

    private void LoadEditorFromPreset(ActionPresetViewModel? preset)
    {
        if (preset is null)
        {
            OnPropertyChanged(nameof(PresetEditorTitle));
            return;
        }

        ActionPresetName = preset.Name;
        SelectedIconDisplay = IconCodeToDisplay.GetValueOrDefault(preset.IconCode, "Auto");
        switch (preset.Action)
        {
            case HotkeyAction hotkeyAction:
                SelectedActionType = ActionTypeHotkey;
                HotkeyChord = hotkeyAction.Chord;
                break;
            case TextSnippetAction textSnippetAction:
                SelectedActionType = ActionTypeText;
                TextSnippet = textSnippetAction.Text;
                break;
            case LaunchAppAction launchAppAction:
                SelectedActionType = ActionTypeLaunch;
                LaunchFilePath = launchAppAction.FilePath;
                LaunchArguments = launchAppAction.Arguments;
                LaunchWorkingDirectory = launchAppAction.WorkingDirectory;
                break;
            case OpenUrlAction openUrlAction:
                SelectedActionType = ActionTypeUrl;
                Url = openUrlAction.Url;
                break;
            case ShellCommandAction shellCommandAction:
                SelectedActionType = ActionTypeShell;
                ShellFileName = shellCommandAction.FileName;
                ShellArguments = shellCommandAction.Arguments;
                ShellWorkingDirectory = shellCommandAction.WorkingDirectory;
                break;
            case SetVolumeDeltaAction setVolumeDeltaAction:
                SelectedActionType = ActionTypeVolume;
                VolumeDeltaPercentText = setVolumeDeltaAction.DeltaPercent.ToString("0.##");
                break;
            case ToggleMuteAction:
                SelectedActionType = ActionTypeMute;
                break;
            default:
                SelectedActionType = ActionTypeHotkey;
                break;
        }

        OnPropertyChanged(nameof(PresetEditorTitle));
    }

    private async Task PersistConfigAsync()
    {
        try
        {
            await _configStore.SaveAsync(_config);
        }
        catch (Exception ex)
        {
            AppendLog($"{DateTimeOffset.Now:HH:mm:ss.fff} Failed to save config: {ex.Message}");
        }
    }

    private void UpdateVisualForRawEvent(RawDeviceEvent deviceEvent)
    {
        switch (deviceEvent.Kind)
        {
            case RawDeviceEventKind.Pressed:
                GetVisual(deviceEvent.InputId).IsActive = true;
                break;
            case RawDeviceEventKind.Released:
                GetVisual(deviceEvent.InputId).IsActive = false;
                break;
            case RawDeviceEventKind.RotatedClockwise:
                FlashVisual(EncoderClockwise);
                break;
            case RawDeviceEventKind.RotatedCounterClockwise:
                FlashVisual(EncoderCounterClockwise);
                break;
        }
    }

    private void FlashVisual(InputVisualViewModel visual)
    {
        visual.IsActive = true;
        _ = Task.Run(async () =>
        {
            await Task.Delay(150);
            Dispatcher.UIThread.Post(() => visual.IsActive = false);
        });
    }

    private void AppendLog(string message)
    {
        EventLogEntries.Insert(0, message);
        while (EventLogEntries.Count > 100)
        {
            EventLogEntries.RemoveAt(EventLogEntries.Count - 1);
        }
    }

    private static IReadOnlyList<TriggerType> GetAvailableTriggers(InputId inputId)
    {
        return inputId switch
        {
            InputId.EncoderClockwise => [TriggerType.RotateCw],
            InputId.EncoderCounterClockwise => [TriggerType.RotateCcw],
            _ => [TriggerType.Press, TriggerType.LongPress, TriggerType.DoubleClick],
        };
    }

    private InputVisualViewModel GetVisual(InputId inputId)
    {
        return inputId switch
        {
            InputId.Key1 => Key1,
            InputId.Key2 => Key2,
            InputId.Key3 => Key3,
            InputId.Key4 => Key4,
            InputId.Key5 => Key5,
            InputId.Key6 => Key6,
            InputId.Key7 => Key7,
            InputId.Key8 => Key8,
            InputId.EncoderButton => EncoderButton,
            InputId.EncoderClockwise => EncoderClockwise,
            InputId.EncoderCounterClockwise => EncoderCounterClockwise,
            _ => throw new ArgumentOutOfRangeException(nameof(inputId), inputId, null),
        };
    }

    private IEnumerable<InputVisualViewModel> GetAllVisuals()
    {
        yield return Key1;
        yield return Key2;
        yield return Key3;
        yield return Key4;
        yield return Key5;
        yield return Key6;
        yield return Key7;
        yield return Key8;
        yield return EncoderButton;
        yield return EncoderClockwise;
        yield return EncoderCounterClockwise;
    }

    private static string FormatRawEvent(RawDeviceEvent deviceEvent)
    {
        return $"{deviceEvent.InputId} {deviceEvent.Kind}";
    }

    private static string FormatResolvedEvent(ResolvedTriggerEvent resolvedTriggerEvent)
    {
        return $"{resolvedTriggerEvent.InputId} {resolvedTriggerEvent.TriggerType}";
    }

    private void NotifyActionTypeVisibilityChanged()
    {
        OnPropertyChanged(nameof(IsHotkeySelected));
        OnPropertyChanged(nameof(IsTextSelected));
        OnPropertyChanged(nameof(IsLaunchSelected));
        OnPropertyChanged(nameof(IsUrlSelected));
        OnPropertyChanged(nameof(IsShellSelected));
        OnPropertyChanged(nameof(IsVolumeSelected));
        OnPropertyChanged(nameof(IsMuteSelected));
    }
}
