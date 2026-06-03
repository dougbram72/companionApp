using System.IO.Ports;
using KeypadCompanion.Domain;

namespace KeypadCompanion.Services;

public sealed class SerialDeviceService(DeviceEventParser parser) : ISerialDeviceService
{
    private readonly object _sync = new();
    private readonly DeviceEventParser _parser = parser;

    private SerialPort? _serialPort;
    private CancellationTokenSource? _readLoopCancellation;
    private Task? _readLoopTask;

    public event EventHandler<RawDeviceEvent>? RawDeviceEventReceived;
    public event EventHandler<SerialConnectionStatus>? ConnectionStatusChanged;
    public event EventHandler<string>? WifiIpDiscovered;

    public bool IsConnected { get; private set; }
    public string? ConnectedPort { get; private set; }

    public IReadOnlyList<string> GetAvailablePorts()
    {
        return SerialPort.GetPortNames()
            .OrderBy(static port => port, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public Task ConnectAsync(string portName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portName))
        {
            throw new ArgumentException("A COM port is required.", nameof(portName));
        }

        lock (_sync)
        {
            DisconnectCore();

            _serialPort = new SerialPort(portName, 115200)
            {
                NewLine = "\n",
                ReadTimeout = 500,
                DtrEnable = true,
                RtsEnable = true,
            };

            _serialPort.Open();
            _readLoopCancellation = new CancellationTokenSource();
            _readLoopTask = Task.Run(() => ReadLoopAsync(_serialPort, _readLoopCancellation.Token), cancellationToken);

            IsConnected = true;
            ConnectedPort = portName;
        }

        ConnectionStatusChanged?.Invoke(this, new SerialConnectionStatus(true, portName, $"Connected to {portName}."));
        return Task.CompletedTask;
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
            try
            {
                await readLoopTask;
            }
            catch
            {
                // Ignore background shutdown errors.
            }
        }
    }

    public Task SendLineAsync(string line)
    {
        lock (_sync)
        {
            if (_serialPort is { IsOpen: true })
            {
                _serialPort.WriteLine(line);
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

        if (_serialPort is not null)
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
            }
            catch
            {
                // Ignore close errors.
            }

            _serialPort.Dispose();
            _serialPort = null;
        }

        _readLoopTask = null;

        if (IsConnected)
        {
            ConnectionStatusChanged?.Invoke(this, new SerialConnectionStatus(false, ConnectedPort, "Disconnected."));
        }

        IsConnected = false;
        ConnectedPort = null;
    }

    private async Task ReadLoopAsync(SerialPort serialPort, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var line = serialPort.ReadLine();
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("WIFI_IP:", StringComparison.OrdinalIgnoreCase))
                    {
                        var ip = trimmed["WIFI_IP:".Length..];
                        WifiIpDiscovered?.Invoke(this, ip);
                    }
                    else if (_parser.TryParse(line, out var deviceEvent) && deviceEvent is not null)
                    {
                        RawDeviceEventReceived?.Invoke(this, deviceEvent);
                    }
                }
                catch (TimeoutException)
                {
                    // Poll loop.
                }
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            IsConnected = false;
            var portName = ConnectedPort;
            ConnectedPort = null;
            ConnectionStatusChanged?.Invoke(this, new SerialConnectionStatus(false, portName, $"Connection lost: {ex.Message}"));
        }
        finally
        {
            await Task.CompletedTask;
        }
    }
}
