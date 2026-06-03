using KeypadCompanion.Domain;
using KeypadCompanion.Services;
using KeypadCompanion.ViewModels;

namespace KeypadCompanion.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task SaveActionPreset_UsesActionDisplayNameWhenNameIsBlank()
    {
        var configStore = new FakeConfigStore();
        using var viewModel = new MainWindowViewModel(
            new FakeSerialDeviceService(),
            new FakeGestureResolver(),
            new FakeMacroExecutor(),
            configStore);

        viewModel.ActionPresetName = "   ";
        viewModel.SelectedActionType = "Hotkey";
        viewModel.HotkeyChord = "Win+Shift+S";

        await viewModel.SaveActionPresetCommand.ExecuteAsync(null);

        var savedAction = Assert.Single(viewModel.ActionPresets, preset => preset.Name == "Hotkey: Win+Shift+S");
        Assert.Equal("Hotkey: Win+Shift+S", savedAction.Summary);
        Assert.NotNull(configStore.SavedConfig);
        Assert.Contains(configStore.SavedConfig!.ActionPresets, preset => preset.Name == "Hotkey: Win+Shift+S");
        Assert.Contains(viewModel.EventLogEntries, entry => entry.Contains("Saved action 'Hotkey: Win+Shift+S'.", StringComparison.Ordinal));
    }

    private sealed class FakeSerialDeviceService : ISerialDeviceService
    {
        public event EventHandler<RawDeviceEvent>? RawDeviceEventReceived
        {
            add { }
            remove { }
        }

        public event EventHandler<SerialConnectionStatus>? ConnectionStatusChanged
        {
            add { }
            remove { }
        }

        public bool IsConnected => false;
        public string? ConnectedPort => null;

        public IReadOnlyList<string> GetAvailablePorts() => [];
        public Task ConnectAsync(string portName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DisconnectAsync() => Task.CompletedTask;
        public void Dispose() { }
    }

    private sealed class FakeGestureResolver : IGestureResolver
    {
        public event EventHandler<ResolvedTriggerEvent>? TriggerResolved
        {
            add { }
            remove { }
        }

        public void UpdateSettings(GestureSettings settings) { }
        public void HandleRawEvent(RawDeviceEvent deviceEvent) { }
        public void Dispose() { }
    }

    private sealed class FakeMacroExecutor : IMacroExecutor
    {
        public Task ExecuteAsync(BindingAction action, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeConfigStore : IConfigStore
    {
        public AppConfig? SavedConfig { get; private set; }

        public Task<AppConfig> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(AppConfig.CreateDefault());

        public Task SaveAsync(AppConfig config, CancellationToken cancellationToken = default)
        {
            SavedConfig = config;
            return Task.CompletedTask;
        }
    }
}
