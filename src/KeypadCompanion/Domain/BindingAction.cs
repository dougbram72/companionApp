using System.Text.Json.Serialization;

namespace KeypadCompanion.Domain;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(HotkeyAction), typeDiscriminator: "hotkey")]
[JsonDerivedType(typeof(TextSnippetAction), typeDiscriminator: "text")]
[JsonDerivedType(typeof(LaunchAppAction), typeDiscriminator: "launch")]
[JsonDerivedType(typeof(OpenUrlAction), typeDiscriminator: "url")]
[JsonDerivedType(typeof(ShellCommandAction), typeDiscriminator: "shell")]
[JsonDerivedType(typeof(SetVolumeDeltaAction), typeDiscriminator: "volumeDelta")]
[JsonDerivedType(typeof(ToggleMuteAction), typeDiscriminator: "toggleMute")]
public abstract class BindingAction
{
    public abstract string DisplayName { get; }
}

public sealed class HotkeyAction : BindingAction
{
    public string Chord { get; set; } = string.Empty;

    public override string DisplayName => $"Hotkey: {Chord}";
}

public sealed class TextSnippetAction : BindingAction
{
    public string Text { get; set; } = string.Empty;

    public override string DisplayName => $"Type text: {Text}";
}

public sealed class LaunchAppAction : BindingAction
{
    public string FilePath { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;

    public override string DisplayName => $"Launch: {FilePath}";
}

public sealed class OpenUrlAction : BindingAction
{
    public string Url { get; set; } = string.Empty;

    public override string DisplayName => $"URL: {Url}";
}

public sealed class ShellCommandAction : BindingAction
{
    public string FileName { get; set; } = "powershell.exe";
    public string Arguments { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;

    public override string DisplayName => $"Shell: {FileName} {Arguments}".Trim();
}

public sealed class SetVolumeDeltaAction : BindingAction
{
    public double DeltaPercent { get; set; }

    public override string DisplayName => DeltaPercent >= 0
        ? $"Volume +{DeltaPercent:0.#}%"
        : $"Volume {DeltaPercent:0.#}%";
}

public sealed class ToggleMuteAction : BindingAction
{
    public override string DisplayName => "Toggle mute";
}
