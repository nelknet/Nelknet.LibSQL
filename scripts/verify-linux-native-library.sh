#!/usr/bin/env bash

set -euo pipefail

if [ "$#" -ne 2 ]; then
  echo "Usage: $0 <library-path> <ELF-machine>" >&2
  exit 2
fi

library_path="$1"
expected_machine="$2"
bindings_source="src/Nelknet.LibSQL.Bindings/LibSQLNative.cs"

if [ ! -f "${library_path}" ]; then
  echo "The native library does not exist: ${library_path}" >&2
  exit 1
fi

if [ ! -f "${bindings_source}" ]; then
  echo "The managed bindings source does not exist: ${bindings_source}" >&2
  exit 1
fi

for required_command in readelf nm ldd perl; do
  if ! command -v "${required_command}" >/dev/null 2>&1; then
    echo "The required command does not exist: ${required_command}" >&2
    exit 1
  fi
done

actual_machine="$(
  readelf -h "${library_path}" |
    awk -F: '/Machine:/ { value = $2; sub(/^[[:space:]]+/, "", value); print value; exit }'
)"

if [ "${actual_machine}" != "${expected_machine}" ]; then
  echo "The ELF machine is ${actual_machine}. Expected ${expected_machine}." >&2
  exit 1
fi

if ! readelf -h "${library_path}" | grep -Eq 'Type:[[:space:]]+DYN'; then
  echo "The ELF file is not a shared object." >&2
  exit 1
fi

expected_symbols_file="$(mktemp)"
actual_symbols_file="$(mktemp)"
missing_symbols_file="$(mktemp)"
trap 'rm -f "${expected_symbols_file}" "${actual_symbols_file}" "${missing_symbols_file}"' EXIT

perl -0ne '
  while (/\[LibraryImport\(LibraryName(?<arguments>[^]]*)\)\]\s*internal static partial\s+\S+\s+(?<method>\w+)\s*\(/g) {
    my $arguments = $+{arguments};
    my $method = $+{method};
    if ($arguments =~ /EntryPoint\s*=\s*"([^"]+)"/) {
      print "$1\n";
    } else {
      print "$method\n";
    }
  }
' "${bindings_source}" | sort -u > "${expected_symbols_file}"

nm -D --defined-only --format=posix "${library_path}" |
  awk '{ print $1 }' |
  sort -u > "${actual_symbols_file}"

comm -23 "${expected_symbols_file}" "${actual_symbols_file}" > "${missing_symbols_file}"

if [ -s "${missing_symbols_file}" ]; then
  echo "The native library does not export these managed entry points:" >&2
  sed 's/^/  /' "${missing_symbols_file}" >&2
  exit 1
fi

if ldd "${library_path}" | grep -q 'not found'; then
  echo "The native library has unresolved dependencies:" >&2
  ldd "${library_path}" >&2
  exit 1
fi

echo "Verified ${library_path} for ${actual_machine}."
