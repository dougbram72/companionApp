using KeypadCompanion.Domain;

namespace KeypadCompanion.ViewModels;

public sealed class ActionPresetViewModel
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required BindingAction Action { get; init; }
    public required string IconCode { get; init; }

    public string Summary => Action.DisplayName;
}
