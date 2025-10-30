using ContextSensor.Core.OS;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ContextSensor.Windows.OS;

/// <summary>
/// Windows-specific implementation for retrieving process information
/// </summary>
public class WindowsProcessInformationProvider : IProcessInformationProvider
{
    private readonly ILogger<WindowsProcessInformationProvider> _logger;

    public WindowsProcessInformationProvider(ILogger<WindowsProcessInformationProvider> logger)
    {
        _logger = logger;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    public string GetProcessName(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName + ".exe";
        }
        catch (ArgumentException)
        {
            _logger.LogWarning("Process with ID {ProcessId} not found", processId);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting process name for PID {ProcessId}", processId);
            return string.Empty;
        }
    }

    public int GetForegroundProcessId()
    {
        try
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                _logger.LogWarning("No foreground window found");
                return 0;
            }

            uint processId;
            GetWindowThreadProcessId(hwnd, out processId);
            return (int)processId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting foreground process ID");
            return 0;
        }
    }

    public bool ProcessExists(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if process {ProcessId} exists", processId);
            return false;
        }
    }
}
