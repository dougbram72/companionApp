using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeypadCompanion.Services;

namespace KeypadCompanion.ViewModels;

public partial class WifiSettingsViewModel : ViewModelBase
{
    private readonly ISerialDeviceService _serialDeviceService;

    public WifiSettingsViewModel(ISerialDeviceService serialDeviceService)
    {
        _serialDeviceService = serialDeviceService;
        _serialDeviceService.WifiIpDiscovered += OnWifiIpDiscovered;
    }

    [ObservableProperty]
    private string ssid = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string status = "Enter the WiFi credentials and click Send.";

    [ObservableProperty]
    private string discoveredIp = string.Empty;

    [ObservableProperty]
    private bool hasDiscoveredIp;

    [RelayCommand]
    private async Task SendToDeviceAsync()
    {
        if (string.IsNullOrWhiteSpace(Ssid))
        {
            Status = "SSID cannot be empty.";
            return;
        }

        if (!_serialDeviceService.IsConnected)
        {
            Status = "Device is not connected via serial. Connect first.";
            return;
        }

        Status = "Sending credentials to device...";
        await _serialDeviceService.SendLineAsync($"WIFI_CONFIG:{Ssid}:{Password}");
        Status = "Sent. Waiting for the device to connect to WiFi...";
    }

    public void Cleanup()
    {
        _serialDeviceService.WifiIpDiscovered -= OnWifiIpDiscovered;
    }

    private void OnWifiIpDiscovered(object? sender, string ip)
    {
        Dispatcher.UIThread.Post(() =>
        {
            DiscoveredIp = ip;
            HasDiscoveredIp = true;
            Status = ip == "failed"
                ? "Device failed to connect. Check SSID and password."
                : $"Device connected at {ip}. You can now use Connect WiFi.";
        });
    }
}
