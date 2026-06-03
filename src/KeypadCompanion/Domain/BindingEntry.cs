namespace KeypadCompanion.Domain;

public sealed class BindingEntry
{
    public InputId InputId { get; set; }
    public TriggerType TriggerType { get; set; }
    public BindingAction Action { get; set; } = new ToggleMuteAction();
    // Display text shown on the key tile (preset name).
    public string Label { get; set; } = string.Empty;
    // Icon code sent to the device. Empty = auto-derive from action type.
    public string IconCode { get; set; } = string.Empty;
}
