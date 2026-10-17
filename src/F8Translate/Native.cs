using System.ComponentModel;
using System.Runtime.InteropServices;

namespace F8Translate;

internal static class Native
{
    internal const int HotkeyMessage = 0x0312;
    internal const int HotkeyId = 1;
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(IntPtr window, int id);
    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    internal static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, INPUT[] inputs, int size);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    internal static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    internal static void RoundWindow(IntPtr window)
    {
        // A preferência só é suportada no Windows 11; no 10 a janela continua normal.
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            int round = 2;
            _ = DwmSetWindowAttribute(window, 33, ref round, sizeof(int));
        }
    }

    // INPUT's union must include MOUSEINPUT, including its native alignment on x64.
    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint Type; public InputUnion Data; }
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT Keyboard;
        [FieldOffset(0)] public MOUSEINPUT Mouse;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort Key, Scan;
        public uint Flags, Time;
        public UIntPtr Extra;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int X, Y;
        public uint Data, Flags, Time;
        public UIntPtr Extra;
    }

    internal static void Ctrl(ushort key)
    {
        static INPUT Make(ushort code, bool up) => new()
        {
            Type = 1,
            Data = new InputUnion { Keyboard = new KEYBDINPUT { Key = code, Flags = up ? 2u : 0u } }
        };
        INPUT[] events = [Make(0x11, false), Make(key, false), Make(key, true), Make(0x11, true)];
        if (SendInput((uint)events.Length, events, Marshal.SizeOf<INPUT>()) != events.Length)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Não foi possível enviar o atalho ao aplicativo.");
    }
}
