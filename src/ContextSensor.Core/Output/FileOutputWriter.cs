using ContextSensor.Core.Configuration;
using ContextSensor.Core.Events;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ContextSensor.Core.Output;

/// <summary>
/// Output writer that writes to a file
/// </summary>
public class FileOutputWriter : OutputWriterBase
{
    private readonly ILogger<FileOutputWriter> _logger;
    private readonly string _filePath;
    private readonly StreamWriter _writer;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly int _flushInterval;
    private int _eventCount = 0;
    private bool _disposed;

    public override string Name => "FileOutputWriter";

    public FileOutputWriter(
        ILogger<FileOutputWriter> logger,
        string filePath,
        string format = "csv",
        int flushInterval = 100)
        : base(format)
    {
        _logger = logger;
        _filePath = filePath;
        _flushInterval = flushInterval;

        // Ensure directory exists
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Open file for appending
        _writer = new StreamWriter(filePath, append: true, encoding: Encoding.UTF8)
        {
            AutoFlush = false
        };

        _logger.LogInformation("FileOutputWriter initialized with file: {FilePath}, flush interval: {FlushInterval}",
            filePath, flushInterval);
    }

    public override async Task WriteAsync(IEventData eventData)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FileOutputWriter));

        await _semaphore.WaitAsync();
        try
        {
            string output = FormatEventData(eventData);
            
            await _writer.WriteLineAsync(output);
            _eventCount++;

            // Flush periodically based on flush interval
            if (_eventCount >= _flushInterval)
            {
                await _writer.FlushAsync();
                _eventCount = 0;
                _logger.LogDebug("File buffer flushed after {FlushInterval} events", _flushInterval);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing event to file");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override async Task FlushAsync()
    {
        if (_disposed)
            return;

        await _semaphore.WaitAsync();
        try
        {
            await _writer.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing file output");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    protected override string GetCsvFallback(IEventData eventData)
    {
        // Fallback to simple format with timestamp
        var timestamp = eventData.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
        return $"{timestamp}, {eventData.EventType}";
    }

    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            _semaphore.Wait();
            try
            {
                _logger.LogInformation("Disposing FileOutputWriter, flushing remaining data...");
                _writer?.Flush();
                _writer?.Dispose();
                _logger.LogInformation("FileOutputWriter disposed successfully");
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing FileOutputWriter");
        }
        finally
        {
            _semaphore?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
