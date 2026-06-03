using System.Diagnostics;
using KeypadCompanion.Domain;
using KeypadCompanion.Services.Interop;

namespace KeypadCompanion.Services;

public sealed class MacroExecutor(IVolumeService volumeService) : IMacroExecutor
{
    private readonly IVolumeService _volumeService = volumeService;

    public async Task ExecuteAsync(BindingAction action, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (action)
        {
            case HotkeyAction hotkeyAction:
                Win32Input.SendHotkey(hotkeyAction.Chord);
                break;
            case TextSnippetAction textSnippetAction:
                await PasteTextAsync(textSnippetAction.Text, cancellationToken);
                break;
            case LaunchAppAction launchAppAction:
                LaunchProcess(launchAppAction.FilePath, launchAppAction.Arguments, launchAppAction.WorkingDirectory, useShellExecute: true);
                break;
            case OpenUrlAction openUrlAction:
                LaunchProcess(openUrlAction.Url, string.Empty, string.Empty, useShellExecute: true);
                break;
            case ShellCommandAction shellCommandAction:
                LaunchProcess(shellCommandAction.FileName, shellCommandAction.Arguments, shellCommandAction.WorkingDirectory, useShellExecute: false);
                break;
            case SetVolumeDeltaAction setVolumeDeltaAction:
                _volumeService.AdjustVolume(setVolumeDeltaAction.DeltaPercent);
                break;
            case ToggleMuteAction:
                _volumeService.ToggleMute();
                break;
            default:
                throw new InvalidOperationException($"Unsupported action type '{action.GetType().Name}'.");
        }
    }

    private static void LaunchProcess(string fileName, string arguments, string workingDirectory, bool useShellExecute)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("A file path or URL is required.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = useShellExecute,
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        Process.Start(startInfo);
    }

    private static async Task PasteTextAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        string? originalClipboardText = null;
        var hadClipboardText = false;

        try
        {
            try
            {
                originalClipboardText = Win32Clipboard.TryGetText();
                hadClipboardText = originalClipboardText is not null;
            }
            catch
            {
                hadClipboardText = false;
            }

            try
            {
                Win32Clipboard.SetText(text);
                await Task.Delay(150, cancellationToken);
                Win32Input.SendHotkey("Ctrl+V");
                await Task.Delay(150, cancellationToken);
            }
            catch
            {
                Win32Input.SendText(text);
            }
        }
        finally
        {
            try
            {
                if (hadClipboardText)
                {
                    Win32Clipboard.SetText(originalClipboardText!);
                }
            }
            catch
            {
                // Ignore clipboard restore failures.
            }
        }
    }
}
