using KeypadCompanion.Domain;

namespace KeypadCompanion.ViewModels;

public sealed class BindingSlotViewModel
{
    public required InputId InputId { get; init; }
    public required TriggerType TriggerType { get; init; }
    public BindingAction? Action { get; set; }

    public string TriggerLabel => TriggerType switch
    {
        TriggerType.Press => "Press",
        TriggerType.LongPress => "Long Press",
        TriggerType.DoubleClick => "Double Click",
        TriggerType.RotateCw => "Rotate CW",
        TriggerType.RotateCcw => "Rotate CCW",
        _ => TriggerType.ToString(),
    };

    public string Summary => Action?.DisplayName ?? "No action assigned";
}
