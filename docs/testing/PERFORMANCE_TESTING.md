# ContextSensor Performance Testing Guide

This guide covers performance testing procedures, benchmarks, and resource usage testing for the ContextSensor application.

## Table of Contents

1. [Overview](#overview)
2. [Test Projects](#test-projects)
3. [Performance Benchmarks](#performance-benchmarks)
4. [Resource Usage Tests](#resource-usage-tests)
5. [Running Tests](#running-tests)
6. [Performance Targets](#performance-targets)
7. [Analyzing Results](#analyzing-results)
8. [Troubleshooting](#troubleshooting)

## Overview

Performance testing ensures that ContextSensor:
- Processes events efficiently with minimal latency
- Maintains low CPU usage during idle and active states
- Keeps memory usage within acceptable limits
- Does not leak resources (handles, threads, memory)
- Scales well under concurrent load
- Starts up and shuts down quickly

## Test Projects

### ContextSensor.PerformanceTests

Location: [`test/ContextSensor.PerformanceTests/`](../../test/ContextSensor.PerformanceTests/)

**Purpose**: Comprehensive performance and resource usage testing

**Dependencies**:
- BenchmarkDotNet - For micro-benchmarking
- xUnit - For unit test framework
- FluentAssertions - For readable assertions
- System.Diagnostics.PerformanceCounter - For system metrics

**Structure**:
```
test/ContextSensor.PerformanceTests/
├── Benchmarks/
│   ├── EventProcessingBenchmarks.cs
│   └── AttributeProviderBenchmarks.cs
├── Tests/
│   ├── ResourceUsageTests.cs
│   └── EndToEndPerformanceTests.cs
├── ContextSensor.PerformanceTests.csproj
└── Program.cs
```

## Performance Benchmarks

### Event Processing Benchmarks

Located in [`Benchmarks/EventProcessingBenchmarks.cs`](../../test/ContextSensor.PerformanceTests/Benchmarks/EventProcessingBenchmarks.cs)

Tests event processing performance at different scales:

| Benchmark | Description | Target |
|-----------|-------------|--------|
| ProcessSingleEvent | Single event processing latency | < 1ms |
| Process100Events | Batch of 100 events | < 50ms |
| Process1000Events | Batch of 1000 events | < 500ms |

**Running Event Processing Benchmarks**:
```powershell
cd test/ContextSensor.PerformanceTests
dotnet run -c Release --benchmark
```

**Expected Output**:
```
| Method              | Mean      | Error    | StdDev   | Rank | Allocated |
|-------------------- |----------:|---------:|---------:|-----:|----------:|
| ProcessSingleEvent  |  0.847 ms | 0.010 ms | 0.009 ms |    1 |     1.2 KB|
| Process100Events    | 42.315 ms | 0.456 ms | 0.426 ms |    2 |   120.4 KB|
| Process1000Events   | 425.89 ms | 4.123 ms | 3.856 ms |    3 |  1204.8 KB|
```

### Attribute Provider Benchmarks

Located in [`Benchmarks/AttributeProviderBenchmarks.cs`](../../test/ContextSensor.PerformanceTests/Benchmarks/AttributeProviderBenchmarks.cs)

Tests attribute resolution and provider performance:

| Benchmark | Description | Target |
|-----------|-------------|--------|
| GetSingleAttribute | Single attribute lookup | < 0.1ms |
| GetMultipleAttributes | Multiple attribute lookups | < 0.5ms |
| CheckAttributeSupport | Attribute support check | < 0.05ms |
| GetProviderStatistics | Provider stats retrieval | < 0.1ms |

**Running Attribute Benchmarks**:
```powershell
cd test/ContextSensor.PerformanceTests
dotnet run -c Release --benchmark
```

## Resource Usage Tests

### Memory Usage Tests

Located in [`Tests/ResourceUsageTests.cs`](../../test/ContextSensor.PerformanceTests/Tests/ResourceUsageTests.cs)

**Tests Included**:

#### 1. Memory Usage During Event Processing
```csharp
Memory_Usage_Should_Stay_Within_Limits_During_Event_Processing()
```
- **Purpose**: Verify memory doesn't grow excessively during event processing
- **Threshold**: < 50 MB increase after processing 10,000 events
- **Why**: Prevents memory bloat from event processing

#### 2. Memory Release After Processing
```csharp
Memory_Should_Be_Released_After_Processing()
```
- **Purpose**: Ensure memory is properly released after temporary data processing
- **Threshold**: < 10 MB difference from baseline after GC
- **Why**: Confirms no memory leaks from temporary allocations

#### 3. Handle Count Stability
```csharp
Handle_Count_Should_Not_Leak()
```
- **Purpose**: Verify system handles are properly released
- **Threshold**: < 50 handle increase after 100 operations
- **Why**: Handle leaks can exhaust system resources

#### 4. Thread Count Stability
```csharp
Thread_Count_Should_Remain_Stable()
```
- **Purpose**: Ensure thread pool doesn't grow unbounded
- **Threshold**: < 10 thread increase after async operations
- **Why**: Too many threads cause context switching overhead

#### 5. Idle CPU Usage
```csharp
CPU_Usage_Should_Be_Reasonable_During_Idle()
```
- **Purpose**: Verify low CPU usage when idle
- **Threshold**: < 5% average CPU during idle periods
- **Why**: High idle CPU wastes energy and system resources

#### 6. Working Set Stability
```csharp
Working_Set_Should_Not_Grow_Unbounded()
```
- **Purpose**: Monitor working set over time
- **Threshold**: < 30 MB growth over 10 measurements
- **Why**: Unbounded growth indicates memory leak

### End-to-End Performance Tests

Located in [`Tests/EndToEndPerformanceTests.cs`](../../test/ContextSensor.PerformanceTests/Tests/EndToEndPerformanceTests.cs)

**Tests Included**:

#### 1. Event Processing Throughput
```csharp
Event_Processing_Throughput_Should_Meet_Requirements()
```
- **Target**: > 1,000 events/second
- **Duration**: 5 seconds
- **Why**: Ensures application can handle high event rates

#### 2. Event Processing Latency
```csharp
Event_Processing_Latency_Should_Be_Low()
```
- **Targets**:
  - Average: < 1ms
  - P95: < 2ms
  - P99: < 5ms
- **Why**: Low latency prevents event backlog

#### 3. Concurrent Processing Scalability
```csharp
Concurrent_Event_Processing_Should_Scale()
```
- **Tests**: 1, 2, 4, 8 threads
- **Expected**: Throughput scales with thread count
- **Why**: Verifies concurrent processing efficiency

#### 4. Sustained Load Performance
```csharp
Sustained_Load_Should_Maintain_Performance()
```
- **Duration**: 30 seconds (6 x 5-second intervals)
- **Threshold**: < 10% performance degradation
- **Why**: Detects performance issues under sustained load

#### 5. Memory Efficiency
```csharp
Memory_Efficiency_During_High_Throughput()
```
- **Test**: Process 100,000 events
- **Threshold**: < 100 MB memory increase
- **Why**: Ensures memory efficiency at scale

#### 6. Startup Time
```csharp
Startup_Time_Should_Be_Fast()
```
- **Target**: < 1 second
- **Why**: Fast startup improves user experience

#### 7. Shutdown Time
```csharp
Shutdown_Should_Be_Clean_And_Fast()
```
- **Target**: < 2 seconds
- **Why**: Clean shutdown prevents data loss

## Running Tests

### Quick Test Run

Run all performance tests:
```powershell
cd test/ContextSensor.PerformanceTests
dotnet test -c Release
```

### Run Specific Test Class

```powershell
dotnet test --filter "FullyQualifiedName~ResourceUsageTests"
dotnet test --filter "FullyQualifiedName~EndToEndPerformanceTests"
```

### Run Specific Test

```powershell
dotnet test --filter "Name~Memory_Usage_Should_Stay_Within_Limits"
```

### Run with Detailed Output

```powershell
dotnet test -c Release --logger "console;verbosity=detailed"
```

### Run BenchmarkDotNet Tests

```powershell
cd test/ContextSensor.PerformanceTests
dotnet run -c Release -- --benchmark
```

**BenchmarkDotNet Options**:
```powershell
# Run specific benchmark
dotnet run -c Release -- --benchmark --filter "*EventProcessing*"

# Export results
dotnet run -c Release -- --benchmark --exporters json,html
```

## Performance Targets

### Event Processing

| Metric | Target | Critical Threshold |
|--------|--------|-------------------|
| Single event latency | < 1ms | < 5ms |
| Throughput | > 1,000 events/s | > 500 events/s |
| P95 latency | < 2ms | < 10ms |
| P99 latency | < 5ms | < 20ms |

### Resource Usage

| Resource | Normal Range | Warning Threshold | Critical Threshold |
|----------|--------------|-------------------|-------------------|
| CPU (idle) | 0-2% | 5% | 10% |
| CPU (active) | 5-15% | 25% | 40% |
| Memory | 50-100 MB | 200 MB | 500 MB |
| Handles | 100-500 | 1,000 | 2,000 |
| Threads | 10-20 | 50 | 100 |

### Application Lifecycle

| Metric | Target | Maximum |
|--------|--------|---------|
| Startup time | < 500ms | < 1s |
| Shutdown time | < 1s | < 2s |
| Config reload | < 100ms | < 500ms |

## Analyzing Results

### Understanding Test Output

**xUnit Output**:
```
Test Run Successful.
Total tests: 13
     Passed: 13
 Total time: 45.2345 Seconds

[PASS] Memory_Usage_Should_Stay_Within_Limits_During_Event_Processing
  Initial Memory: 45.23 MB
  Final Memory: 68.45 MB
  Memory Increase: 23.22 MB

[PASS] Event_Processing_Throughput_Should_Meet_Requirements
  Total Events Processed: 15234
  Duration: 5.00 seconds
  Events Per Second: 3046.80
```

### BenchmarkDotNet Output

**Understanding Columns**:
- **Mean**: Average execution time
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation
- **Rank**: Performance ranking (1 = fastest)
- **Allocated**: Memory allocated per operation

**Good Results**:
- Low mean values
- Low standard deviation (consistent performance)
- Minimal allocations

**Problem Indicators**:
- High standard deviation (inconsistent performance)
- Increasing mean over iterations (potential memory leak)
- Excessive allocations (memory pressure)

### Performance Regression Detection

Compare results across versions:

```powershell
# Generate baseline
dotnet run -c Release -- --benchmark --exporters json
mv BenchmarkDotNet.Artifacts/results/*.json baseline-v1.0.0.json

# After changes, compare
dotnet run -c Release -- --benchmark --exporters json
# Compare results manually or use BenchmarkDotNet.Comparing
```

### Memory Profiling

For detailed memory analysis:

```powershell
# Using dotMemory (JetBrains)
dotMemory.exe attach <PID>

# Using Visual Studio Diagnostic Tools
# Debug > Performance Profiler > Memory Usage

# Using PerfView (Microsoft)
PerfView collect -GCOnly ContextSensor.App.exe
```

## Troubleshooting

### Test Failures

#### Memory Tests Failing

**Symptom**: Memory increase exceeds threshold

**Diagnosis**:
```powershell
# Check for memory leaks
dotnet-dump collect -p <PID>
dotnet-dump analyze <dump-file>
> dumpheap -stat
> gcroot <object-address>
```

**Common Causes**:
- Event handlers not unsubscribed
- Static collections growing
- Large object retention
- Timers not disposed

#### Performance Tests Failing

**Symptom**: Throughput below target or latency too high

**Diagnosis**:
1. Check CPU usage during test
2. Review event processing logic
3. Profile with PerfView or dotTrace
4. Check for synchronous I/O operations
5. Review lock contention

**Common Causes**:
- Inefficient algorithms
- Excessive allocations
- Lock contention
- Synchronous I/O in async context
- Large object allocations

#### Resource Leak Tests Failing

**Symptom**: Handle or thread count growing

**Diagnosis**:
```powershell
# Monitor handles
Get-Process ContextSensor.App | Select-Object HandleCount

# Monitor threads
Get-Process ContextSensor.App | Select-Object -ExpandProperty Threads | Measure-Object
```

**Common Causes**:
- File handles not closed
- Network connections not disposed
- Timers not stopped
- Event listeners not removed

### Performance Optimization Tips

1. **Reduce Allocations**:
   - Use object pooling for frequent allocations
   - Prefer `Span<T>` and `Memory<T>` for buffers
   - Reuse collections instead of creating new ones

2. **Optimize Hot Paths**:
   - Profile to identify hot paths
   - Use efficient data structures (Dictionary vs List)
   - Cache expensive computations

3. **Async Best Practices**:
   - Avoid `async void`
   - Don't use `Task.Result` or `Task.Wait()`
   - Use `ConfigureAwait(false)` for library code

4. **Resource Management**:
   - Always dispose IDisposable objects
   - Use `using` statements
   - Implement proper cleanup in async methods

## Continuous Performance Testing

### Integration with CI/CD

Add to your build pipeline:

```yaml
# Azure DevOps example
- task: DotNetCoreCLI@2
  displayName: 'Run Performance Tests'
  inputs:
    command: 'test'
    projects: 'test/ContextSensor.PerformanceTests/*.csproj'
    arguments: '-c Release --logger trx'
    
- task: PublishTestResults@2
  displayName: 'Publish Performance Test Results'
  inputs:
    testResultsFormat: 'VSTest'
    testResultsFiles: '**/*.trx'
```

### Performance Monitoring

Set up alerts for performance regressions:

1. Store baseline metrics
2. Run performance tests on each build
3. Compare against baseline
4. Alert if degradation > 10%

## Best Practices

1. **Run in Release Mode**: Always run performance tests in Release configuration
2. **Warm Up**: Include warm-up iterations before measurements
3. **Isolate Tests**: Run performance tests on dedicated hardware
4. **Multiple Runs**: Run tests multiple times for statistical significance
5. **Document Baselines**: Keep historical performance data
6. **Profile Before Optimizing**: Measure to identify actual bottlenecks
7. **Test Realistic Scenarios**: Use production-like workloads

## See Also

- [Main Testing Guide](GUIDE.md) - Overall testing procedures
- [Updater Testing Guide](UPDATER_TESTING.md) - Update service testing
- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)