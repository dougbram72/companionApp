using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using KeypadCompanion.Domain;

namespace KeypadCompanion.ViewModels;

public sealed partial class InputVisualViewModel(InputId inputId, string displayName) : ObservableObject
{
    private static readonly SolidColorBrush IdleBrush = new(Color.Parse("#1D293D"));
    private static readonly SolidColorBrush ActiveBrush = new(Color.Parse("#228B5A"));
    private static readonly SolidColorBrush SelectedBrush = new(Color.Parse("#D6A34F"));
    private static readonly SolidColorBrush IdleBorderBrush = new(Color.Parse("#3B4A61"));
    private static readonly SolidColorBrush SelectedBorderBrush = new(Color.Parse("#F3C56B"));

    public InputId InputId { get; } = inputId;
    public string DisplayName { get; } = displayName;

    [ObservableProperty]
    private bool isActive;

    [ObservableProperty]
    private bool isSelected;

    public IBrush Background => IsSelected ? SelectedBrush : IsActive ? ActiveBrush : IdleBrush;
    public IBrush BorderBrush => IsSelected ? SelectedBorderBrush : IdleBorderBrush;

    partial void OnIsActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(Background));
    }

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(Background));
        OnPropertyChanged(nameof(BorderBrush));
    }
}
