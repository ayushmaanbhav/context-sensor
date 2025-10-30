using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ContextSensor.Monitor;

public class ProcessMonitorService : BackgroundService
{
    private readonly ILogger<ProcessMonitorService> _logger;
    private readonly MonitoringSettings _settings;
    private readonly List<DateTime> _restartAttempts = new();
    private int _consecutiveFailures = 0;
    private Process? _monitoredProcess;

    public ProcessMonitorService(
        ILogger<ProcessMonitorService> logger,
        IOptions<MonitorConfiguration> configuration)
    {
        _logger = logger;
        _settings = configuration.Value.Monitoring;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Process Monitor Service starting...");
        _logger.LogInformation("Monitoring process: {ProcessName} at {ProcessPath}", 
            _settings.ProcessName, _settings.ProcessPath);
        _logger.LogInformation("Check interval: {Interval}s, Max restart attempts: {MaxAttempts}", 
            _settings.CheckIntervalSeconds, _settings.MaxRestartAttempts);

        // Wait for initial startup delay
        if (_settings.StartupDelaySeconds > 0)
        {
            _logger.LogInformation("Waiting {Delay}s before starting monitoring...", 
                _settings.StartupDelaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(_settings.StartupDelaySeconds), stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorProcessAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in monitoring loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.CheckIntervalSeconds), stoppingToken);
        }
    }

    private async Task MonitorProcessAsync(CancellationToken cancellationToken)
    {
        bool isRunning = IsProcessRunning();

        if (isRunning)
        {
            // Process is running - reset failure counter
            if (_consecutiveFailures > 0)
            {
                _logger.LogInformation("Process {ProcessName} is running normally. Resetting failure counter.", 
                    _settings.ProcessName);
                _consecutiveFailures = 0;
            }
            return;
        }

        // Process is not running
        _logger.LogWarning("Process {ProcessName} is not running!", _settings.ProcessName);

        // Check restart policy
        if (!ShouldAttemptRestart())
        {
            _logger.LogError("Maximum restart attempts exceeded. Not attempting restart.");
            return;
        }

        // Attempt to start the process
        await AttemptRestartAsync(cancellationToken);
    }

    private bool IsProcessRunning()
    {
        try
        {
            // First check if we have a reference to the process
            if (_monitoredProcess != null && !_monitoredProcess.HasExited)
            {
                return true;
            }

            // Check by process name
            var processes = Process.GetProcessesByName(_settings.ProcessName);
            if (processes.Length > 0)
            {
                _monitoredProcess = processes[0];
                return true;
            }

            // Process not found
            _monitoredProcess = null;
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if process is running");
            return false;
        }
    }

    private bool ShouldAttemptRestart()
    {
        // Clean up old restart attempts (older than 1 hour)
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        _restartAttempts.RemoveAll(dt => dt < oneHourAgo);

        // Check if we've exceeded max attempts per hour
        if (_restartAttempts.Count >= _settings.MaxRestartAttemptsPerHour)
        {
            _logger.LogError("Maximum restart attempts per hour ({MaxAttempts}) exceeded", 
                _settings.MaxRestartAttemptsPerHour);
            return false;
        }

        // Check consecutive failures
        if (_consecutiveFailures >= _settings.MaxRestartAttempts)
        {
            _logger.LogError("Maximum consecutive restart attempts ({MaxAttempts}) exceeded", 
                _settings.MaxRestartAttempts);
            return false;
        }

        return true;
    }

    private async Task AttemptRestartAsync(CancellationToken cancellationToken)
    {
        _consecutiveFailures++;
        _restartAttempts.Add(DateTime.UtcNow);

        // Calculate delay with exponential backoff
        var delay = _settings.RestartDelaySeconds * 
            Math.Pow(_settings.RestartBackoffMultiplier, _consecutiveFailures - 1);
        
        _logger.LogInformation("Attempting to restart process (attempt {Attempt}/{MaxAttempts}) after {Delay}s delay...", 
            _consecutiveFailures, _settings.MaxRestartAttempts, (int)delay);

        await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);

        try
        {
            if (!_settings.EnableAutoStart)
            {
                _logger.LogWarning("Auto-start is disabled. Skipping restart.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_settings.ProcessPath))
            {
                _logger.LogError("Process path is not configured. Cannot start process.");
                return;
            }

            if (!File.Exists(_settings.ProcessPath))
            {
                _logger.LogError("Process executable not found at: {ProcessPath}", _settings.ProcessPath);
                return;
            }

            // Start the process in the active user session (interactive)
            try
            {
                var workingDir = Path.GetDirectoryName(_settings.ProcessPath) ?? string.Empty;

                if (TryLaunchInActiveUserSession(_settings.ProcessPath, workingDir, out var pid, out var error))
                {
                    try
                    {
                        _monitoredProcess = Process.GetProcessById(pid);
                    }
                    catch
                    {
                        _monitoredProcess = null;
                    }

                    _logger.LogInformation("Successfully started process {ProcessName} in user session with PID: {ProcessId}",
                        _settings.ProcessName, pid);
                }
                else
                {
                    _logger.LogError("Failed to start process {ProcessName} in user session. {Error}",
                        _settings.ProcessName, error ?? "Unknown error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while launching process {ProcessName} in user session", _settings.ProcessName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting process {ProcessName}", _settings.ProcessName);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Process Monitor Service stopping...");
        
        // Optionally stop the monitored process when service stops
        // For now, we'll leave it running
        
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _monitoredProcess?.Dispose();
        base.Dispose();
    }

    // Launch ContextSensor.App in the active interactive user session (not Session 0)
    private bool TryLaunchInActiveUserSession(string exePath, string workingDirectory, out int pid, out string? error)
    {
        pid = 0;
        error = null;

        uint sessionId = WTSGetActiveConsoleSessionId();
        if (sessionId == 0xFFFFFFFF)
        {
            error = "No active console session.";
            return false;
        }

        IntPtr userToken = IntPtr.Zero;
        IntPtr primaryToken = IntPtr.Zero;
        IntPtr env = IntPtr.Zero;
        PROCESS_INFORMATION pi = default;

        try
        {
            if (!WTSQueryUserToken(sessionId, out userToken))
            {
                error = $"WTSQueryUserToken failed: {Marshal.GetLastWin32Error()}";
                return false;
            }

            if (!DuplicateTokenEx(userToken, TOKEN_ALL_ACCESS, IntPtr.Zero, SecurityImpersonation, TokenPrimary, out primaryToken))
            {
                error = $"DuplicateTokenEx failed: {Marshal.GetLastWin32Error()}";
                return false;
            }

            if (!CreateEnvironmentBlock(out env, primaryToken, false))
            {
                error = $"CreateEnvironmentBlock failed: {Marshal.GetLastWin32Error()}";
                return false;
            }

            var si = new STARTUPINFO
            {
                cb = Marshal.SizeOf<STARTUPINFO>(),
                lpDesktop = "winsta0\\default"
            };

            string cmdLine = $"\"{exePath}\"";
            uint flags = CREATE_UNICODE_ENVIRONMENT | CREATE_NEW_CONSOLE;

            if (!CreateProcessAsUser(
                primaryToken,
                null,
                cmdLine,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                flags,
                env,
                string.IsNullOrWhiteSpace(workingDirectory) ? null : workingDirectory,
                ref si,
                out pi))
            {
                error = $"CreateProcessAsUser failed: {Marshal.GetLastWin32Error()}";
                return false;
            }

            pid = pi.dwProcessId;
            return true;
        }
        finally
        {
            if (pi.hThread != IntPtr.Zero) CloseHandle(pi.hThread);
            if (pi.hProcess != IntPtr.Zero) CloseHandle(pi.hProcess);
            if (env != IntPtr.Zero) DestroyEnvironmentBlock(env);
            if (primaryToken != IntPtr.Zero) CloseHandle(primaryToken);
            if (userToken != IntPtr.Zero) CloseHandle(userToken);
        }
    }

    // P/Invoke and interop

    private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    private const uint CREATE_NEW_CONSOLE = 0x00000010;

    private const int SecurityImpersonation = 2;
    private const int TokenPrimary = 1;

    private const uint TOKEN_ALL_ACCESS = 0xF01FF;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    [DllImport("kernel32.dll")]
    private static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQueryUserToken(uint SessionId, out IntPtr phToken);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool DuplicateTokenEx(
        IntPtr hExistingToken,
        uint dwDesiredAccess,
        IntPtr lpTokenAttributes,
        int ImpersonationLevel,
        int TokenType,
        out IntPtr phNewToken);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcessAsUser(
        IntPtr hToken,
        string? lpApplicationName,
        string? lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
}
