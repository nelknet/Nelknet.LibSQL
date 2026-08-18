#!/usr/bin/env bash

set -euo pipefail

readonly build_info_file="src/Nelknet.LibSQL.Bindings/runtimes/LIBSQL_BUILD_INFO"
readonly runtime_entries=(
  "Linux x64 SHA-256|src/Nelknet.LibSQL.Bindings/runtimes/linux-x64/native/libsql.so"
  "Linux ARM64 SHA-256|src/Nelknet.LibSQL.Bindings/runtimes/linux-arm64/native/libsql.so"
  "macOS ARM64 SHA-256|src/Nelknet.LibSQL.Bindings/runtimes/osx-arm64/native/libsql.dylib"
  "Windows x64 SHA-256|src/Nelknet.LibSQL.Bindings/runtimes/win-x64/native/libsql.dll"
)

usage() {
  echo "Usage: $0 write <reference> <commit> <tag> <cargo-version> <rust-compiler> <workflow-run>" >&2
  echo "       $0 verify" >&2
  exit 2
}

hash_file() {
  local file_path="$1"

  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$file_path" | awk '{ print $1 }'
    return
  fi

  if command -v shasum >/dev/null 2>&1; then
    shasum -a 256 "$file_path" | awk '{ print $1 }'
    return
  fi

  echo "A SHA-256 command does not exist." >&2
  exit 1
}

extract_value() {
  local key="$1"
  sed -n "s/^${key}: //p" "$build_info_file"
}

require_single_value() {
  local key="$1"
  local count
  local value

  count="$(grep -c "^${key}: " "$build_info_file" || true)"
  value="$(extract_value "$key")"

  if [ "$count" -ne 1 ] || [ -z "$value" ]; then
    echo "The build information requires one value for ${key}." >&2
    exit 1
  fi
}

write_build_info() {
  if [ "$#" -ne 6 ]; then
    usage
  fi

  local reference="$1"
  local commit_sha="$2"
  local tag_name="$3"
  local cargo_version="$4"
  local rust_compiler="$5"
  local workflow_run="$6"
  local build_date
  local entry
  local key
  local file_path
  local temporary_file

  build_date="$(date -u +"%Y-%m-%d %H:%M:%S UTC")"
  temporary_file="$(mktemp)"
  trap 'rm -f "$temporary_file"' EXIT

  {
    printf 'libSQL Reference: %s\n' "$reference"
    printf 'libSQL Commit: %s\n' "$commit_sha"
    printf 'libSQL Tag: %s\n' "$tag_name"
    printf 'libSQL Cargo Version: %s\n' "$cargo_version"
    printf 'Rust Compiler: %s\n' "$rust_compiler"
    printf 'Build Date: %s\n' "$build_date"
    printf 'Build Workflow Run: %s\n' "$workflow_run"

    for entry in "${runtime_entries[@]}"; do
      key="${entry%%|*}"
      file_path="${entry#*|}"
      printf '%s: %s\n' "$key" "$(hash_file "$file_path")"
    done
  } > "$temporary_file"

  mv "$temporary_file" "$build_info_file"
  trap - EXIT
}

verify_build_info() {
  if [ ! -f "$build_info_file" ]; then
    echo "The build information file does not exist: ${build_info_file}" >&2
    exit 1
  fi

  local expected_keys_file
  local actual_keys_file
  local entry
  local key
  local file_path
  local expected_hash
  local actual_hash
  local commit_sha
  local build_date
  local workflow_run

  expected_keys_file="$(mktemp)"
  actual_keys_file="$(mktemp)"
  trap 'rm -f "$expected_keys_file" "$actual_keys_file"' EXIT

  printf '%s\n' \
    "libSQL Reference" \
    "libSQL Commit" \
    "libSQL Tag" \
    "libSQL Cargo Version" \
    "Rust Compiler" \
    "Build Date" \
    "Build Workflow Run" > "$expected_keys_file"

  for entry in "${runtime_entries[@]}"; do
    printf '%s\n' "${entry%%|*}" >> "$expected_keys_file"
  done

  cut -d: -f1 "$build_info_file" > "$actual_keys_file"

  if ! cmp -s "$expected_keys_file" "$actual_keys_file"; then
    echo "The build information fields or field order are invalid." >&2
    diff -u "$expected_keys_file" "$actual_keys_file" >&2 || true
    exit 1
  fi

  while IFS= read -r key; do
    require_single_value "$key"
  done < "$expected_keys_file"

  commit_sha="$(extract_value "libSQL Commit")"
  build_date="$(extract_value "Build Date")"
  workflow_run="$(extract_value "Build Workflow Run")"

  if [[ ! "$commit_sha" =~ ^[0-9a-f]{40}$ ]]; then
    echo "The libSQL commit is not a full lowercase SHA." >&2
    exit 1
  fi

  if [[ ! "$build_date" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}\ [0-9]{2}:[0-9]{2}:[0-9]{2}\ UTC$ ]]; then
    echo "The build date format is invalid." >&2
    exit 1
  fi

  if [[ ! "$workflow_run" =~ ^https://github\.com/[^/]+/[^/]+/actions/runs/[0-9]+$ ]]; then
    echo "The build workflow URL is invalid." >&2
    exit 1
  fi

  for entry in "${runtime_entries[@]}"; do
    key="${entry%%|*}"
    file_path="${entry#*|}"

    if [ ! -f "$file_path" ]; then
      echo "The native library does not exist: ${file_path}" >&2
      exit 1
    fi

    expected_hash="$(extract_value "$key")"
    actual_hash="$(hash_file "$file_path")"

    if [ "$actual_hash" != "$expected_hash" ]; then
      echo "The native library hash does not match: ${file_path}" >&2
      exit 1
    fi
  done

  trap - EXIT
  rm -f "$expected_keys_file" "$actual_keys_file"
}

case "${1:-}" in
  write)
    shift
    write_build_info "$@"
    ;;
  verify)
    if [ "$#" -ne 1 ]; then
      usage
    fi
    verify_build_info
    ;;
  *)
    usage
    ;;
esac
