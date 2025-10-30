using ContextSensor.Core.Attributes;
using ContextSensor.Core.Configuration;
using ContextSensor.Core.Events;
using ContextSensor.Core.OS;
using ContextSensor.Core.Output;
using ContextSensor.Windows.Attributes;
using ContextSensor.Windows.Events;
using ContextSensor.Windows.OS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ContextSensor.App;

class Program
{
    private static EventOrchestrator? _orchestrator;
    private static IHost? _host;

    [STAThread]
    static async Task<int> Main(string[] args)
    {
        try
        {
            Console.WriteLine("=== ContextSensor Starting ===");
            Console.WriteLine();

            // Configure Serilog - write logs to ProgramData\ContextSensor\logs
            var commonData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var appDataDir = System.IO.Path.Combine(commonData, "ContextSensor");
            var logDir = System.IO.Path.Combine(appDataDir, "logs");
            System.IO.Directory.CreateDirectory(logDir);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(System.IO.Path.Combine(logDir, "contextsensor.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .CreateLogger();

            // Build host with DI
            _host = Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .Build();

            // Start the application
            await RunApplicationAsync();

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Log.Fatal(ex, "Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Register core services
        services.AddSingleton<IConfigurationLoader, JsonConfigurationLoader>();
        services.AddSingleton<IProcessInformationProvider, WindowsProcessInformationProvider>();
        
        // Register attribute system
        services.AddSingleton<AttributeProviderRegistry>();
        
        // Register OS-specific event type mapper
        services.AddSingleton<IEventTypeMapper, WindowsEventTypeMapper>();
        
        // Register logging
        services.AddLogging(builder =>
        {
            builder.AddSerilog();
        });
    }

    private static async Task RunApplicationAsync()
    {
        var logger = _host!.Services.GetRequiredService<ILogger<Program>>();
        var configLoader = _host.Services.GetRequiredService<IConfigurationLoader>();
        var processInfoProvider = _host.Services.GetRequiredService<IProcessInformationProvider>();

        try
        {
            // Load configuration
            logger.LogInformation("Loading configuration from appsettings.json");
            var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var config = configLoader.LoadConfiguration(configPath);

            logger.LogInformation("Configuration loaded successfully");
            logger.LogInformation("Application: {AppName} v{Version}",
                config.AppSettings.ApplicationName,
                config.AppSettings.Version);
            
            // Log event schema information
            logger.LogInformation("Loaded {EventCount} event definitions from events.json",
                config.EventSchema.Events?.Count ?? 0);
            foreach (var evt in config.EventSchema.Events ?? new List<EventDefinition>())
            {
                logger.LogInformation("  - {EventName}: {EventDescription} ({AttributeCount} attributes)",
                    evt.Name, evt.Description, evt.OutputAttributes?.Count ?? 0);
            }

            // Initialize attribute registry
            logger.LogInformation("Initializing attribute registry");
            AttributeRegistry.Initialize();
            
            // Create and configure attribute provider registry
            var providerRegistry = _host.Services.GetRequiredService<AttributeProviderRegistry>();
            RegisterAttributeProviders(providerRegistry, processInfoProvider, logger);
            
            // Create event processor
            var processorLogger = _host.Services.GetRequiredService<ILogger<AttributeBasedEventProcessor>>();
            var eventTypeMapper = _host.Services.GetRequiredService<IEventTypeMapper>();
            var eventProcessor = new AttributeBasedEventProcessor(
                processorLogger,
                providerRegistry,
                config.EventSchema,
                eventTypeMapper);
            
            // Create event listeners based on configuration
            var eventListeners = CreateEventListeners(config, processInfoProvider, logger);
            
            // Create output writers based on configuration
            var outputWriters = CreateOutputWriters(config, logger);

            // Create and start orchestrator
            var orchestratorLogger = _host.Services.GetRequiredService<ILogger<EventOrchestrator>>();
            _orchestrator = new EventOrchestrator(orchestratorLogger, eventListeners, outputWriters, eventProcessor);

            logger.LogInformation("Starting event monitoring...");
            Console.WriteLine("Event monitoring started. Capturing left mouse button clicks...");
            Console.WriteLine("Press Ctrl+C to stop.");
            Console.WriteLine();

            await _orchestrator.StartAsync();

            // Set up cancellation handler
            Console.CancelKeyPress += OnCancelKeyPress;

            // Keep the application running with message pump for Windows hooks
            Application.Run(new HiddenForm());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running application");
            throw;
        }
    }

    private static void RegisterAttributeProviders(
        AttributeProviderRegistry registry,
        IProcessInformationProvider processInfoProvider,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        var loggerFactory = _host!.Services.GetRequiredService<ILoggerFactory>();
        
        try
        {
            // Register all attribute providers
            logger.LogInformation("Registering attribute providers");
            
            registry.RegisterProvider(new EventAttributeProvider(
                loggerFactory.CreateLogger<EventAttributeProvider>()));
            
            registry.RegisterProvider(new ForegroundProcessAttributeProvider(
                processInfoProvider,
                loggerFactory.CreateLogger<ForegroundProcessAttributeProvider>()));
            
            registry.RegisterProvider(new MouseAttributeProvider(
                loggerFactory.CreateLogger<MouseAttributeProvider>()));
            
            var stats = registry.GetStatistics();
            logger.LogInformation("Registered {ProviderCount} attribute providers supporting {AttributeCount} attributes",
                stats.ProviderCount, stats.AttributeCount);
            
            foreach (var providerStat in stats.ProviderDetails)
            {
                logger.LogInformation("  - {ProviderName}: {AttributeCount} attributes",
                    providerStat.ProviderName, providerStat.SupportedAttributeCount);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering attribute providers");
            throw;
        }
    }

    private static List<IEventListener> CreateEventListeners(
        SensorConfiguration config,
        IProcessInformationProvider processInfoProvider,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        var listeners = new List<IEventListener>();
        var loggerFactory = _host!.Services.GetRequiredService<ILoggerFactory>();
        var eventTypeMapper = _host!.Services.GetRequiredService<IEventTypeMapper>();

        foreach (var listenerConfig in config.EventListeners.Where(l => l.Enabled))
        {
            IEventListener? listener = listenerConfig.Type switch
            {
                "MouseEventListener" => new MouseEventListener(
                    loggerFactory.CreateLogger<MouseEventListener>(),
                    processInfoProvider,
                    config.EventSchema,
                    eventTypeMapper),
                _ => null
            };

            if (listener != null)
            {
                listeners.Add(listener);
                logger.LogInformation("Created event listener: {ListenerType}", listenerConfig.Type);
            }
            else
            {
                logger.LogError("Unknown event listener type: {ListenerType}", listenerConfig.Type);
                throw new InvalidOperationException($"Unknown event listener type: {listenerConfig.Type}");
            }
        }

        if (listeners.Count == 0)
        {
            logger.LogError("No event listeners were created");
            throw new InvalidOperationException("No event listeners were created - check configuration");
        }

        return listeners;
    }

    private static List<IOutputWriter> CreateOutputWriters(SensorConfiguration config, Microsoft.Extensions.Logging.ILogger logger)
    {
        var writers = new List<IOutputWriter>();
        var loggerFactory = _host!.Services.GetRequiredService<ILoggerFactory>();

        foreach (var writerConfig in config.OutputWriters.Where(w => w.Enabled))
        {
            IOutputWriter? writer = writerConfig.Type switch
            {
                "ConsoleOutputWriter" => new ConsoleOutputWriter(
                    loggerFactory.CreateLogger<ConsoleOutputWriter>(),
                    writerConfig.Format),
                "FileOutputWriter" => new FileOutputWriter(
                    loggerFactory.CreateLogger<FileOutputWriter>(),
                    writerConfig.Destination,
                    writerConfig.Format,
                    writerConfig.FlushInterval),
                _ => null
            };

            if (writer != null)
            {
                writers.Add(writer);
                logger.LogInformation("Created output writer: {WriterType}", writerConfig.Type);
            }
            else
            {
                logger.LogError("Unknown output writer type: {WriterType}", writerConfig.Type);
                throw new InvalidOperationException($"Unknown output writer type: {writerConfig.Type}");
            }
        }

        if (writers.Count == 0)
        {
            logger.LogError("No output writers were created");
            throw new InvalidOperationException("No output writers were created - check configuration");
        }

        return writers;
    }

    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        Console.WriteLine();
        Console.WriteLine("Shutting down gracefully...");
        
        var logger = _host?.Services.GetRequiredService<ILogger<Program>>();
        logger?.LogInformation("Shutdown requested - initiating graceful shutdown");

        try
        {
            if (_orchestrator != null)
            {
                logger?.LogInformation("Stopping event orchestrator...");
                _orchestrator.StopAsync().GetAwaiter().GetResult();
                
                logger?.LogInformation("Disposing orchestrator resources...");
                _orchestrator.Dispose();
                
                logger?.LogInformation("All resources cleaned up successfully");
            }

            Console.WriteLine("Shutdown complete.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during shutdown");
            Console.Error.WriteLine($"Error during shutdown: {ex.Message}");
        }
        finally
        {
            Application.Exit();
        }
    }

    /// <summary>
    /// Hidden form to provide message pump for Windows hooks
    /// </summary>
    private class HiddenForm : Form
    {
        public HiddenForm()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
        }
    }
}
