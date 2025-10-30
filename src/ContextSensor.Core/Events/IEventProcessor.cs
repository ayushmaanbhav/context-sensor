namespace ContextSensor.Core.Events;

/// <summary>
/// Interface for processing captured events
/// </summary>
public interface IEventProcessor
{
    /// <summary>
    /// Process an event and prepare it for output
    /// </summary>
    Task<IEventData> ProcessAsync(IEventData eventData);
}
