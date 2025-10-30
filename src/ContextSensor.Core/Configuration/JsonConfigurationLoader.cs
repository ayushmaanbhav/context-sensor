using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ContextSensor.Core.Configuration;

/// <summary>
/// JSON-based configuration loader implementation
/// </summary>
public class JsonConfigurationLoader : IConfigurationLoader
{
    private readonly ILogger<JsonConfigurationLoader> _logger;

    public JsonConfigurationLoader(ILogger<JsonConfigurationLoader> logger)
    {
        _logger = logger;
    }

    public SensorConfiguration LoadConfiguration(string configFilePath)
    {
        try
        {
            _logger.LogInformation("Loading configuration from {ConfigFilePath}", configFilePath);
            
            if (!File.Exists(configFilePath))
            {
                _logger.LogError("Configuration file not found at {ConfigFilePath}", configFilePath);
                throw new FileNotFoundException($"Configuration file not found: {configFilePath}");
            }

            var json = File.ReadAllText(configFilePath);
            var configuration = JsonConvert.DeserializeObject<SensorConfiguration>(json);

            if (configuration == null)
            {
                _logger.LogError("Failed to deserialize configuration from {ConfigFilePath}", configFilePath);
                throw new InvalidOperationException($"Failed to load configuration from {configFilePath}");
            }

            // Load event schema from events.json
            var eventsFilePath = Path.Combine(Path.GetDirectoryName(configFilePath) ?? "", "events.json");
            configuration.EventSchema = LoadEventSchema(eventsFilePath);

            if (!ValidateConfiguration(configuration, out var errors))
            {
                _logger.LogError("Configuration validation failed: {Errors}", string.Join(", ", errors));
                throw new InvalidOperationException($"Invalid configuration: {string.Join(", ", errors)}");
            }

            _logger.LogInformation("Configuration loaded successfully");
            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration from {ConfigFilePath}", configFilePath);
            throw;
        }
    }

    public bool ValidateConfiguration(SensorConfiguration configuration, out List<string> errors)
    {
        errors = new List<string>();

        if (configuration == null)
        {
            errors.Add("Configuration is null");
            return false;
        }

        if (configuration.EventListeners == null || !configuration.EventListeners.Any())
        {
            errors.Add("No event listeners configured");
        }

        if (configuration.OutputWriters == null || !configuration.OutputWriters.Any())
        {
            errors.Add("No output writers configured");
        }

        foreach (var listener in configuration.EventListeners ?? new List<EventListenerConfig>())
        {
            if (string.IsNullOrWhiteSpace(listener.Type))
            {
                errors.Add("Event listener has no type specified");
            }
        }

        foreach (var writer in configuration.OutputWriters ?? new List<OutputWriterConfig>())
        {
            if (string.IsNullOrWhiteSpace(writer.Type))
            {
                errors.Add("Output writer has no type specified");
            }
        }

        return !errors.Any();
    }

    /// <summary>
    /// Loads event schema from events.json file
    /// </summary>
    private EventSchema LoadEventSchema(string eventsFilePath)
    {
        try
        {
            _logger.LogInformation("Loading event schema from {EventsFilePath}", eventsFilePath);
            
            if (!File.Exists(eventsFilePath))
            {
                _logger.LogError("Events file not found at {EventsFilePath}", eventsFilePath);
                throw new FileNotFoundException($"Events schema file not found: {eventsFilePath}");
            }

            var json = File.ReadAllText(eventsFilePath);
            var schema = JsonConvert.DeserializeObject<EventSchema>(json);

            if (schema == null)
            {
                _logger.LogError("Failed to deserialize event schema from {EventsFilePath}", eventsFilePath);
                throw new InvalidOperationException($"Failed to load event schema from {eventsFilePath}");
            }

            _logger.LogInformation("Event schema loaded with {EventCount} event definitions", schema.Events?.Count ?? 0);
            return schema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading event schema from {EventsFilePath}", eventsFilePath);
            throw;
        }
    }
}
