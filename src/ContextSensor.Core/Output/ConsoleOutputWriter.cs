using ContextSensor.Core.Events;
using Microsoft.Extensions.Logging;

namespace ContextSensor.Core.Output;

/// <summary>
/// Output writer that writes to console/stdout
/// </summary>
public class ConsoleOutputWriter : OutputWriterBase
{
    private readonly ILogger<ConsoleOutputWriter> _logger;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private bool _disposed;

    public override string Name => "ConsoleOutputWriter";

    public ConsoleOutputWriter(ILogger<ConsoleOutputWriter> logger, string format = "csv")
        : base(format)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task WriteAsync(IEventData eventData)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ConsoleOutputWriter));

        await _semaphore.WaitAsync();
        try
        {
            string output = FormatEventData(eventData);
            await Console.Out.WriteLineAsync(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing event to console");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override Task FlushAsync()
    {
        // Console doesn't need explicit flushing
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _semaphore?.Dispose();
        GC.SuppressFinalize(this);
    }
}
