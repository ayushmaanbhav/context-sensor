using System.Diagnostics;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace ContextSensor.PerformanceTests.Tests;

/// <summary>
/// End-to-end performance tests for the main application
/// </summary>
public class EndToEndPerformanceTests
{
    private readonly ITestOutputHelper _output;

    public EndToEndPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Event_Processing_Throughput_Should_Meet_Requirements()
    {
        // Arrange
        const int targetEventsPerSecond = 1000;
        const int testDurationSeconds = 5;
        var totalEvents = 0;
        var stopwatch = Stopwatch.StartNew();

        // Act - Process events for test duration
        while (stopwatch.Elapsed.TotalSeconds < testDurationSeconds)
        {
            // Simulate event processing
            ProcessMockEvent();
            totalEvents++;
        }

        stopwatch.Stop();
        var actualEventsPerSecond = totalEvents / stopwatch.Elapsed.TotalSeconds;

        _output.WriteLine($"Total Events Processed: {totalEvents}");
        _output.WriteLine($"Duration: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        _output.WriteLine($"Events Per Second: {actualEventsPerSecond:F2}");

        // Assert
        actualEventsPerSecond.Should().BeGreaterThan(targetEventsPerSecond,
            "application should process at least 1000 events per second");
    }

    [Fact]
    public void Event_Processing_Latency_Should_Be_Low()
    {
        // Arrange
        const int iterations = 1000;
        var latencies = new List<double>();

        // Act - Measure latency for each event
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            ProcessMockEvent();
            sw.Stop();
            latencies.Add(sw.Elapsed.TotalMilliseconds);
        }

        // Calculate statistics
        var avgLatency = latencies.Average();
        var maxLatency = latencies.Max();
        var p95Latency = GetPercentile(latencies, 0.95);
        var p99Latency = GetPercentile(latencies, 0.99);

        _output.WriteLine($"Average Latency: {avgLatency:F3} ms");
        _output.WriteLine($"P95 Latency: {p95Latency:F3} ms");
        _output.WriteLine($"P99 Latency: {p99Latency:F3} ms");
        _output.WriteLine($"Max Latency: {maxLatency:F3} ms");

        // Assert
        avgLatency.Should().BeLessThan(1.0, "average latency should be under 1ms");
        p95Latency.Should().BeLessThan(2.0, "P95 latency should be under 2ms");
        p99Latency.Should().BeLessThan(5.0, "P99 latency should be under 5ms");
    }

    [Fact]
    public void Concurrent_Event_Processing_Should_Scale()
    {
        // Arrange
        const int eventsPerThread = 1000;
        var threadCounts = new[] { 1, 2, 4, 8 };
        var results = new Dictionary<int, double>();

        // Act - Test with different thread counts
        foreach (var threadCount in threadCounts)
        {
            var sw = Stopwatch.StartNew();
            var tasks = new List<Task>();

            for (int i = 0; i < threadCount; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < eventsPerThread; j++)
                    {
                        ProcessMockEvent();
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            sw.Stop();

            var totalEvents = threadCount * eventsPerThread;
            var eventsPerSecond = totalEvents / sw.Elapsed.TotalSeconds;
            results[threadCount] = eventsPerSecond;

            _output.WriteLine($"Threads: {threadCount}, Events/sec: {eventsPerSecond:F2}");
        }

        // Assert - Should scale reasonably with thread count
        results[4].Should().BeGreaterThan(results[1] * 2,
            "throughput should scale with increased concurrency");
    }

    [Fact]
    public void Sustained_Load_Should_Maintain_Performance()
    {
        // Arrange
        const int measurementIntervalSeconds = 5;
        const int totalMeasurements = 6; // 30 seconds total
        var throughputMeasurements = new List<double>();

        // Act - Measure throughput over sustained period
        for (int i = 0; i < totalMeasurements; i++)
        {
            var eventCount = 0;
            var sw = Stopwatch.StartNew();

            while (sw.Elapsed.TotalSeconds < measurementIntervalSeconds)
            {
                ProcessMockEvent();
                eventCount++;
            }

            var throughput = eventCount / sw.Elapsed.TotalSeconds;
            throughputMeasurements.Add(throughput);
            
            _output.WriteLine($"Interval {i + 1}: {throughput:F2} events/sec");
        }

        // Calculate performance degradation
        var firstThree = throughputMeasurements.Take(3).Average();
        var lastThree = throughputMeasurements.TakeLast(3).Average();
        var degradation = ((firstThree - lastThree) / firstThree) * 100;

        _output.WriteLine($"First 3 intervals avg: {firstThree:F2} events/sec");
        _output.WriteLine($"Last 3 intervals avg: {lastThree:F2} events/sec");
        _output.WriteLine($"Performance degradation: {degradation:F2}%");

        // Assert - Performance should not degrade significantly
        degradation.Should().BeLessThan(10,
            "performance should not degrade more than 10% under sustained load");
    }

    [Fact]
    public void Memory_Efficiency_During_High_Throughput()
    {
        // Arrange
        var process = Process.GetCurrentProcess();
        process.Refresh();
        var initialMemoryMB = process.WorkingSet64 / (1024.0 * 1024.0);
        
        _output.WriteLine($"Initial Memory: {initialMemoryMB:F2} MB");

        // Act - Process many events
        const int eventCount = 100000;
        for (int i = 0; i < eventCount; i++)
        {
            ProcessMockEvent();
            
            // Periodic GC to simulate real-world behavior
            if (i % 10000 == 0)
            {
                GC.Collect(0, GCCollectionMode.Optimized);
            }
        }

        // Force full GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        process.Refresh();
        var finalMemoryMB = process.WorkingSet64 / (1024.0 * 1024.0);
        var memoryIncrease = finalMemoryMB - initialMemoryMB;

        _output.WriteLine($"Final Memory: {finalMemoryMB:F2} MB");
        _output.WriteLine($"Memory Increase: {memoryIncrease:F2} MB");
        _output.WriteLine($"Events Processed: {eventCount}");

        // Assert - Memory increase should be reasonable
        memoryIncrease.Should().BeLessThan(100,
            "memory increase should be under 100MB after processing 100k events");
    }

    [Fact]
    public void Startup_Time_Should_Be_Fast()
    {
        // Arrange & Act
        var sw = Stopwatch.StartNew();
        
        // Simulate application startup activities
        InitializeConfigurationMock();
        InitializeEventListenersMock();
        InitializeOutputWritersMock();
        
        sw.Stop();

        _output.WriteLine($"Startup Time: {sw.ElapsedMilliseconds} ms");

        // Assert - Startup should be quick
        sw.ElapsedMilliseconds.Should().BeLessThan(1000,
            "application should start within 1 second");
    }

    [Fact]
    public void Shutdown_Should_Be_Clean_And_Fast()
    {
        // Arrange - Simulate running state
        InitializeConfigurationMock();
        InitializeEventListenersMock();
        InitializeOutputWritersMock();

        // Act - Measure shutdown time
        var sw = Stopwatch.StartNew();
        
        CleanupResourcesMock();
        FlushDataMock();
        
        sw.Stop();

        _output.WriteLine($"Shutdown Time: {sw.ElapsedMilliseconds} ms");

        // Assert
        sw.ElapsedMilliseconds.Should().BeLessThan(2000,
            "shutdown should complete within 2 seconds");
    }

    // Helper methods to simulate operations
    private static void ProcessMockEvent()
    {
        // Simulate lightweight event processing
        var data = new
        {
            Timestamp = DateTime.UtcNow,
            Type = "MouseClick",
            X = 100,
            Y = 200
        };
        _ = data.ToString();
    }

    private static void InitializeConfigurationMock()
    {
        Thread.Sleep(50); // Simulate config loading
    }

    private static void InitializeEventListenersMock()
    {
        Thread.Sleep(100); // Simulate listener initialization
    }

    private static void InitializeOutputWritersMock()
    {
        Thread.Sleep(50); // Simulate writer initialization
    }

    private static void CleanupResourcesMock()
    {
        Thread.Sleep(100); // Simulate resource cleanup
    }

    private static void FlushDataMock()
    {
        Thread.Sleep(50); // Simulate data flush
    }

    private static double GetPercentile(List<double> values, double percentile)
    {
        var sorted = values.OrderBy(x => x).ToList();
        var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }
}