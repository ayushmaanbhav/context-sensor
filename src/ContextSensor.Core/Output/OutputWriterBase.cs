using ContextSensor.Core.Configuration;
using ContextSensor.Core.Events;

namespace ContextSensor.Core.Output;

/// <summary>
/// Base class for output writers that provides common formatting functionality
/// </summary>
public abstract class OutputWriterBase : IOutputWriter
{
    protected readonly string _format;

    protected OutputWriterBase(string format = "csv")
    {
        _format = format.ToLowerInvariant();
    }

    public abstract string Name { get; }
    public abstract Task WriteAsync(IEventData eventData);
    public abstract Task FlushAsync();
    public abstract void Dispose();

    /// <summary>
    /// Formats event data according to the configured format
    /// </summary>
    protected string FormatEventData(IEventData eventData)
    {
        return _format switch
        {
            "csv" => FormatAsCsv(eventData),
            "json" => FormatAsJson(eventData),
            _ => eventData.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// Formats event data as CSV
    /// </summary>
    protected virtual string FormatAsCsv(IEventData eventData)
    {
        // Check if we have an EventDefinition with attributes
        if (eventData.Metadata.TryGetValue("EventDefinition", out var eventDefObj)
            && eventDefObj is EventDefinition eventDef
            && eventDef.OutputAttributes?.Any() == true)
        {
            return FormatFromAttributes(eventData, eventDef);
        }

        // Fallback to simple format
        return GetCsvFallback(eventData);
    }

    /// <summary>
    /// Provides fallback CSV format when no attributes are defined
    /// Can be overridden by derived classes
    /// </summary>
    protected virtual string GetCsvFallback(IEventData eventData)
    {
        return eventData.ToString() ?? $"{eventData.EventType} at {eventData.Timestamp}";
    }

    /// <summary>
    /// Formats event data using configured attributes
    /// </summary>
    private string FormatFromAttributes(IEventData eventData, EventDefinition eventDef)
    {
        var parts = new List<string>();

        foreach (var attr in eventDef.OutputAttributes)
        {
            var attrKey = $"attr_{attr.Name}";
            if (eventData.Metadata.TryGetValue(attrKey, out var value))
            {
                parts.Add(FormatValue(value, attr.Datatype));
            }
            else
            {
                parts.Add(string.Empty);
            }
        }

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Formats a single attribute value
    /// </summary>
    private string FormatValue(object? value, string datatype)
    {
        return ValueFormatter.FormatValue(value, datatype);
    }

    /// <summary>
    /// Formats event data as JSON
    /// </summary>
    private string FormatAsJson(IEventData eventData)
    {
        // Check if we have an EventDefinition with attributes
        if (eventData.Metadata.TryGetValue("EventDefinition", out var eventDefObj)
            && eventDefObj is EventDefinition eventDef
            && eventDef.OutputAttributes?.Any() == true)
        {
            // Only include configured attributes
            var output = new Dictionary<string, object?>();
            
            foreach (var attr in eventDef.OutputAttributes)
            {
                var attrKey = $"attr_{attr.Name}";
                if (eventData.Metadata.TryGetValue(attrKey, out var value))
                {
                    output[attr.Name] = value;
                }
            }
            
            return System.Text.Json.JsonSerializer.Serialize(output);
        }
        
        return string.Empty;
    }
}
