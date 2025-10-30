using ContextSensor.Updater;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

 // Ensure log directory exists before configuring Serilog
var commonData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
var logDir = Path.Combine(commonData, "ContextSensor", "logs");
Directory.CreateDirectory(logDir);

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        Path.Combine(logDir, "updater.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    Log.Information("Starting ContextSensor Updater Service");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure configuration sources
    builder.Configuration
        .SetBasePath(AppContext.BaseDirectory)
        // Make config optional so service can start even if MSI hasn't copied it yet
        .AddJsonFile("updater.config.json", optional: true, reloadOnChange: true);

    // Configure logging
    builder.Services.AddSerilog();

    // Configure services
    builder.Services.Configure<UpdaterConfiguration>(
        builder.Configuration);

    // Add HttpClient
    builder.Services.AddHttpClient();

    // Add Windows Service support
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "ContextSensor.Updater";
    });

    // Register the updater service
    builder.Services.AddHostedService<UpdaterService>();

    var host = builder.Build();
    
    Log.Information("ContextSensor Updater Service configured successfully");
    
    await host.RunAsync();
    
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "ContextSensor Updater Service terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
