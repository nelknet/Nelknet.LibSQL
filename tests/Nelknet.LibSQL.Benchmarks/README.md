# Performance regression suite

This project measures critical managed and native paths with BenchmarkDotNet.
Each report contains execution time, the 95th percentile, and managed allocation data.

The suite covers these paths:

- Native BLOB parameters
- Native BLOB reads
- Parameter parse and resolution
- Repeated parameter command execution
- Statement cache reuse

## Run a smoke test

Run this command after a benchmark code change:

```bash
dotnet run \
  --project tests/Nelknet.LibSQL.Benchmarks \
  --configuration Release \
  -- \
  --job Dry \
  --filter '*SqlParameterLayoutBenchmarks.Parse*' \
  --artifacts artifacts/benchmark-smoke
```

The dry job verifies benchmark discovery, compilation, execution, and report export.
Do not use dry job results for performance decisions.

## Record a benchmark report

Close unnecessary applications before each measurement.
Connect the computer to power, and keep its power mode constant.

Run all benchmarks:

```bash
dotnet run \
  --project tests/Nelknet.LibSQL.Benchmarks \
  --configuration Release \
  -- \
  --artifacts artifacts/benchmarks
```

Use a category to select a smaller set:

```bash
dotnet run \
  --project tests/Nelknet.LibSQL.Benchmarks \
  --configuration Release \
  -- \
  --anyCategories Blob \
  --artifacts artifacts/benchmarks-blob
```

BenchmarkDotNet writes full JSON and GitHub Markdown reports below the selected artifact directory.

## Compare two worktrees

Do not compare reports from different computers. Hardware or runtime differences invalidate the comparison.

Create one baseline worktree and one candidate worktree.
Run this command from either repository root:

```bash
./scripts/compare-benchmark-worktrees.sh \
  /path/to/baseline-worktree \
  /path/to/candidate-worktree \
  '*BlobReadBenchmarks*' \
  /path/to/comparison-output
```

The script performs these actions:

1. It runs the selected benchmarks in the baseline worktree.
2. It runs the same benchmarks in the candidate worktree.
3. It verifies that both reports describe the same environment.
4. It prints time and allocation deltas.
5. It fails after a time regression above five percent.
6. It fails after any managed allocation increase.

Use the report command directly when a change has a documented performance tradeoff:

```bash
dotnet run \
  --project tests/Nelknet.LibSQL.Benchmarks \
  --configuration Release \
  --no-build \
  -- \
  compare \
  /path/to/baseline-results \
  /path/to/candidate-results \
  --max-regression 10
```

## Add a benchmark

Measure a public behavior or an internal reusable abstraction.
Keep setup outside the benchmark method.
Reuse destination buffers when allocation is not part of the target behavior.
Add a category that identifies the affected subsystem.
Include the benchmark filter and comparison table in the pull request.
