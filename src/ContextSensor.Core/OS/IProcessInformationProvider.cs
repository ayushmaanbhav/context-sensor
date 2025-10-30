namespace ContextSensor.Core.OS;

/// <summary>
/// Interface for retrieving process information
/// </summary>
public interface IProcessInformationProvider
{
    /// <summary>
    /// Get process name from process ID
    /// </summary>
    string GetProcessName(int processId);
    
    /// <summary>
    /// Get foreground process ID
    /// </summary>
    int GetForegroundProcessId();
    
    /// <summary>
    /// Check if a process exists
    /// </summary>
    bool ProcessExists(int processId);
}
