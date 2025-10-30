using ContextSensor.Core.Attributes;
using ContextSensor.Windows.Events;
using Microsoft.Extensions.Logging;

namespace ContextSensor.Windows.Attributes;

/// <summary>
/// Provider for mouse component attributes (mouse.x, mouse.y, mouse.button, etc.)
/// </summary>
public class MouseAttributeProvider : AttributeProviderBase
{
    private readonly ILogger<MouseAttributeProvider> _logger;

    public MouseAttributeProvider(ILogger<MouseAttributeProvider> logger)
        : base(new[]
        {
            "mouse.x",
            "mouse.y",
        })
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override string ProviderName => "MouseAttributeProvider";

    public override object? GetAttributeValue(string attributeQualifiedName, AttributeContext? context = null)
    {
        return SafeGetValue<object>(
            attributeQualifiedName,
            () =>
            {
                // Extract mouse event data from context if available
                var mouseData = ExtractMouseEventData(context);
                
                if (mouseData == null)
                {
                    _logger.LogDebug("No mouse event data available in context for attribute {AttributeName}", attributeQualifiedName);
                    return null;
                }

                return attributeQualifiedName switch
                {
                    "mouse.x" => mouseData.X,
                    "mouse.y" => mouseData.Y,
                    _ => null
                };
            },
            _logger);
    }

    public override Dictionary<string, object?> GetAttributeValues(
        IEnumerable<string> attributeQualifiedNames,
        AttributeContext? context = null)
    {
        var result = new Dictionary<string, object?>();
        var requestedAttributes = attributeQualifiedNames.ToHashSet();

        // Extract mouse data once for efficiency
        var mouseData = ExtractMouseEventData(context);
        
        if (mouseData == null)
        {
            _logger.LogDebug("No mouse event data available in context");
            return result;
        }

        foreach (var attrName in requestedAttributes)
        {
            if (!CanProvideAttribute(attrName))
                continue;

            result[attrName] = attrName switch
            {
                "mouse.x" => mouseData.X,
                "mouse.y" => mouseData.Y,
                _ => null
            };
        }

        return result;
    }

    private MouseEventData? ExtractMouseEventData(AttributeContext? context)
    {
        if (context?.EventData is MouseEventData mouseData)
        {
            return mouseData;
        }

        return null;
    }
}
