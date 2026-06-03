using System.Runtime.InteropServices;

namespace KeypadCompanion.Services.Interop;

internal static class Win32Input
{
    private const int InputKeyboard = 1;
    private const uint KeyEventfKeyUp = 0x0002;
    private const uint KeyEventfUnicode = 0x0004;

    private static readonly IReadOnlyDictionary<string, ushort> KeyMap = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
    {
        ["CTRL"] = 0x11,
        ["CONTROL"] = 0x11,
        ["SHIFT"] = 0x10,
        ["ALT"] = 0x12,
        ["WIN"] = 0x5B,
        ["LWIN"] = 0x5B,
        ["RWIN"] = 0x5C,
        ["ENTER"] = 0x0D,
        ["TAB"] = 0x09,
        ["SPACE"] = 0x20,
        ["ESC"] = 0x1B,
        ["ESCAPE"] = 0x1B,
        ["UP"] = 0x26,
        ["DOWN"] = 0x28,
        ["LEFT"] = 0x25,
        ["RIGHT"] = 0x27,
        ["BACKSPACE"] = 0x08,
        ["DELETE"] = 0x2E,
        ["HOME"] = 0x24,
        ["END"] = 0x23,
        ["PGUP"] = 0x21,
        ["PGDN"] = 0x22,
        ["VOLUMEUP"] = 0xAF,
        ["VOLUMEDOWN"] = 0xAE,
        ["VOLUMEMUTE"] = 0xAD,
    };

    public static void SendHotkey(string chord)
    {
        var parts = chord
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseVirtualKey)
            .ToArray();

        if (parts.Length == 0)
        {
            return;
        }

        var inputs = new List<INPUT>(parts.Length * 2);
        foreach (var part in parts)
        {
            inputs.Add(CreateVirtualKeyInput(part, isKeyUp: false));
        }

        for (var i = parts.Length - 1; i >= 0; i--)
        {
            inputs.Add(CreateVirtualKeyInput(parts[i], isKeyUp: true));
        }

        SendInputs(inputs);
    }

    public static void SendText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var inputs = new List<INPUT>(text.Length * 2);
        foreach (var character in text)
        {
            inputs.Add(CreateUnicodeInput(character, isKeyUp: false));
            inputs.Add(CreateUnicodeInput(character, isKeyUp: true));
        }

        SendInputs(inputs);
    }

    private static ushort ParseVirtualKey(string token)
    {
        if (KeyMap.TryGetValue(token, out var mapped))
        {
            return mapped;
        }

        if (token.Length == 1)
        {
            var character = char.ToUpperInvariant(token[0]);
            if (character is >= 'A' and <= 'Z')
            {
                return character;
            }

            if (character is >= '0' and <= '9')
            {
                return character;
            }
        }

        if (token.StartsWith("F", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(token[1..], out var functionNumber) &&
            functionNumber is >= 1 and <= 24)
        {
            return (ushort)(0x70 + functionNumber - 1);
        }

        throw new InvalidOperationException($"Unsupported hotkey token '{token}'.");
    }

    private static INPUT CreateVirtualKeyInput(ushort virtualKey, bool isKeyUp)
    {
        return new INPUT
        {
            type = InputKeyboard,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    dwFlags = isKeyUp ? KeyEventfKeyUp : 0,
                },
            },
        };
    }

    private static INPUT CreateUnicodeInput(char character, bool isKeyUp)
    {
        return new INPUT
        {
            type = InputKeyboard,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wScan = character,
                    dwFlags = KeyEventfUnicode | (isKeyUp ? KeyEventfKeyUp : 0),
                },
            },
        };
    }

    private static void SendInputs(IReadOnlyList<INPUT> inputs)
    {
        if (inputs.Count == 0)
        {
            return;
        }

        var sent = SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
        if (sent != inputs.Count)
        {
            throw new InvalidOperationException($"SendInput failed with error {Marshal.GetLastWin32Error()}.");
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }
}
