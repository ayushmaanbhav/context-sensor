using Newtonsoft.Json;

namespace ContextSensor.Core.Configuration;

/// <summary>
/// Root schema containing all event definitions
/// </summary>
public class EventSchema
{
    /// <summary>
    /// List of event definitions
    /// </summary>
    [JsonProperty("events")]
    public List<EventDefinition> Events { get; set; } = new();
}

/// <summary>
/// Defines a specific event to capture
/// </summary>
public class EventDefinition
{
    /// <summary>
    /// Event name/identifier (e.g., "mouse_click_down")
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable description of the event
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// List of attributes to include in output
    /// </summary>
    [JsonProperty("output_attributes")]
    public List<OutputAttribute> OutputAttributes { get; set; } = new();
}

/// <summary>
/// Defines an attribute to include in event output
/// </summary>
public class OutputAttribute
{
    /// <summary>
    /// Name of the attribute in output
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Data type of the attribute (e.g., "string", "integer", "boolean", "double")
    /// </summary>
    [JsonProperty("datatype")]
    public string Datatype { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what this attribute represents
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;
}
