using System.Diagnostics;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace ContextSensor.PerformanceTests.Tests;

/// <summary>
/// Tests for monitoring resource usage (CPU, Memory, Handles, Threads)
/// </summary>
public class ResourceUsageTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly Process _currentProcess;

    public ResourceUsageTests(ITestOutputHelper output)
    {
        _output = output;
        _currentProcess = Process.GetCurrentProcess();
    }

    [Fact]
    public void Memory_Usage_Should_Stay_Within_Limits_During_Event_Processing()
    {
        // Arrange
        var initialMemory = GetMemoryUsageMB();
        _output.WriteLine($"Initial Memory: {initialMemory:F2} MB");

        // Act - Simulate event processing workload
        var iterations = 10000;
        for (int i = 0; i < iterations; i++)
        {
            // Simulate event creation and processing
            var eventData = new
            {
                Timestamp = DateTime.UtcNow,
                Type = "MouseClick",
                X = i % 1920,
                Y = i % 1080
            };

            // Process event (simulate work)
            _ = eventData.ToString();
        }

        // Force garbage collection to get accurate measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GetMemoryUsageMB();
        var memoryIncrease = finalMemory - initialMemory;

        _output.WriteLine($"Final Memory: {finalMemory:F2} MB");
        _output.WriteLine($"Memory Increase: {memoryIncrease:F2} MB");

        // Assert - Memory increase should be less than 50 MB
        memoryIncrease.Should().BeLessThan(50, 
            "memory usage should not increase significantly during event processing");
    }

    [Fact]
    public void Memory_Should_Be_Released_After_Processing()
    {
        // Arrange
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var baselineMemory = GetMemoryUsageMB();
        _output.WriteLine($"Baseline Memory: {baselineMemory:F2} MB");

        // Act - Create and process temporary data
        ProcessTemporaryData();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var afterCleanupMemory = GetMemoryUsageMB();
        var memoryDifference = Math.Abs(afterCleanupMemory - baselineMemory);

        _output.WriteLine($"After Cleanup Memory: {afterCleanupMemory:F2} MB");
        _output.WriteLine($"Memory Difference: {memoryDifference:F2} MB");

        // Assert - Memory should return close to baseline
        memoryDifference.Should().BeLessThan(10,
            "memory should be released after processing temporary data");
    }

    [Fact]
    public void Handle_Count_Should_Not_Leak()
    {
        // Arrange
        var initialHandles = GetHandleCount();
        _output.WriteLine($"Initial Handles: {initialHandles}");

        // Act - Perform operations that might create handles
        for (int i = 0; i < 100; i++)
        {
            using (var tempProcess = Process.GetCurrentProcess())
            {
                _ = tempProcess.Id;
            }
        }

        var finalHandles = GetHandleCount();
        var handleIncrease = finalHandles - initialHandles;

        _output.WriteLine($"Final Handles: {finalHandles}");
        _output.WriteLine($"Handle Increase: {handleIncrease}");

        // Assert - Handle count should not increase significantly
        handleIncrease.Should().BeLessThan(50,
            "handle count should not leak during operations");
    }

    [Fact]
    public void Thread_Count_Should_Remain_Stable()
    {
        // Arrange
        var initialThreads = GetThreadCount();
        _output.WriteLine($"Initial Threads: {initialThreads}");

        // Act - Simulate some async operations
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await Task.Delay(10);
            }));
        }

        Task.WaitAll(tasks.ToArray());
        Thread.Sleep(100); // Allow threads to complete

        var finalThreads = GetThreadCount();
        var threadIncrease = finalThreads - initialThreads;

        _output.WriteLine($"Final Threads: {finalThreads}");
        _output.WriteLine($"Thread Increase: {threadIncrease}");

        // Assert - Thread count should not increase excessively
        threadIncrease.Should().BeLessThan(10,
            "thread count should remain stable after task completion");
    }

    [Fact]
    public void CPU_Usage_Test_Should_Complete_Without_Hanging()
    {
        // Arrange - This test verifies the test process itself doesn't consume excessive resources
        var startTime = DateTime.UtcNow;
        
        // Act - Perform lightweight operations
        for (int i = 0; i < 1000; i++)
        {
            _ = DateTime.UtcNow.ToString();
        }
        
        var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
        
        _output.WriteLine($"Completed 1000 iterations in {duration:F2}ms");

        // Assert - Should complete quickly
        duration.Should().BeLessThan(1000,
            "lightweight operations should complete within 1 second");
    }

    [Fact]
    public void Working_Set_Should_Not_Grow_Unbounded()
    {
        // Arrange
        var measurements = new List<long>();
        const int measurementCount = 10;

        // Act - Measure working set over time with light workload
        for (int i = 0; i < measurementCount; i++)
        {
            // Light workload
            for (int j = 0; j < 1000; j++)
            {
                _ = DateTime.UtcNow.ToString();
            }

            var workingSet = GetWorkingSetMB();
            measurements.Add(workingSet);
            _output.WriteLine($"Measurement {i + 1}: {workingSet} MB working set");
            
            Thread.Sleep(100);
        }

        // Assert - Working set should not grow continuously
        var first = measurements.Take(3).Average();
        var last = measurements.TakeLast(3).Average();
        var growth = last - first;

        _output.WriteLine($"Working Set Growth: {growth:F2} MB");
        
        growth.Should().BeLessThan(30,
            "working set should not grow unbounded over time");
    }

    private static void ProcessTemporaryData()
    {
        var tempData = new List<byte[]>();
        for (int i = 0; i < 100; i++)
        {
            tempData.Add(new byte[1024 * 100]); // 100 KB each
        }
        // Let it go out of scope
    }

    private double GetMemoryUsageMB()
    {
        _currentProcess.Refresh();
        return _currentProcess.WorkingSet64 / (1024.0 * 1024.0);
    }

    private int GetHandleCount()
    {
        _currentProcess.Refresh();
        return _currentProcess.HandleCount;
    }

    private int GetThreadCount()
    {
        _currentProcess.Refresh();
        return _currentProcess.Threads.Count;
    }

    private long GetWorkingSetMB()
    {
        _currentProcess.Refresh();
        return _currentProcess.WorkingSet64 / (1024 * 1024);
    }


    public void Dispose()
    {
        _currentProcess?.Dispose();
    }
}