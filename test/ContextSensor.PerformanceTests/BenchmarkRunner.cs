using BenchmarkDotNet.Running;
using ContextSensor.PerformanceTests.Benchmarks;

namespace ContextSensor.PerformanceTests;

/// <summary>
/// Entry point for running BenchmarkDotNet benchmarks
/// </summary>
public class BenchmarkRunner
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== ContextSensor Performance Benchmarks ===");
        Console.WriteLine();
        Console.WriteLine("Running BenchmarkDotNet benchmarks...");
        Console.WriteLine("This may take several minutes.");
        Console.WriteLine();
        
        // Run all benchmarks
        var switcher = new BenchmarkSwitcher(new[] {
            typeof(EventProcessingBenchmarks),
            typeof(AttributeProviderBenchmarks)
        });
        
        switcher.Run(args);
    }
}