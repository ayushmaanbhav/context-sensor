using ContextSensor.Core.Events;

namespace ContextSensor.Core.Output;

/// <summary>
/// Interface for writing event data to various destinations
/// </summary>
public interface IOutputWriter : IDisposable
{
    /// <summary>
    /// Name of the output writer
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Write event data
    /// </summary>
    Task WriteAsync(IEventData eventData);
    
    /// <summary>
    /// Flush any buffered data
    /// </summary>
    Task FlushAsync();
}
