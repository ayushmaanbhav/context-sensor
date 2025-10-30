using Microsoft.Extensions.Logging;

namespace ContextSensor.Core.Attributes;

/// <summary>
/// Interface for providers that supply values for specific attributes
/// </summary>
public interface IAttributeProvider
{
    /// <summary>
    /// Gets the name of the provider (used for identification and logging)
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets the list of attribute qualified names that this provider can supply
    /// </summary>
    IEnumerable<string> SupportedAttributes { get; }

    /// <summary>
    /// Checks if this provider can supply a value for the given attribute
    /// </summary>
    /// <param name="attributeQualifiedName">The qualified name of the attribute (e.g., "process.id" or "os_event_type")</param>
    /// <returns>True if the provider can supply the attribute value</returns>
    bool CanProvideAttribute(string attributeQualifiedName);

    /// <summary>
    /// Gets the value for a specific attribute
    /// </summary>
    /// <param name="attributeQualifiedName">The qualified name of the attribute</param>
    /// <param name="context">Optional context information that may be needed to retrieve the value</param>
    /// <returns>The attribute value as an object, or null if not available</returns>
    object? GetAttributeValue(string attributeQualifiedName, AttributeContext? context = null);

    /// <summary>
    /// Gets multiple attribute values at once (more efficient than calling GetAttributeValue multiple times)
    /// </summary>
    /// <param name="attributeQualifiedNames">The qualified names of the attributes to retrieve</param>
    /// <param name="context">Optional context information that may be needed to retrieve the values</param>
    /// <returns>Dictionary mapping attribute names to their values</returns>
    Dictionary<string, object?> GetAttributeValues(IEnumerable<string> attributeQualifiedNames, AttributeContext? context = null);
}

/// <summary>
/// Context information that can be passed to attribute providers
/// </summary>
public class AttributeContext
{
    /// <summary>
    /// The event data associated with the current event (if any)
    /// </summary>
    public object? EventData { get; set; }

    /// <summary>
    /// Timestamp when the attribute value is being requested
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata that may be useful for providers
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Cache for attribute values to avoid redundant lookups within the same context
    /// </summary>
    public Dictionary<string, object?> ValueCache { get; set; } = new();
}

/// <summary>
/// Base class for attribute providers that implements common functionality
/// </summary>
public abstract class AttributeProviderBase : IAttributeProvider
{
    private readonly HashSet<string> _supportedAttributes;

    protected AttributeProviderBase(IEnumerable<string> supportedAttributes)
    {
        _supportedAttributes = new HashSet<string>(supportedAttributes);
    }

    public abstract string ProviderName { get; }

    public IEnumerable<string> SupportedAttributes => _supportedAttributes;

    public virtual bool CanProvideAttribute(string attributeQualifiedName)
    {
        return _supportedAttributes.Contains(attributeQualifiedName);
    }

    public abstract object? GetAttributeValue(string attributeQualifiedName, AttributeContext? context = null);

    public virtual Dictionary<string, object?> GetAttributeValues(
        IEnumerable<string> attributeQualifiedNames, 
        AttributeContext? context = null)
    {
        var result = new Dictionary<string, object?>();
        
        foreach (var attrName in attributeQualifiedNames)
        {
            if (CanProvideAttribute(attrName))
            {
                result[attrName] = GetAttributeValue(attrName, context);
            }
        }

        return result;
    }

    /// <summary>
    /// Helper method to get a cached value or compute it if not cached
    /// </summary>
    protected T GetOrComputeValue<T>(
        AttributeContext? context,
        string cacheKey,
        Func<T> computeFunc)
    {
        if (context?.ValueCache.TryGetValue(cacheKey, out var cachedValue) == true)
        {
            return (T)cachedValue!;
        }

        var value = computeFunc();
        
        if (context != null)
        {
            context.ValueCache[cacheKey] = value;
        }

        return value;
    }

    /// <summary>
    /// Helper method to safely compute attribute values with error handling
    /// </summary>
    /// <typeparam name="T">The type of the value to compute</typeparam>
    /// <param name="attributeName">The name of the attribute for logging</param>
    /// <param name="computeFunc">Function to compute the value</param>
    /// <param name="logger">Optional logger for error logging</param>
    /// <param name="defaultValue">Default value to return on error</param>
    /// <returns>The computed value or default value on error</returns>
    protected T? SafeGetValue<T>(
        string attributeName,
        Func<T?> computeFunc,
        Microsoft.Extensions.Logging.ILogger? logger = null,
        T? defaultValue = default)
    {
        try
        {
            return computeFunc();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting attribute {AttributeName}", attributeName);
            return defaultValue;
        }
    }
}
