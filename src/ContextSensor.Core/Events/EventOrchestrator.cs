using ContextSensor.Core.Configuration;
using ContextSensor.Core.Output;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace ContextSensor.Core.Events;

/// <summary>
/// Orchestrates event listening and processing
/// </summary>
public class EventOrchestrator : IDisposable
{
    private readonly ILogger<EventOrchestrator> _logger;
    private readonly List<IEventListener> _eventListeners;
    private readonly List<IOutputWriter> _outputWriters;
    private readonly IEventProcessor? _eventProcessor;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Channel<IEventData> _eventChannel;
    private Task? _processingTask;
    private int _isRunning; // 0 = not running, 1 = running
    private bool _disposed;

    public EventOrchestrator(
        ILogger<EventOrchestrator> logger,
        IEnumerable<IEventListener> eventListeners,
        IEnumerable<IOutputWriter> outputWriters,
        IEventProcessor? eventProcessor = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventListeners = eventListeners?.ToList() ?? throw new ArgumentNullException(nameof(eventListeners));
        _outputWriters = outputWriters?.ToList() ?? throw new ArgumentNullException(nameof(outputWriters));
        _eventProcessor = eventProcessor;
        _cancellationTokenSource = new CancellationTokenSource();
        _eventChannel = Channel.CreateUnbounded<IEventData>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public async Task StartAsync()
    {
        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
        {
            _logger.LogWarning("EventOrchestrator is already running");
            return;
        }

        try
        {
            _logger.LogInformation("Starting EventOrchestrator with {ListenerCount} listeners and {WriterCount} writers",
                _eventListeners.Count, _outputWriters.Count);

            // Start background processing task
            _processingTask = Task.Run(() => ProcessEventsAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            // Subscribe to all event listeners
            foreach (var listener in _eventListeners)
            {
                listener.EventCaptured += OnEventCaptured;
                await listener.StartAsync(_cancellationTokenSource.Token);
                _logger.LogInformation("Started event listener: {ListenerName}", listener.Name);
            }

            _logger.LogInformation("EventOrchestrator started successfully");
        }
        catch (Exception ex)
        {
            Interlocked.Exchange(ref _isRunning, 0);
            _logger.LogError(ex, "Error starting EventOrchestrator");
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (Interlocked.CompareExchange(ref _isRunning, 0, 1) != 1)
        {
            _logger.LogWarning("EventOrchestrator is not running");
            return;
        }

        try
        {
            _logger.LogInformation("Stopping EventOrchestrator");

            // Signal cancellation
            _cancellationTokenSource.Cancel();

            // Unsubscribe and stop all listeners
            foreach (var listener in _eventListeners)
            {
                listener.EventCaptured -= OnEventCaptured;
                await listener.StopAsync();
                _logger.LogInformation("Stopped event listener: {ListenerName}", listener.Name);
            }

            // Complete the channel to stop processing
            _eventChannel.Writer.Complete();

            // Wait for processing task to complete
            if (_processingTask != null)
            {
                try
                {
                    await _processingTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
            }

            // Flush all output writers
            foreach (var writer in _outputWriters)
            {
                await writer.FlushAsync();
                _logger.LogInformation("Flushed output writer: {WriterName}", writer.Name);
            }

            _logger.LogInformation("EventOrchestrator stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping EventOrchestrator");
            throw;
        }
    }

    private void OnEventCaptured(object? sender, IEventData eventData)
    {
        // Queue event to channel for processing
        if (!_eventChannel.Writer.TryWrite(eventData))
        {
            _logger.LogWarning("Failed to queue event {EventType} - channel may be closed", eventData.EventType);
        }
    }

    private async Task ProcessEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var eventData in _eventChannel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    // Process event if processor is available
                    var processedEvent = eventData;
                    if (_eventProcessor != null)
                    {
                        processedEvent = await _eventProcessor.ProcessAsync(eventData);
                    }

                    // Write to all output writers
                    foreach (var writer in _outputWriters)
                    {
                        await writer.WriteAsync(processedEvent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event: {EventType}", eventData.EventType);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Event processing cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in event processing loop");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (Interlocked.CompareExchange(ref _isRunning, 0, 1) == 1)
        {
            try
            {
                StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disposal");
            }
        }

        _cancellationTokenSource.Dispose();

        foreach (var listener in _eventListeners)
        {
            try
            {
                listener.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing listener");
            }
        }

        foreach (var writer in _outputWriters)
        {
            try
            {
                writer.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing writer");
            }
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
