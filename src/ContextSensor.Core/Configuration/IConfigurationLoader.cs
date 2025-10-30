namespace ContextSensor.Core.Configuration;

/// <summary>
/// Interface for loading application configuration
/// </summary>
public interface IConfigurationLoader
{
    /// <summary>
    /// Load configuration from file
    /// </summary>
    SensorConfiguration LoadConfiguration(string configFilePath);
    
    /// <summary>
    /// Validate configuration
    /// </summary>
    bool ValidateConfiguration(SensorConfiguration configuration, out List<string> errors);
}
