using ContextSensor.Core.Configuration;
using ContextSensor.Core.Events;
using ContextSensor.Core.OS;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace ContextSensor.Windows.Events;

/// <summary>
/// Windows-specific mouse event listener using low-level mouse hooks
/// </summary>
public class MouseEventListener : IEventListener
{
    private readonly ILogger<MouseEventListener> _logger;
    private readonly IProcessInformationProvider _processInfoProvider;
    private readonly EventSchema _eventSchema;
    private readonly IEventTypeMapper _eventTypeMapper;
    private IntPtr _hookId = IntPtr.Zero;
    private int _isListening; // 0 = not listening, 1 = listening
    private readonly LowLevelMouseProc _proc;
    private bool _disposed;

    public string Name => "MouseEventListener";
    public bool IsListening => Interlocked.CompareExchange(ref _isListening, 0, 0) == 1;
    public event EventHandler<IEventData>? EventCaptured;

    // Windows Hook Constants
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;

    // Windows API Imports
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    public MouseEventListener(
        ILogger<MouseEventListener> logger,
        IProcessInformationProvider processInfoProvider,
        EventSchema eventSchema,
        IEventTypeMapper eventTypeMapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _processInfoProvider = processInfoProvider ?? throw new ArgumentNullException(nameof(processInfoProvider));
        _eventSchema = eventSchema ?? throw new ArgumentNullException(nameof(eventSchema));
        _eventTypeMapper = eventTypeMapper ?? throw new ArgumentNullException(nameof(eventTypeMapper));
        _proc = HookCallback; // Store delegate to prevent garbage collection
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _isListening, 1, 0) != 0)
        {
            _logger.LogWarning("MouseEventListener is already listening");
            return Task.CompletedTask;
        }

        try
        {
            _logger.LogInformation("Starting MouseEventListener");

            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                if (curModule != null)
                {
                    _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }

            if (_hookId == IntPtr.Zero)
            {
                var errorCode = Marshal.GetLastWin32Error();
                Interlocked.Exchange(ref _isListening, 0);
                throw new InvalidOperationException($"Failed to set mouse hook. Error code: {errorCode}");
            }

            _logger.LogInformation("MouseEventListener started successfully");

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Interlocked.Exchange(ref _isListening, 0);
            _logger.LogError(ex, "Error starting MouseEventListener");
            throw;
        }
    }

    public Task StopAsync()
    {
        if (Interlocked.CompareExchange(ref _isListening, 0, 1) != 1)
        {
            _logger.LogWarning("MouseEventListener is not listening");
            return Task.CompletedTask;
        }

        try
        {
            _logger.LogInformation("Stopping MouseEventListener");

            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }

            _logger.LogInformation("MouseEventListener stopped successfully");

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping MouseEventListener");
            throw;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                
                // Handle different mouse button events
                if (wParam == (IntPtr)WM_LBUTTONDOWN)
                {
                    CaptureMouseEvent("WM_LBUTTONDOWN", hookStruct);
                }
                else if (wParam == (IntPtr)WM_RBUTTONDOWN)
                {
                    CaptureMouseEvent("WM_RBUTTONDOWN", hookStruct);
                }
                else if (wParam == (IntPtr)WM_MBUTTONDOWN)
                {
                    CaptureMouseEvent("WM_MBUTTONDOWN", hookStruct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in mouse hook callback");
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private void CaptureMouseEvent(string eventType, MSLLHOOKSTRUCT hookStruct)
    {
        try
        {
            // Check if this event is defined in the schema
            if (_eventSchema != null)
            {
                // Map OS event type to schema event name using the mapper
                var schemaEventName = _eventTypeMapper.MapToSchemaName(eventType);
                var eventDef = _eventSchema.Events.FirstOrDefault(e =>
                    e.Name.Equals(schemaEventName, StringComparison.OrdinalIgnoreCase));
                
                // If schema is provided but event is not defined, skip it
                if (eventDef == null)
                {
                    _logger.LogDebug("Event {EventType} not defined in schema, skipping", eventType);
                    return;
                }
            }

            var processId = _processInfoProvider.GetForegroundProcessId();
            var processName = _processInfoProvider.GetProcessName(processId);

            var eventData = new MouseEventData
            {
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                ProcessId = processId,
                ProcessName = processName,
                X = hookStruct.pt.x,
                Y = hookStruct.pt.y,
                Metadata = new Dictionary<string, object>
                {
                    { "ProcessId", processId },
                    { "MouseData", hookStruct.mouseData },
                    { "Flags", hookStruct.flags },
                    { "Time", hookStruct.time }
                }
            };

            EventCaptured?.Invoke(this, eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing mouse event");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (Interlocked.CompareExchange(ref _isListening, 0, 1) == 1)
        {
            try
            {
                StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disposal");
            }
        }

        GC.SuppressFinalize(this);
    }
}
