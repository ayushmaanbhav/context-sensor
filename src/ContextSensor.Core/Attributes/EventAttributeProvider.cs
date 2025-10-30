using ContextSensor.Core.Events;
using Microsoft.Extensions.Logging;

namespace ContextSensor.Core.Attributes;

/// <summary>
/// Provider for event-related global attributes (os_event_type, etc.)
/// </summary>
public class EventAttributeProvider : AttributeProviderBase
{
    private readonly ILogger<EventAttributeProvider> _logger;

    public EventAttributeProvider(ILogger<EventAttributeProvider> logger)
        : base(new[]
        {
            "os_event_type",
            "timestamp"
        })
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override string ProviderName => "EventAttributeProvider";

    public override object? GetAttributeValue(string attributeQualifiedName, AttributeContext? context = null)
    {
        return SafeGetValue<object>(
            attributeQualifiedName,
            () => attributeQualifiedName switch
            {
                "os_event_type" => GetOsEventType(context),
                "timestamp" => GetTimestamp(context),
                _ => null
            },
            _logger);
    }

    private string? GetOsEventType(AttributeContext? context)
    {
        // Try to get event type from metadata
        if (context?.Metadata.TryGetValue("EventType", out var eventTypeObj) == true)
        {
            return eventTypeObj?.ToString();
        }

        // Try to get from event data if it has an EventType property
        if (context?.EventData != null)
        {
            var eventDataType = context.EventData.GetType();
            var eventTypeProp = eventDataType.GetProperty("EventType");
            
            if (eventTypeProp != null)
            {
                return eventTypeProp.GetValue(context.EventData)?.ToString();
            }
        }

        return null;
    }

    private DateTime? GetTimestamp(AttributeContext? context)
    {
        // Get timestamp from event data
        if (context?.EventData is IEventData eventData)
        {
            return eventData.Timestamp;
        }

        // Fallback to context timestamp
        return context?.Timestamp;
    }
}
