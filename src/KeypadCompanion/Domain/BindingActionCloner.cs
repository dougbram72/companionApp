namespace KeypadCompanion.Domain;

public static class BindingActionCloner
{
    public static BindingAction Clone(BindingAction action)
    {
        return action switch
        {
            HotkeyAction hotkeyAction => new HotkeyAction { Chord = hotkeyAction.Chord },
            TextSnippetAction textSnippetAction => new TextSnippetAction { Text = textSnippetAction.Text },
            LaunchAppAction launchAppAction => new LaunchAppAction
            {
                FilePath = launchAppAction.FilePath,
                Arguments = launchAppAction.Arguments,
                WorkingDirectory = launchAppAction.WorkingDirectory,
            },
            OpenUrlAction openUrlAction => new OpenUrlAction { Url = openUrlAction.Url },
            ShellCommandAction shellCommandAction => new ShellCommandAction
            {
                FileName = shellCommandAction.FileName,
                Arguments = shellCommandAction.Arguments,
                WorkingDirectory = shellCommandAction.WorkingDirectory,
            },
            SetVolumeDeltaAction setVolumeDeltaAction => new SetVolumeDeltaAction { DeltaPercent = setVolumeDeltaAction.DeltaPercent },
            ToggleMuteAction => new ToggleMuteAction(),
            _ => throw new InvalidOperationException($"Unsupported action type '{action.GetType().Name}'."),
        };
    }
}
