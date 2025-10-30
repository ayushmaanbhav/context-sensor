using ContextSensor.Core.Events;

namespace ContextSensor.Windows.Events;

/// <summary>
/// Mouse event data implementation
/// </summary>
public class MouseEventData : IEventData
{
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// Process ID that received the mouse event
    /// </summary>
    public int ProcessId { get; set; }
    
    /// <summary>
    /// Process name (executable name)
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;
    
    /// <summary>
    /// X coordinate of the mouse click
    /// </summary>
    public int X { get; set; }
    
    /// <summary>
    /// Y coordinate of the mouse click
    /// </summary>
    public int Y { get; set; }

    public override string ToString()
    {
        // Simple default format - actual formatting is done by output writers using attributes
        return $"{EventType} at ({X}, {Y})";
    }
}
