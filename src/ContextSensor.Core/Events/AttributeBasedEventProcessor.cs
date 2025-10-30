using ContextSensor.Core.Attributes;
using ContextSensor.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace ContextSensor.Core.Events;

/// <summary>
/// Event processor that enriches event data with attributes from providers
/// based on the event schema configuration
/// </summary>
public class AttributeBasedEventProcessor : IEventProcessor
{
    private readonly ILogger<AttributeBasedEventProcessor> _logger;
    private readonly AttributeProviderRegistry _providerRegistry;
    private readonly EventSchema _eventSchema;
    private readonly IEventTypeMapper _eventTypeMapper;

    public AttributeBasedEventProcessor(
        ILogger<AttributeBasedEventProcessor> logger,
        AttributeProviderRegistry providerRegistry,
        EventSchema eventSchema,
        IEventTypeMapper eventTypeMapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providerRegistry = providerRegistry ?? throw new ArgumentNullException(nameof(providerRegistry));
        _eventSchema = eventSchema ?? throw new ArgumentNullException(nameof(eventSchema));;
        _eventTypeMapper = eventTypeMapper ?? throw new ArgumentNullException(nameof(eventTypeMapper));
    }

    public Task<IEventData> ProcessAsync(IEventData eventData)
    {
        try
        {
            // Find the event definition in schema
            var eventDef = FindEventDefinition(eventData);
            
            if (eventDef == null)
            {
                _logger.LogDebug("No event definition found for {EventType}, returning original data", 
                    eventData.EventType);
                return Task.FromResult(eventData);
            }

            // Get the list of attributes to retrieve
            var attributeNames = eventDef.OutputAttributes
                .Select(a => a.Name)
                .ToList();

            if (!attributeNames.Any())
            {
                _logger.LogDebug("No attributes configured for {EventType}", eventData.EventType);
                return Task.FromResult(eventData);
            }

            // Create attribute context
            var context = CreateAttributeContext(eventData);

            // Get all attribute values efficiently in batch
            var attributeValues = _providerRegistry.GetAttributeValues(attributeNames, context);

            // Store attribute values in event metadata
            foreach (var kvp in attributeValues)
            {
                var attrKey = $"attr_{kvp.Key}";
                eventData.Metadata[attrKey] = kvp.Value ?? (object)string.Empty;
            }

            // Also store the event definition for output formatting
            eventData.Metadata["EventDefinition"] = eventDef;

            _logger.LogDebug("Processed event {EventType} with {AttributeCount} attributes",
                eventData.EventType, attributeValues.Count);

            return Task.FromResult(eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event {EventType}", eventData.EventType);
            return Task.FromResult(eventData);
        }
    }

    private EventDefinition? FindEventDefinition(IEventData eventData)
    {
        if (_eventSchema?.Events == null)
            return null;

        // Try to find by event type name
        var eventType = eventData.EventType;
        
        // Map OS event types to schema names using the mapper
        var schemaName = _eventTypeMapper.MapToSchemaName(eventType);
        
        return _eventSchema.Events.FirstOrDefault(e =>
            e.Name.Equals(schemaName, StringComparison.OrdinalIgnoreCase));
    }

    private AttributeContext CreateAttributeContext(IEventData eventData)
    {
        var context = new AttributeContext
        {
            EventData = eventData,
            Timestamp = eventData.Timestamp
        };

        // Copy event metadata to context metadata
        foreach (var kvp in eventData.Metadata)
        {
            context.Metadata[kvp.Key] = kvp.Value;
        }

        // Add common context metadata
        context.Metadata["EventType"] = eventData.EventType;

        return context;
    }
}
