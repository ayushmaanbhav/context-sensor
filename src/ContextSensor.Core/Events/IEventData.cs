namespace ContextSensor.Core.Events;

/// <summary>
/// Base interface for all event data
/// </summary>
public interface IEventData
{
    /// <summary>
    /// Type of the event (e.g., WM_LBUTTONDOWN)
    /// </summary>
    string EventType { get; }
    
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    DateTime Timestamp { get; }
    
    /// <summary>
    /// Additional metadata about the event
    /// </summary>
    Dictionary<string, object> Metadata { get; }
}
