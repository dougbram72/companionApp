using KeypadCompanion.Domain;

namespace KeypadCompanion.Tests;

public sealed class AppConfigTests
{
    [Fact]
    public void Normalize_AddsMissingDefaultEncoderBindings()
    {
        var config = new AppConfig
        {
            ActiveProfile = new ProfileConfig
            {
                Bindings =
                [
                    new BindingEntry
                    {
                        InputId = InputId.Key1,
                        TriggerType = TriggerType.Press,
                        Action = new HotkeyAction { Chord = "Ctrl+1" },
                    },
                ],
            },
            ActionPresets = [],
        };

        config.Normalize();

        Assert.Contains(config.ActiveProfile.Bindings, binding =>
            binding.InputId == InputId.EncoderClockwise &&
            binding.TriggerType == TriggerType.RotateCw &&
            binding.Action is SetVolumeDeltaAction { DeltaPercent: > 0 });

        Assert.Contains(config.ActiveProfile.Bindings, binding =>
            binding.InputId == InputId.EncoderCounterClockwise &&
            binding.TriggerType == TriggerType.RotateCcw &&
            binding.Action is SetVolumeDeltaAction { DeltaPercent: < 0 });

        Assert.Contains(config.ActiveProfile.Bindings, binding =>
            binding.InputId == InputId.EncoderButton &&
            binding.TriggerType == TriggerType.Press &&
            binding.Action is ToggleMuteAction);
    }
}
