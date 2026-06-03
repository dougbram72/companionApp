using Avalonia.Controls;
using KeypadCompanion.ViewModels;

namespace KeypadCompanion.Views;

public partial class WifiSettingsWindow : Window
{
    public WifiSettingsWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is WifiSettingsViewModel vm)
        {
            vm.Cleanup();
        }

        base.OnClosed(e);
    }
}
