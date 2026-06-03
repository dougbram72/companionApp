using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using KeypadCompanion.Services;
using KeypadCompanion.ViewModels;
using KeypadCompanion.Views;

namespace KeypadCompanion;

public partial class App : Application
{
    private MainWindowViewModel? _mainWindowViewModel;
    private ISerialDeviceService? _serialDeviceService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _serialDeviceService = new SerialDeviceService(new DeviceEventParser());
            var tcpDeviceService = new TcpDeviceService(new DeviceEventParser());
            var gestureResolver = new GestureResolver();
            var macroExecutor = new MacroExecutor(new WindowsVolumeService());
            var configStore = new JsonConfigStore();

            _mainWindowViewModel = new MainWindowViewModel(
                _serialDeviceService,
                tcpDeviceService,
                gestureResolver,
                macroExecutor,
                configStore);
            _mainWindowViewModel.WindowInteractionRequested += OnWindowInteractionRequested;
            _mainWindowViewModel.WifiSettingsRequested += OnWifiSettingsRequested;

            DataContext = _mainWindowViewModel;
            desktop.MainWindow = new MainWindow
            {
                DataContext = _mainWindowViewModel,
            };

            _ = _mainWindowViewModel.InitializeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnWifiSettingsRequested(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is not Window mainWindow ||
            _serialDeviceService is null)
        {
            return;
        }

        var vm = new WifiSettingsViewModel(_serialDeviceService);
        var window = new WifiSettingsWindow { DataContext = vm };
        window.ShowDialog(mainWindow);
    }

    private void OnWindowInteractionRequested(object? sender, WindowInteractionRequest request)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is not Window window)
        {
            return;
        }

        switch (request)
        {
            case WindowInteractionRequest.Show:
                if (!window.IsVisible)
                {
                    window.Show();
                }

                window.WindowState = WindowState.Normal;
                window.Activate();
                break;
            case WindowInteractionRequest.Exit:
                desktop.Shutdown();
                break;
        }
    }
}
