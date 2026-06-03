using KeypadCompanion.Domain;

namespace KeypadCompanion.Services;

public interface ISerialDeviceService : IDisposable
{
    event EventHandler<RawDeviceEvent>? RawDeviceEventReceived;
    event EventHandler<SerialConnectionStatus>? ConnectionStatusChanged;
    event EventHandler<string>? WifiIpDiscovered;

    bool IsConnected { get; }
    string? ConnectedPort { get; }

    IReadOnlyList<string> GetAvailablePorts();
    Task ConnectAsync(string portName, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task SendLineAsync(string line);
}

public interface IGestureResolver : IDisposable
{
    event EventHandler<ResolvedTriggerEvent>? TriggerResolved;

    void UpdateSettings(GestureSettings settings);
    void HandleRawEvent(RawDeviceEvent deviceEvent);
}

public interface IMacroExecutor
{
    Task ExecuteAsync(BindingAction action, CancellationToken cancellationToken = default);
}

public interface IVolumeService
{
    void AdjustVolume(double deltaPercent);
    void ToggleMute();
}

public interface IConfigStore
{
    Task<AppConfig> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppConfig config, CancellationToken cancellationToken = default);
}

public sealed record SerialConnectionStatus(
    bool IsConnected,
    string? PortName,
    string Message);
