#!/usr/bin/env bash

set -euo pipefail

if [[ $# -lt 3 || $# -gt 4 ]]; then
  echo "Usage: $0 <baseline-worktree> <candidate-worktree> <filter> [output-directory]" >&2
  exit 2
fi

readonly baseline_worktree="$1"
readonly candidate_worktree="$2"
readonly benchmark_filter="$3"
readonly output_directory="${4:-${candidate_worktree}/artifacts/benchmark-comparison}"
readonly baseline_project="${baseline_worktree}/tests/Nelknet.LibSQL.Benchmarks/Nelknet.LibSQL.Benchmarks.csproj"
readonly candidate_project="${candidate_worktree}/tests/Nelknet.LibSQL.Benchmarks/Nelknet.LibSQL.Benchmarks.csproj"
readonly baseline_output="${output_directory}/baseline"
readonly candidate_output="${output_directory}/candidate"

if [[ ! -f "$baseline_project" ]]; then
  echo "The baseline benchmark project does not exist: $baseline_project" >&2
  exit 2
fi

if [[ ! -f "$candidate_project" ]]; then
  echo "The candidate benchmark project does not exist: $candidate_project" >&2
  exit 2
fi

if [[ -e "$baseline_output" || -e "$candidate_output" ]]; then
  echo "The comparison output already contains a baseline or candidate directory: $output_directory" >&2
  exit 2
fi

mkdir -p "$baseline_output" "$candidate_output"

dotnet run \
  --project "$baseline_project" \
  --configuration Release \
  -- \
  --filter "$benchmark_filter" \
  --artifacts "$baseline_output"

dotnet run \
  --project "$candidate_project" \
  --configuration Release \
  -- \
  --filter "$benchmark_filter" \
  --artifacts "$candidate_output"

dotnet run \
  --project "$candidate_project" \
  --configuration Release \
  --no-build \
  -- \
  compare \
  "$baseline_output" \
  "$candidate_output" \
  --max-regression 5 \
  --fail-on-allocation-increase
