namespace ContextSensor.Core.Attributes;

/// <summary>
/// Defines a component that groups related attributes (e.g., Process, Mouse, Keyboard)
/// </summary>
public class ComponentDefinition
{
    /// <summary>
    /// Unique identifier for the component (e.g., "process", "mouse", "keyboard")
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the component
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this component represents
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// List of attributes that belong to this component
    /// </summary>
    public List<AttributeDefinition> Attributes { get; set; } = new();
}

/// <summary>
/// Defines an attribute with its datatype, description, and metadata
/// </summary>
public class AttributeDefinition
{
    /// <summary>
    /// Unique identifier for the attribute (e.g., "process_id", "mouse_x")
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the attribute
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Data type of the attribute
    /// </summary>
    public AttributeDataType DataType { get; set; }

    /// <summary>
    /// Description of what this attribute represents
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// ID of the component this attribute belongs to (null for global attributes)
    /// </summary>
    public string? ComponentId { get; set; }

    /// <summary>
    /// Full qualified name in format "component.attribute" or just "attribute" for globals
    /// </summary>
    public string QualifiedName => string.IsNullOrEmpty(ComponentId) 
        ? Id 
        : $"{ComponentId}.{Id}";

    /// <summary>
    /// Reference to another attribute (used for derived attributes like foreground_process_id)
    /// </summary>
    public string? ReferencedAttributeId { get; set; }

    /// <summary>
    /// Provider type name that can supply values for this attribute
    /// </summary>
    public string? ProviderType { get; set; }

    /// <summary>
    /// Additional metadata for the attribute
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Supported data types for attributes
/// </summary>
public enum AttributeDataType
{
    String,
    Integer,
    Long,
    Double,
    Boolean,
    DateTime,
    TimeSpan,
    Json
}

/// <summary>
/// Registry of all available components and attributes in the system
/// </summary>
public static class AttributeRegistry
{
    private static readonly Dictionary<string, ComponentDefinition> _components = new();
    private static readonly Dictionary<string, AttributeDefinition> _attributes = new();
    private static readonly object _initLock = new object();
    private static volatile bool _initialized = false;

    /// <summary>
    /// Initialize the registry with standard components and attributes
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        lock (_initLock)
        {
            if (_initialized) return;

            RegisterProcessComponent();
            RegisterMouseComponent();
            RegisterGlobalAttributes();
            RegisterCustomAttributes();

            _initialized = true;
        }
    }

    private static void RegisterProcessComponent()
    {
        var component = new ComponentDefinition
        {
            Id = "process",
            Name = "Process",
            Description = "Attributes related to operating system processes",
            Attributes = new List<AttributeDefinition>
            {
                new AttributeDefinition
                {
                    Id = "id",
                    Name = "Process ID",
                    DataType = AttributeDataType.Integer,
                    Description = "The unique process identifier (PID)",
                    ComponentId = "process",
                    ProviderType = "IProcessInformationProvider"
                },
                new AttributeDefinition
                {
                    Id = "name",
                    Name = "Process Name",
                    DataType = AttributeDataType.String,
                    Description = "The executable name of the process",
                    ComponentId = "process",
                    ProviderType = "IProcessInformationProvider"
                }
            }
        };

        _components[component.Id] = component;
        foreach (var attr in component.Attributes)
        {
            _attributes[attr.QualifiedName] = attr;
        }
    }

    private static void RegisterMouseComponent()
    {
        var component = new ComponentDefinition
        {
            Id = "mouse",
            Name = "Mouse",
            Description = "Attributes related to mouse input events",
            Attributes = new List<AttributeDefinition>
            {
                new AttributeDefinition
                {
                    Id = "x",
                    Name = "Mouse X Position",
                    DataType = AttributeDataType.Integer,
                    Description = "The X coordinate of the mouse cursor",
                    ComponentId = "mouse",
                    ProviderType = "IMouseProvider"
                },
                new AttributeDefinition
                {
                    Id = "y",
                    Name = "Mouse Y Position",
                    DataType = AttributeDataType.Integer,
                    Description = "The Y coordinate of the mouse cursor",
                    ComponentId = "mouse",
                    ProviderType = "IMouseProvider"
                }
            }
        };

        _components[component.Id] = component;
        foreach (var attr in component.Attributes)
        {
            _attributes[attr.QualifiedName] = attr;
        }
    }

    private static void RegisterGlobalAttributes()
    {
        // Global attributes that don't belong to any specific component
        var osEventType = new AttributeDefinition
        {
            Id = "os_event_type",
            Name = "OS Event Type",
            DataType = AttributeDataType.String,
            Description = "The OS-level event type identifier",
            ComponentId = null,
            ProviderType = "IEventProvider"
        };

        _attributes[osEventType.QualifiedName] = osEventType;
    }

    private static void RegisterCustomAttributes()
    {
        // Custom derived attributes that reference component attributes
        var foregroundProcessId = new AttributeDefinition
        {
            Id = "foreground_process_id",
            Name = "Foreground Process ID",
            DataType = AttributeDataType.Integer,
            Description = "Process ID of the foreground (active) application",
            ComponentId = null,
            ReferencedAttributeId = "process.id",
            ProviderType = "IForegroundProcessAttributeProvider"
        };

        var foregroundProcessName = new AttributeDefinition
        {
            Id = "foreground_process_name",
            Name = "Foreground Process Name",
            DataType = AttributeDataType.String,
            Description = "Executable name of the foreground (active) process",
            ComponentId = null,
            ReferencedAttributeId = "process.name",
            ProviderType = "IForegroundProcessAttributeProvider"
        };

        _attributes[foregroundProcessId.QualifiedName] = foregroundProcessId;
        _attributes[foregroundProcessName.QualifiedName] = foregroundProcessName;
    }

    /// <summary>
    /// Get a component by its ID
    /// </summary>
    public static ComponentDefinition? GetComponent(string componentId)
    {
        return _components.TryGetValue(componentId, out var component) ? component : null;
    }

    /// <summary>
    /// Get an attribute by its qualified name (e.g., "process.id" or "os_event_type")
    /// </summary>
    public static AttributeDefinition? GetAttribute(string qualifiedName)
    {
        return _attributes.TryGetValue(qualifiedName, out var attribute) ? attribute : null;
    }

    /// <summary>
    /// Get all registered components
    /// </summary>
    public static IEnumerable<ComponentDefinition> GetAllComponents()
    {
        return _components.Values.ToList();
    }

    /// <summary>
    /// Get all registered attributes
    /// </summary>
    public static IEnumerable<AttributeDefinition> GetAllAttributes()
    {
        return _attributes.Values.ToList();
    }

    /// <summary>
    /// Get all attributes for a specific provider type
    /// </summary>
    public static IEnumerable<AttributeDefinition> GetAttributesByProvider(string providerType)
    {
        return _attributes.Values.Where(a => a.ProviderType == providerType).ToList();
    }
}
