# Case Study 01 — Multithreading

OS course assignment. Measures speedup from parallelising `Calculate1()` across N threads over 11M data points, 30 iterations.

## Requirements

- .NET 9 SDK
- `data.bin` and `DLL/CalculatingFunctions.dll` (included)

## Run

```bash
# Default: uses all logical cores
dotnet run -c Release

# Explicit worker count
dotnet run -c Release -- 4
```

> Always use `-c Release`. Debug build is significantly slower for decimal-heavy loops.

## Sweep (scaling curve)

```bash
for n in 1 2 4 8 16; do dotnet run -c Release -- $n; done
```

## Expected output

```
Workers: 4 | Build: Release
Loading data...
Data loaded successfully.

Calculation start ...
Calculation finished in 4857 ms. Result: 4686980924312.00000000
Final result OK.
```

## Benchmark (Apple Silicon arm64)

| Workers | Time (ms) | Speedup | Efficiency |
|---------|-----------|---------|------------|
| 1       | 19144     | 1.00×   | 100%       |
| 2       | 9804      | 1.95×   | 97%        |
| 4       | 4857      | 3.94×   | 98%        |
| 8       | 3707      | 5.17×   | 65%        |
| 16      | 3376      | 5.67×   | 35%        |

Sweet spot: **4 workers** (matches P-core count). Cliff at 8+ due to E-cores being weak on `decimal` arithmetic.

## Files

| File | Purpose |
|------|---------|
| `Program.cs` | Partitioned implementation — submit this |
| `Program.Ref1.cs` | Naive race-condition version — reference only |
| `DLL/CalculatingFunctions.dll` | Black-box compute function |
| `data.bin` | 11,000,001 float32 values (scaled ×36) |
