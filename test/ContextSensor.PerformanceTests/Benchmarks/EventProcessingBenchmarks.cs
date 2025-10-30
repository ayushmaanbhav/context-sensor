using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ContextSensor.Core.Attributes;
using ContextSensor.Core.Configuration;
using ContextSensor.Core.Events;
using ContextSensor.Windows.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContextSensor.PerformanceTests.Benchmarks;

/// <summary>
/// Benchmarks for event processing performance
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class EventProcessingBenchmarks
{
    private AttributeBasedEventProcessor _processor = null!;
    private MouseEventData _testEvent = null!;
    private EventSchema _schema = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Initialize attribute registry
        AttributeRegistry.Initialize();

        // Create test event schema
        _schema = new EventSchema
        {
            Events = new List<EventDefinition>
            {
                new EventDefinition
                {
                    Name = "MouseClick",
                    Description = "Mouse click event",
                    OutputAttributes = new List<OutputAttribute>
                    {
                        new OutputAttribute { Name = "event.name", Datatype = "string" },
                        new OutputAttribute { Name = "event.timestamp", Datatype = "string" },
                        new OutputAttribute { Name = "mouse.x", Datatype = "integer" },
                        new OutputAttribute { Name = "mouse.y", Datatype = "integer" }
                    }
                }
            }
        };

        // Create processor
        var registry = new AttributeProviderRegistry(NullLogger<AttributeProviderRegistry>.Instance);
        registry.RegisterProvider(new EventAttributeProvider(NullLogger<EventAttributeProvider>.Instance));
        
        var mapper = new WindowsEventTypeMapper(NullLogger<WindowsEventTypeMapper>.Instance);
        _processor = new AttributeBasedEventProcessor(
            NullLogger<AttributeBasedEventProcessor>.Instance,
            registry,
            _schema,
            mapper);

        // Create test event
        _testEvent = new MouseEventData
        {
            EventType = "MouseClick",
            X = 100,
            Y = 200,
            Timestamp = DateTime.UtcNow
        };
    }

    [Benchmark]
    public async Task ProcessSingleEvent()
    {
        var result = await _processor.ProcessAsync(_testEvent);
    }

    [Benchmark]
    public async Task Process100Events()
    {
        for (int i = 0; i < 100; i++)
        {
            await _processor.ProcessAsync(_testEvent);
        }
    }

    [Benchmark]
    public async Task Process1000Events()
    {
        for (int i = 0; i < 1000; i++)
        {
            await _processor.ProcessAsync(_testEvent);
        }
    }
}