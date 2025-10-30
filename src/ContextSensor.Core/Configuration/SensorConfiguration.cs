namespace ContextSensor.Core.Configuration;

/// <summary>
/// Main configuration model for the ContextSensor application
/// </summary>
public class SensorConfiguration
{
    /// <summary>
    /// List of event listeners to enable
    /// </summary>
    public List<EventListenerConfig> EventListeners { get; set; } = new();
    
    /// <summary>
    /// List of output writers to enable
    /// </summary>
    public List<OutputWriterConfig> OutputWriters { get; set; } = new();
    
    /// <summary>
    /// Data storage settings
    /// </summary>
    public StorageConfig Storage { get; set; } = new();
    
    /// <summary>
    /// Logging configuration
    /// </summary>
    public LoggingConfig Logging { get; set; } = new();
    
    /// <summary>
    /// Application-level settings
    /// </summary>
    public AppSettings AppSettings { get; set; } = new();
    
    /// <summary>
    /// Event schema defining which events to capture and what data to output
    /// </summary>
    public EventSchema EventSchema { get; set; } = new();
}

/// <summary>
/// Configuration for an event listener
/// </summary>
public class EventListenerConfig
{
    /// <summary>
    /// Type name of the event listener implementation
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this listener is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Configuration for an output writer
/// </summary>
public class OutputWriterConfig
{
    /// <summary>
    /// Type name of the output writer implementation
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this writer is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Output format (e.g., csv, json, console)
    /// </summary>
    public string Format { get; set; } = "console";
    
    /// <summary>
    /// Output destination (e.g., file path, console)
    /// </summary>
    public string Destination { get; set; } = string.Empty;
    
    /// <summary>
    /// For file writers: number of events before flushing buffer
    /// </summary>
    public int FlushInterval { get; set; } = 100;
}

/// <summary>
/// Storage configuration
/// </summary>
public class StorageConfig
{
    /// <summary>
    /// Base directory for stateful data
    /// </summary>
    public string DataDirectory { get; set; } = "./data";
}

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingConfig
{
    /// <summary>
    /// Log file path
    /// </summary>
    public string LogFilePath { get; set; } = "./logs/contextsensor.log";
    
    /// <summary>
    /// Minimum log level (Debug, Information, Warning, Error)
    /// </summary>
    public string MinimumLevel { get; set; } = "Information";
}

/// <summary>
/// Application settings
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Application name
    /// </summary>
    public string ApplicationName { get; set; } = "ContextSensor";
    
    /// <summary>
    /// Application version
    /// </summary>
    public string Version { get; set; } = "1.0.0";
}
