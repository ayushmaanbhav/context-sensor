namespace ContextSensor.Core.Events;

/// <summary>
/// Interface for listening to system events
/// </summary>
public interface IEventListener : IDisposable
{
    /// <summary>
    /// Name of the event listener
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Event fired when an event is captured
    /// </summary>
    event EventHandler<IEventData>? EventCaptured;
    
    /// <summary>
    /// Start listening for events
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stop listening for events
    /// </summary>
    Task StopAsync();
    
    /// <summary>
    /// Whether the listener is currently active
    /// </summary>
    bool IsListening { get; }
}
