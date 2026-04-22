#!/usr/bin/env bash

set -euo pipefail

readonly version_file="src/Nelknet.LibSQL.Bindings/runtimes/LIBSQL_VERSION"
readonly badge_file=".github/badges/libsql-version.json"

extract_value() {
  local key="$1"
  sed -n "s/^${key}: //p" "$version_file"
}

require_value() {
  local name="$1"
  local value="$2"

  if [[ -z "$value" ]]; then
    echo "Missing '${name}' in ${version_file}" >&2
    exit 1
  fi
}

commit_sha="$(extract_value "libSQL Commit")"
tag_name="$(extract_value "libSQL Tag")"

require_value "libSQL Commit" "$commit_sha"
require_value "libSQL Tag" "$tag_name"

commit_short="${commit_sha:0:7}"

if [[ "$tag_name" != "no-tag" ]]; then
  badge_message="${tag_name}"
else
  badge_message="commit-${commit_short}"
fi

tmp_file="$(mktemp)"
mkdir -p "$(dirname "$badge_file")"

cat > "$tmp_file" <<EOF
{
  "schemaVersion": 1,
  "label": "bundled libSQL",
  "message": "${badge_message}",
  "color": "0f766e"
}
EOF

if [[ "${1:-}" == "--check" ]]; then
  if ! cmp -s "$tmp_file" "$badge_file"; then
    echo "libSQL badge metadata is out of date. Run scripts/generate-libsql-badge.sh." >&2
    rm -f "$tmp_file"
    exit 1
  fi

  rm -f "$tmp_file"
  exit 0
fi

mv "$tmp_file" "$badge_file"
