using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/updateserver.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting ContextSensor Update Server");

    var builder = WebApplication.CreateBuilder(args);
    
    builder.Host.UseSerilog();
    
    // Configure to listen on all interfaces
    builder.WebHost.UseUrls("http://0.0.0.0:8080");

    var app = builder.Build();

    // Enable serving static files from wwwroot
    app.UseStaticFiles();

    // Log all requests
    app.Use(async (context, next) =>
    {
        Log.Information("Request: {Method} {Path} from {IP}", 
            context.Request.Method, 
            context.Request.Path, 
            context.Connection.RemoteIpAddress);
        await next();
    });

    // Endpoint to list available files
    app.MapGet("/", () =>
    {
        var wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var files = Directory.Exists(wwwroot) 
            ? Directory.GetFiles(wwwroot, "*", SearchOption.AllDirectories)
                .Select(f => f.Replace(wwwroot, "").Replace("\\", "/"))
                .ToList()
            : new List<string>();

        return Results.Ok(new
        {
            message = "ContextSensor Update Server",
            availableFiles = files,
            manifestUrl = "http://localhost:8080/manifest.json",
            timestamp = DateTime.UtcNow
        });
    });

    Log.Information("Update Server starting on http://localhost:8080");
    Log.Information("Serving files from: {Path}", Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Update Server terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
