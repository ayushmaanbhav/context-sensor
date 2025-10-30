using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ContextSensor.Core.Attributes;

/// <summary>
/// Registry that maps attributes to their providers and manages provider instances
/// </summary>
public class AttributeProviderRegistry
{
    private readonly ConcurrentDictionary<string, IAttributeProvider> _providers = new();
    private readonly ConcurrentDictionary<string, List<string>> _attributeToProviders = new();
    private readonly ILogger<AttributeProviderRegistry> _logger;

    public AttributeProviderRegistry(ILogger<AttributeProviderRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register a provider with the registry
    /// </summary>
    /// <param name="provider">The provider to register</param>
    public void RegisterProvider(IAttributeProvider provider)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        if (string.IsNullOrEmpty(provider.ProviderName))
            throw new ArgumentException("Provider name cannot be null or empty", nameof(provider));

        if (_providers.ContainsKey(provider.ProviderName))
        {
            _logger.LogWarning("Provider {ProviderName} is already registered. Overwriting.", provider.ProviderName);
        }

        _providers[provider.ProviderName] = provider;

        // Map each supported attribute to this provider
        foreach (var attribute in provider.SupportedAttributes)
        {
            var providerList = _attributeToProviders.GetOrAdd(attribute, _ => new List<string>());
            
            lock (providerList)
            {
                if (!providerList.Contains(provider.ProviderName))
                {
                    providerList.Add(provider.ProviderName);
                }
            }
        }

        _logger.LogInformation(
            "Registered provider {ProviderName} supporting {AttributeCount} attributes",
            provider.ProviderName,
            provider.SupportedAttributes.Count());
    }

    /// <summary>
    /// Unregister a provider from the registry
    /// </summary>
    /// <param name="providerName">The name of the provider to unregister</param>
    public void UnregisterProvider(string providerName)
    {
        if (string.IsNullOrEmpty(providerName))
            throw new ArgumentException("Provider name cannot be null or empty", nameof(providerName));

        if (!_providers.TryRemove(providerName, out var provider))
        {
            _logger.LogWarning("Provider {ProviderName} is not registered", providerName);
            return;
        }

        // Remove provider from attribute mappings
        foreach (var attribute in provider.SupportedAttributes)
        {
            if (_attributeToProviders.TryGetValue(attribute, out var providerList))
            {
                lock (providerList)
                {
                    providerList.Remove(providerName);
                    
                    // Clean up empty lists
                    if (providerList.Count == 0)
                    {
                        _attributeToProviders.TryRemove(attribute, out _);
                    }
                }
            }
        }

        _logger.LogInformation("Unregistered provider {ProviderName}", providerName);
    }

    /// <summary>
    /// Get a provider by name
    /// </summary>
    /// <param name="providerName">The name of the provider</param>
    /// <returns>The provider instance, or null if not found</returns>
    public IAttributeProvider? GetProvider(string providerName)
    {
        return _providers.TryGetValue(providerName, out var provider) ? provider : null;
    }

    /// <summary>
    /// Get the first provider that can supply a specific attribute
    /// </summary>
    /// <param name="attributeQualifiedName">The qualified name of the attribute</param>
    /// <returns>The provider instance, or null if no provider is found</returns>
    public IAttributeProvider? GetProviderForAttribute(string attributeQualifiedName)
    {
        if (!_attributeToProviders.TryGetValue(attributeQualifiedName, out var providerNames) || 
            providerNames.Count == 0)
        {
            return null;
        }

        // Return the first provider (you could implement priority logic here)
        var providerName = providerNames[0];
        return _providers.TryGetValue(providerName, out var provider) ? provider : null;
    }

    /// <summary>
    /// Get all providers that can supply a specific attribute
    /// </summary>
    /// <param name="attributeQualifiedName">The qualified name of the attribute</param>
    /// <returns>List of provider instances</returns>
    public IEnumerable<IAttributeProvider> GetProvidersForAttribute(string attributeQualifiedName)
    {
        if (!_attributeToProviders.TryGetValue(attributeQualifiedName, out var providerNames))
        {
            return Enumerable.Empty<IAttributeProvider>();
        }

        List<string> namesCopy;
        lock (providerNames)
        {
            namesCopy = providerNames.ToList();
        }

        return namesCopy
            .Select(name => _providers.TryGetValue(name, out var provider) ? provider : null)
            .Where(p => p != null)
            .Cast<IAttributeProvider>();
    }

    /// <summary>
    /// Get the value for a specific attribute
    /// </summary>
    /// <param name="attributeQualifiedName">The qualified name of the attribute</param>
    /// <param name="context">Optional context information</param>
    /// <returns>The attribute value, or null if not available</returns>
    public object? GetAttributeValue(string attributeQualifiedName, AttributeContext? context = null)
    {
        var provider = GetProviderForAttribute(attributeQualifiedName);
        
        if (provider == null)
        {
            _logger.LogWarning("No provider found for attribute {AttributeName}", attributeQualifiedName);
            return null;
        }

        try
        {
            return provider.GetAttributeValue(attributeQualifiedName, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error getting value for attribute {AttributeName} from provider {ProviderName}",
                attributeQualifiedName, 
                provider.ProviderName);
            return null;
        }
    }

    /// <summary>
    /// Get values for multiple attributes efficiently
    /// </summary>
    /// <param name="attributeQualifiedNames">The qualified names of the attributes</param>
    /// <param name="context">Optional context information</param>
    /// <returns>Dictionary mapping attribute names to their values</returns>
    public Dictionary<string, object?> GetAttributeValues(
        IEnumerable<string> attributeQualifiedNames,
        AttributeContext? context = null)
    {
        var result = new Dictionary<string, object?>();
        
        if (attributeQualifiedNames == null)
            return result;

        // Group attributes by their provider for efficient batch retrieval
        var providerGroups = attributeQualifiedNames
            .Select(attr => new { Attribute = attr, Provider = GetProviderForAttribute(attr) })
            .Where(x => x.Provider != null)
            .GroupBy(x => x.Provider!);

        foreach (var group in providerGroups)
        {
            try
            {
                var provider = group.Key;
                var attributes = group.Select(x => x.Attribute).ToList();
                
                var values = provider.GetAttributeValues(attributes, context);
                
                foreach (var kvp in values)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting batch values from provider {ProviderName}",
                    group.Key.ProviderName);
            }
        }

        return result;
    }

    /// <summary>
    /// Check if an attribute is supported by any registered provider
    /// </summary>
    /// <param name="attributeQualifiedName">The qualified name of the attribute</param>
    /// <returns>True if at least one provider supports the attribute</returns>
    public bool IsAttributeSupported(string attributeQualifiedName)
    {
        return _attributeToProviders.ContainsKey(attributeQualifiedName);
    }

    /// <summary>
    /// Get all registered provider names
    /// </summary>
    public IEnumerable<string> GetAllProviderNames()
    {
        return _providers.Keys;
    }

    /// <summary>
    /// Get all supported attributes
    /// </summary>
    public IEnumerable<string> GetAllSupportedAttributes()
    {
        return _attributeToProviders.Keys;
    }

    /// <summary>
    /// Get statistics about the registry
    /// </summary>
    public RegistryStatistics GetStatistics()
    {
        return new RegistryStatistics
        {
            ProviderCount = _providers.Count,
            AttributeCount = _attributeToProviders.Count,
            ProviderDetails = _providers.Values.Select(p => new ProviderStatistics
            {
                ProviderName = p.ProviderName,
                SupportedAttributeCount = p.SupportedAttributes.Count()
            }).ToList()
        };
    }
}

/// <summary>
/// Statistics about the attribute provider registry
/// </summary>
public class RegistryStatistics
{
    public int ProviderCount { get; set; }
    public int AttributeCount { get; set; }
    public List<ProviderStatistics> ProviderDetails { get; set; } = new();
}

/// <summary>
/// Statistics about a specific provider
/// </summary>
public class ProviderStatistics
{
    public string ProviderName { get; set; } = string.Empty;
    public int SupportedAttributeCount { get; set; }
}
