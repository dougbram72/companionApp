namespace KeypadCompanion.Domain;

public sealed class AppConfig
{
    public ConnectionSettings Connection { get; set; } = new();
    public GestureSettings Gesture { get; set; } = new();
    public WindowSettings Window { get; set; } = new();
    public WiFiSettings WiFi { get; set; } = new();
    public List<ActionPreset> ActionPresets { get; set; } = ActionPreset.CreateDefaults();
    public ProfileConfig ActiveProfile { get; set; } = ProfileConfig.CreateDefault();

    public static AppConfig CreateDefault() => new();

    public void Normalize()
    {
        Connection ??= new ConnectionSettings();
        Gesture ??= new GestureSettings();
        Window ??= new WindowSettings();
        WiFi ??= new WiFiSettings();
        ActionPresets ??= ActionPreset.CreateDefaults();
        ActiveProfile ??= ProfileConfig.CreateDefault();
        ActiveProfile.Name ??= "Default";
        ActiveProfile.Bindings ??= [];

        foreach (var defaultPreset in ActionPreset.CreateDefaults())
        {
            if (!ActionPresets.Any(existing => existing.Name.Equals(defaultPreset.Name, StringComparison.OrdinalIgnoreCase)))
            {
                ActionPresets.Add(defaultPreset);
            }
        }

        EnsureBinding(InputId.EncoderClockwise, TriggerType.RotateCw, new SetVolumeDeltaAction { DeltaPercent = 2.0 });
        EnsureBinding(InputId.EncoderCounterClockwise, TriggerType.RotateCcw, new SetVolumeDeltaAction { DeltaPercent = -2.0 });
        EnsureBinding(InputId.EncoderButton, TriggerType.Press, new ToggleMuteAction());
    }

    private void EnsureBinding(InputId inputId, TriggerType triggerType, BindingAction action)
    {
        if (ActiveProfile.Bindings.Any(existing => existing.InputId == inputId && existing.TriggerType == triggerType))
        {
            return;
        }

        ActiveProfile.Bindings.Add(new BindingEntry
        {
            InputId = inputId,
            TriggerType = triggerType,
            Action = action,
        });
    }
}

public sealed class ConnectionSettings
{
    public bool AutoConnect { get; set; } = true;
    public string? LastSuccessfulPort { get; set; }
}

public sealed class GestureSettings
{
    public int LongPressMilliseconds { get; set; } = 500;
    public int DoubleClickMilliseconds { get; set; } = 275;
}

public sealed class WindowSettings
{
    public bool MinimizeToTray { get; set; } = true;
}

public sealed class WiFiSettings
{
    public string LastKnownIp { get; set; } = string.Empty;
    public string LastUsedSsid { get; set; } = string.Empty;
}

public sealed class ProfileConfig
{
    public string Name { get; set; } = "Default";
    public List<BindingEntry> Bindings { get; set; } = [];

    public static ProfileConfig CreateDefault()
    {
        return new ProfileConfig
        {
            Bindings =
            [
                new BindingEntry
                {
                    InputId = InputId.EncoderClockwise,
                    TriggerType = TriggerType.RotateCw,
                    Action = new SetVolumeDeltaAction { DeltaPercent = 2.0 },
                },
                new BindingEntry
                {
                    InputId = InputId.EncoderCounterClockwise,
                    TriggerType = TriggerType.RotateCcw,
                    Action = new SetVolumeDeltaAction { DeltaPercent = -2.0 },
                },
                new BindingEntry
                {
                    InputId = InputId.EncoderButton,
                    TriggerType = TriggerType.Press,
                    Action = new ToggleMuteAction(),
                },
            ],
        };
    }
}
