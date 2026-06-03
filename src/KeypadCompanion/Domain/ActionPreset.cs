namespace KeypadCompanion.Domain;

public sealed class ActionPreset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public BindingAction Action { get; set; } = new ToggleMuteAction();
    // Empty string means "auto-derive from action type".
    public string IconCode { get; set; } = string.Empty;

    public static List<ActionPreset> CreateDefaults()
    {
        return
        [
            new ActionPreset
            {
                Name = "Volume Up 2%",
                Action = new SetVolumeDeltaAction { DeltaPercent = 2.0 },
            },
            new ActionPreset
            {
                Name = "Volume Down 2%",
                Action = new SetVolumeDeltaAction { DeltaPercent = -2.0 },
            },
            new ActionPreset
            {
                Name = "Toggle Mute",
                Action = new ToggleMuteAction(),
            },
        ];
    }
}
