using ContextSensor.Monitor;
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
        Path.Combine(logDir, "monitor.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    Log.Information("Starting ContextSensor Monitor Service");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure configuration sources
    builder.Configuration
        .SetBasePath(AppContext.BaseDirectory)
        // Make config optional so service can start even if MSI hasn't copied it yet
        .AddJsonFile("monitor.config.json", optional: true, reloadOnChange: true);

    // Configure logging
    builder.Services.AddSerilog();

    // Configure services
    builder.Services.Configure<MonitorConfiguration>(
        builder.Configuration);

    // Add Windows Service support
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "ContextSensor.Monitor";
    });

    // Register the monitoring service
    builder.Services.AddHostedService<ProcessMonitorService>();

    var host = builder.Build();
    
    Log.Information("ContextSensor Monitor Service configured successfully");
    
    await host.RunAsync();
    
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "ContextSensor Monitor Service terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
