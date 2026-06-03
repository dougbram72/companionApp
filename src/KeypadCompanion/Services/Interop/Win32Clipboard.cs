using System.Runtime.InteropServices;

namespace KeypadCompanion.Services.Interop;

internal static class Win32Clipboard
{
    private const uint CfUnicodeText = 13;
    private const uint GmemMoveable = 0x0002;

    public static string? TryGetText()
    {
        ExecuteWithClipboard(() =>
        {
            if (!IsClipboardFormatAvailable(CfUnicodeText))
            {
                return null;
            }

            var handle = GetClipboardData(CfUnicodeText);
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            var pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return Marshal.PtrToStringUni(pointer);
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }, out string? text);

        return text;
    }

    public static void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        ExecuteWithClipboard(() =>
        {
            if (!EmptyClipboard())
            {
                throw new InvalidOperationException($"EmptyClipboard failed with error {Marshal.GetLastWin32Error()}.");
            }

            var bytes = (text.Length + 1) * sizeof(char);
            var handle = GlobalAlloc(GmemMoveable, (UIntPtr)bytes);
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"GlobalAlloc failed with error {Marshal.GetLastWin32Error()}.");
            }

            var pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero)
            {
                GlobalFree(handle);
                throw new InvalidOperationException($"GlobalLock failed with error {Marshal.GetLastWin32Error()}.");
            }

            try
            {
                Marshal.Copy(text.ToCharArray(), 0, pointer, text.Length);
                Marshal.WriteInt16(pointer, text.Length * sizeof(char), 0);
            }
            finally
            {
                GlobalUnlock(handle);
            }

            if (SetClipboardData(CfUnicodeText, handle) == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                GlobalFree(handle);
                throw new InvalidOperationException($"SetClipboardData failed with error {error}.");
            }

            return true;
        }, out _);
    }

    private static void ExecuteWithClipboard<T>(Func<T> action, out T result)
    {
        const int maxAttempts = 10;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                Thread.Sleep(20);
                continue;
            }

            try
            {
                result = action();
                return;
            }
            finally
            {
                CloseClipboard();
            }
        }

        throw new InvalidOperationException($"Unable to access the clipboard. Last error: {Marshal.GetLastWin32Error()}.");
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);
}
