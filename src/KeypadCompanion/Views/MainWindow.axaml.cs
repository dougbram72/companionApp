using Avalonia;
using Avalonia.Controls;
using KeypadCompanion.Domain;
using KeypadCompanion.ViewModels;

namespace KeypadCompanion.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel &&
            !viewModel.IsExitRequested &&
            viewModel.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WindowStateProperty &&
            change.NewValue is WindowState windowState &&
            windowState == WindowState.Minimized &&
            DataContext is MainWindowViewModel viewModel &&
            viewModel.MinimizeToTray)
        {
            Hide();
            WindowState = WindowState.Normal;
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.AssignmentMenusChanged -= OnAssignmentMenusChanged;
        }

        _viewModel = DataContext as MainWindowViewModel;
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.AssignmentMenusChanged += OnAssignmentMenusChanged;
        RebuildAssignmentMenus();
    }

    private void OnAssignmentMenusChanged(object? sender, EventArgs e)
    {
        RebuildAssignmentMenus();
    }

    private void RebuildAssignmentMenus()
    {
        if (_viewModel is null)
        {
            return;
        }

        SetContextMenu(Key1Button, InputId.Key1);
        SetContextMenu(Key2Button, InputId.Key2);
        SetContextMenu(Key3Button, InputId.Key3);
        SetContextMenu(Key4Button, InputId.Key4);
        SetContextMenu(Key5Button, InputId.Key5);
        SetContextMenu(Key6Button, InputId.Key6);
        SetContextMenu(Key7Button, InputId.Key7);
        SetContextMenu(Key8Button, InputId.Key8);
        SetContextMenu(EncoderButtonButton, InputId.EncoderButton);
        SetContextMenu(EncoderClockwiseButton, InputId.EncoderClockwise);
        SetContextMenu(EncoderCounterClockwiseButton, InputId.EncoderCounterClockwise);
    }

    private void SetContextMenu(Control control, InputId inputId)
    {
        if (_viewModel is null)
        {
            return;
        }

        var menu = new ContextMenu();
        foreach (var trigger in _viewModel.GetAvailableTriggersForInput(inputId))
        {
            var triggerItem = new MenuItem
            {
                Header = $"{FormatTrigger(trigger)} ({_viewModel.GetBindingSummary(inputId, trigger)})",
            };

            var clearItem = new MenuItem
            {
                Header = "Clear",
            };
            clearItem.Click += async (_, _) => await _viewModel.AssignPresetToTriggerAsync(inputId, trigger, null);
            triggerItem.Items.Add(clearItem);
            triggerItem.Items.Add(new Separator());

            foreach (var preset in _viewModel.GetActionPresetsSnapshot())
            {
                var presetItem = new MenuItem
                {
                    Header = $"{preset.Name} - {preset.Summary}",
                };
                presetItem.Click += async (_, _) => await _viewModel.AssignPresetToTriggerAsync(inputId, trigger, preset.Id);
                triggerItem.Items.Add(presetItem);
            }

            menu.Items.Add(triggerItem);
        }

        control.ContextMenu = menu;
    }

    private static string FormatTrigger(TriggerType triggerType)
    {
        return triggerType switch
        {
            TriggerType.Press => "Press",
            TriggerType.LongPress => "Long Press",
            TriggerType.DoubleClick => "Double Click",
            TriggerType.RotateCw => "Rotate CW",
            TriggerType.RotateCcw => "Rotate CCW",
            _ => triggerType.ToString(),
        };
    }
}
