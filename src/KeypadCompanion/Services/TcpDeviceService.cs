using System.IO;
using System.Net.Sockets;
using KeypadCompanion.Domain;

namespace KeypadCompanion.Services;

public sealed class TcpDeviceService(DeviceEventParser parser) : ISerialDeviceService
{
    private readonly object _sync = new();
    private readonly DeviceEventParser _parser = parser;

    private TcpClient? _tcpClient;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private CancellationTokenSource? _readLoopCancellation;
    private Task? _readLoopTask;

    public event EventHandler<RawDeviceEvent>? RawDeviceEventReceived;
    public event EventHandler<SerialConnectionStatus>? ConnectionStatusChanged;
    public event EventHandler<string>? WifiIpDiscovered;

    public bool IsConnected { get; private set; }
    public string? ConnectedPort { get; private set; }

    // Returns an empty list — TCP has no COM ports.
    public IReadOnlyList<string> GetAvailablePorts() => [];

    // portName must be "host:port", e.g. "192.168.1.42:4242"
    public async Task ConnectAsync(string portName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portName))
            throw new ArgumentException("host:port is required.", nameof(portName));

        var parts = portName.Split(':', 2);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var port))
            throw new ArgumentException($"Expected host:port, got: {portName}", nameof(portName));

        var host = parts[0];

        lock (_sync) { DisconnectCore(); }

        var client = new TcpClient();
        await client.ConnectAsync(host, port, cancellationToken);

        lock (_sync)
        {
            _tcpClient = client;
            _reader = new StreamReader(client.GetStream(), leaveOpen: true);
            _writer = new StreamWriter(client.GetStream(), leaveOpen: true) { AutoFlush = true };
            _readLoopCancellation = new CancellationTokenSource();
            _readLoopTask = Task.Run(
                () => ReadLoopAsync(_reader, _readLoopCancellation.Token),
                cancellationToken);

            IsConnected = true;
            ConnectedPort = portName;
        }

        ConnectionStatusChanged?.Invoke(this,
            new SerialConnectionStatus(true, portName, $"Connected via WiFi to {portName}."));
    }

    public async Task DisconnectAsync()
    {
        Task? readLoopTask;
        lock (_sync)
        {
            readLoopTask = _readLoopTask;
            DisconnectCore();
        }

        if (readLoopTask is not null)
        {
            try { await readLoopTask; } catch { }
        }
    }

    public Task SendLineAsync(string line)
    {
        lock (_sync)
        {
            if (_writer is not null)
            {
                try { _writer.WriteLine(line); }
                catch { /* ignore if disconnected */ }
            }
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
    }

    private void DisconnectCore()
    {
        _readLoopCancellation?.Cancel();
        _readLoopCancellation?.Dispose();
        _readLoopCancellation = null;

        try { _writer?.Dispose(); } catch { }
        try { _reader?.Dispose(); } catch { }
        try { _tcpClient?.Dispose(); } catch { }

        _writer = null;
        _reader = null;
        _readLoopTask = null;
        _tcpClient = null;

        if (IsConnected)
        {
            ConnectionStatusChanged?.Invoke(this,
                new SerialConnectionStatus(false, ConnectedPort, "WiFi disconnected."));
        }

        IsConnected = false;
        ConnectedPort = null;
    }

    private async Task ReadLoopAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null) break; // connection closed

                var trimmed = line.Trim();
                if (trimmed.StartsWith("WIFI_IP:", StringComparison.OrdinalIgnoreCase))
                {
                    WifiIpDiscovered?.Invoke(this, trimmed["WIFI_IP:".Length..]);
                }
                else if (_parser.TryParse(line, out var deviceEvent) && deviceEvent is not null)
                {
                    RawDeviceEventReceived?.Invoke(this, deviceEvent);
                }
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            IsConnected = false;
            var portName = ConnectedPort;
            ConnectedPort = null;
            ConnectionStatusChanged?.Invoke(this,
                new SerialConnectionStatus(false, portName, $"WiFi connection lost: {ex.Message}"));
        }
    }
}
