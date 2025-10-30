namespace ContextSensor.Core.Events;

/// <summary>
/// Interface for mapping OS-specific event types to schema event names.
/// Different operating systems may have different event type identifiers
/// that need to be mapped to common schema names.
/// </summary>
public interface IEventTypeMapper
{
    /// <summary>
    /// Maps an OS-specific event type to a schema event name
    /// </summary>
    /// <param name="osEventType">The OS-specific event type identifier</param>
    /// <returns>The mapped schema event name</returns>
    string MapToSchemaName(string osEventType);
}
