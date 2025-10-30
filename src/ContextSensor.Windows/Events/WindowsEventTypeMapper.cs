using ContextSensor.Core.Events;
using Microsoft.Extensions.Logging;

namespace ContextSensor.Windows.Events;

/// <summary>
/// Windows-specific implementation of event type mapping.
/// Maps Windows message types (like WM_LBUTTONDOWN) to schema event names.
/// </summary>
public class WindowsEventTypeMapper : IEventTypeMapper
{
    private readonly ILogger<WindowsEventTypeMapper> _logger;
    private readonly Dictionary<string, string> _eventTypeMappings;

    public WindowsEventTypeMapper(ILogger<WindowsEventTypeMapper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Define Windows-specific event type mappings
        _eventTypeMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Mouse button events
            { "WM_LBUTTONDOWN", "mouse_click_down" },
            { "WM_RBUTTONDOWN", "mouse_click_down" },
            { "WM_MBUTTONDOWN", "mouse_click_down" },
            
            // Additional Windows message types can be added here as needed
        };
    }

    public string MapToSchemaName(string osEventType)
    {
        if (string.IsNullOrWhiteSpace(osEventType))
        {
            _logger.LogWarning("Received null or empty OS event type");
            return string.Empty;
        }

        // Check if mapping exists
        if (_eventTypeMappings.TryGetValue(osEventType, out var schemaName))
        {
            _logger.LogDebug("Mapped OS event type {OsEventType} to schema name {SchemaName}",
                osEventType, schemaName);
            return schemaName;
        }

        // Default: convert to lowercase
        var defaultName = osEventType.ToLowerInvariant();
        _logger.LogDebug("No mapping found for {OsEventType}, using default: {DefaultName}",
            osEventType, defaultName);
        
        return defaultName;
    }
}
