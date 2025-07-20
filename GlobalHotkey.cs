using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Scry;

/// <summary>
/// Listens for low-level keyboard events when the app window is closed.
/// </summary>
internal static class GlobalHotkey
{
    // Hook type for global low-level keyboard events
    private const int WH_KEYBOARD_LL = 13;
    // Windows message ID for key down
    private const int WM_KEYDOWN = 0x0100;
    // Virtual keycode for Space and Control
    private const int VK_SPACE = 0x20;
    private const int VK_CONTROL = 0x11;

    // Delegate type matching the Win32 hook prototype
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private static readonly LowLevelKeyboardProc _proc = HookCallback;

    // Handle to the installed hook
    private static IntPtr _hookID = IntPtr.Zero;

    // User-provided callback to invoke when hotkey is pressed
    private static Action? _onWake;

    /// <summary>
    /// Registers the global hotkey listener. Call once at startup.
    /// </summary>
    /// <param name="onWake">Callback invoked on Ctrl+Space.</param>
    public static void Register(Action onWake)
    {
        _onWake = onWake;
        _hookID = SetHook(_proc);
    }

    /// <summary>
    /// P/Invoke to install the hook into the calling process.
    /// </summary>
    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        if (curModule == null)
            throw new InvalidOperationException("curModule null while setting global hotkey hook callback");

        // Pass module handle and hook type
        return SetWindowsHookEx(
            WH_KEYBOARD_LL,
            proc,
            GetModuleHandle(curModule.ModuleName),
            0);
    }

    /// <summary>
    /// Called by the OS for every keyboard event.
    /// Filters for Ctrl+Space and invokes our callback on the UI thread.
    /// </summary>
    /// <param name="nCode">The type of the event</param>
    /// <param name="wParam">The type of message</param>
    /// <param name="lParam">Pointer to KBDLLHOOKSTRUCT (keyboard event data)</param>
    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // Only handle key-down events with a valid code
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            Console.WriteLine("Toggled");

            // If "Space" pressed while "Control" is held down in this moment
            if (vkCode == VK_SPACE && (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0)
            {
                // Post back to Avalonia's UI thread to toggle the window
                Dispatcher.UIThread.Post(() => _onWake?.Invoke());
            }
        }

        // Call the next hook in the chain so other apps/hooks still get key events
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    #region Win32 API
    // Installs a hook procedure into the hook chain. idHook=WH_KEYBOARD_LL for low-level keyboard events.
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelKeyboardProc lpfn,
        IntPtr hMod,
        uint dwThreadId);

    // Removes a hook procedure installed in SetWindowsHookEx.
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    // Passes the hook information to the next hook procedure in the current hook chain.
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(
        IntPtr hhk,
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    // Retrieves a module handle for the specified module. In this case, this executables module.
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    // Retrieves the state of the specified virtual key. High-order bit set = key is down. Gets the "Space" key state in this context.
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
    #endregion
}
