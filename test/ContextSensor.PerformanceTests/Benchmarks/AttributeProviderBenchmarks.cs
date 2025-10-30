using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ContextSensor.Core.Attributes;
using ContextSensor.Windows.Attributes;
using ContextSensor.Windows.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContextSensor.PerformanceTests.Benchmarks;

/// <summary>
/// Benchmarks for attribute provider performance
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class AttributeProviderBenchmarks
{
    private AttributeProviderRegistry _registry = null!;
    private AttributeContext _context = null!;
    private List<string> _attributeNames = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Initialize registry
        _registry = new AttributeProviderRegistry(NullLogger<AttributeProviderRegistry>.Instance);
        
        // Register providers
        _registry.RegisterProvider(new EventAttributeProvider(NullLogger<EventAttributeProvider>.Instance));
        _registry.RegisterProvider(new MouseAttributeProvider(NullLogger<MouseAttributeProvider>.Instance));

        // Create test event
        var testEvent = new MouseEventData
        {
            EventType = "MouseClick",
            X = 100,
            Y = 200,
            Timestamp = DateTime.UtcNow
        };

        // Create attribute context
        _context = new AttributeContext
        {
            EventData = testEvent,
            Timestamp = testEvent.Timestamp
        };

        // Create attribute list
        _attributeNames = new List<string>
        {
            "event.name",
            "event.timestamp",
            "mouse.x",
            "mouse.y"
        };
    }

    [Benchmark]
    public void GetSingleAttribute()
    {
        var value = _registry.GetAttributeValue("event.name", _context);
    }

    [Benchmark]
    public void GetMultipleAttributes()
    {
        foreach (var name in _attributeNames)
        {
            var value = _registry.GetAttributeValue(name, _context);
        }
    }

    [Benchmark]
    public void CheckAttributeSupport()
    {
        foreach (var name in _attributeNames)
        {
            var supported = _registry.IsAttributeSupported(name);
        }
    }

    [Benchmark]
    public void GetProviderStatistics()
    {
        var stats = _registry.GetStatistics();
    }
}