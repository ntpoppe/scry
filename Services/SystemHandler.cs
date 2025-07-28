using Scry.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Scry.Services;

public class SystemHandler : ICommandHandler
{
    public string Prefix => "sys";
    public string Description => "OS shortcuts and controls";
    public bool IsEntryless => false;

    private readonly Dictionary<string, Func<ExecuteResult>> _map;

    public SystemHandler()
    {
        _map = new Dictionary<string, Func<ExecuteResult>>(StringComparer.OrdinalIgnoreCase)
        {
            ["lock"] = LockScreen,
            ["sleep"] = Sleep,
            ["shutdown"] = Shutdown,
            ["restart"] = Restart,
            ["mute"] = Mute,
            ["tasks"] = TaskManager,
            ["cpanel"] = ControlPanel,
            ["devices"] = DeviceManager,
            ["settings"] = SystemSettings
        };
    }

    public IEnumerable<ListEntry> GetOptions()
    {
        var primaryCommands = new[]
        {
            "lock",
            "sleep",
            "shutdown",
            "restart",
            "mute",
            "tasks",
            "cpanel",
            "devices",
            "settings"
        };

        return primaryCommands.Select(cmd => new ListEntry(cmd, GetDescription(cmd)));
    }

    private string GetDescription(string command)
        => command switch
        {
            "lock" => "lock the screen",
            "sleep" => "put computer to sleep",
            "shutdown" => "shutdown the computer",
            "restart" => "restart the computer",
            "mute" => "toggle mute",
            "tasks" => "open task manager",
            "cpanel" => "open control panel",
            "devices" => "open device manager",
            "settings" => "open system settings",
            _ => "system command"
        };

    public ExecuteResult Execute(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return new ExecuteResult(false, "System command cannot be empty");

        var parts = command.ToLowerInvariant().Trim().Split(' ', 2);
        var action = parts[0];
        var parameter = parts.Length > 1 ? parts[1] : string.Empty;

        if (_map.TryGetValue(command, out var handler))
        {
            try
            {
                return handler();
            }
            catch (Exception ex)
            {
                return new ExecuteResult(false, $"Failed to execute system command: {ex.Message}");
            }
        }

        return new ExecuteResult(false, $"Unknown system command: {command}");
    }

    private ExecuteResult LockScreen()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("rundll32.exe", "user32.dll,LockWorkStation");
            return new ExecuteResult(true);
        }

        return new ExecuteResult(false, "Lock screen not supported on this platform");
    }

    private ExecuteResult Sleep()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("powercfg", "/hibernate off");
            Process.Start("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0");
            return new ExecuteResult(true);
        }

        return new ExecuteResult(false, "Sleep not supported on this platform");
    }

    private ExecuteResult Shutdown()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("shutdown", "/s /t 0");
            return new ExecuteResult(true);
        }

        return new ExecuteResult(false, "Shutdown not supported on this platform");
    }

    private ExecuteResult Restart()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("shutdown", "/r /t 0");
            return new ExecuteResult(true);
        }

        return new ExecuteResult(false, "Restart not supported on this platform");
    }
    private ExecuteResult Mute()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("powershell", "-Command \"(New-Object -ComObject WScript.Shell).SendKeys([char]173)\"");
            return new ExecuteResult(true);
        }

        return new ExecuteResult(false, "Mute not supported on this platform");
    }

    private ExecuteResult TaskManager()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("taskmgr");
            return new ExecuteResult(true);
        }

        return new ExecuteResult(false, "Task manager not supported on this platform");
    }

    private ExecuteResult ControlPanel()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("control");
            return new ExecuteResult(true);
        }

        return new ExecuteResult(false, "Control panel not supported on this platform");
    }

    private ExecuteResult DeviceManager()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("devmgmt.msc");
            return new ExecuteResult(true);
        }

        return new ExecuteResult(false, "Device manager not supported on this platform");
    }

    private ExecuteResult SystemSettings()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("ms-settings:");
            return new ExecuteResult(true);
        }

        return new ExecuteResult(false, "System settings not supported on this platform");
    }
}
