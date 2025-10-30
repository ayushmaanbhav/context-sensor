```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26200.6899)
13th Gen Intel Core i7-1360P, 1 CPU, 16 logical and 12 physical cores
.NET SDK 8.0.415
  [Host]   : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2
  ShortRun : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error    | StdDev   | Rank | Gen0   | Allocated |
|------------------- |---------:|---------:|---------:|-----:|-------:|----------:|
| GetSingleAttribute | 35.99 ns | 12.87 ns | 0.706 ns |    1 | 0.0034 |      32 B |
