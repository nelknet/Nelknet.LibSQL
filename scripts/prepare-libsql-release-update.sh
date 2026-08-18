#!/usr/bin/env bash

set -euo pipefail

readonly build_info_file="src/Nelknet.LibSQL.Bindings/runtimes/LIBSQL_BUILD_INFO"
readonly build_props_file="Directory.Build.props"
readonly changelog_file="CHANGELOG.md"

bash scripts/libsql-build-info.sh verify

extract_version_value() {
  local key="$1"
  sed -n "s/^${key}: //p" "$build_info_file"
}

usage() {
  cat <<'EOF' >&2
Usage: scripts/prepare-libsql-release-update.sh \
  --previous-libsql-tag <tag-or-no-tag> \
  --previous-libsql-commit <full-commit-sha>
EOF
  exit 1
}

require_value() {
  local name="$1"
  local value="$2"

  if [[ -z "$value" ]]; then
    echo "Missing required value for ${name}" >&2
    exit 1
  fi
}

previous_tag=""
previous_commit=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --previous-libsql-tag)
      previous_tag="${2:-}"
      shift 2
      ;;
    --previous-libsql-commit)
      previous_commit="${2:-}"
      shift 2
      ;;
    *)
      usage
      ;;
  esac
done

require_value "previous libSQL tag" "$previous_tag"
require_value "previous libSQL commit" "$previous_commit"

current_version="$(sed -n 's/.*<VersionPrefix>\([^<]*\)<\/VersionPrefix>.*/\1/p' "$build_props_file" | tr -d '[:space:]')"
require_value "current package version" "$current_version"

if grep -q "<VersionSuffix>" "$build_props_file"; then
  echo "VersionSuffix is not supported by automated libSQL release updates." >&2
  exit 1
fi

IFS='.' read -r current_major current_minor current_patch <<< "$current_version"
next_version="${current_major}.${current_minor}.$((current_patch + 1))"

new_tag="$(extract_version_value "libSQL Tag")"
new_commit="$(extract_version_value "libSQL Commit")"

require_value "new libSQL tag" "$new_tag"
require_value "new libSQL commit" "$new_commit"

if [[ "$new_tag" != "no-tag" ]]; then
  new_identity="upstream release tag \`${new_tag}\`"
else
  new_identity="untagged upstream commit \`${new_commit:0:7}\`"
fi

if [[ "$previous_tag" != "no-tag" ]]; then
  previous_identity="upstream release tag \`${previous_tag}\`"
else
  previous_identity="untagged upstream commit \`${previous_commit:0:7}\`"
fi

changelog_bullet="- Switch bundled libSQL native libraries from ${previous_identity} to ${new_identity}"

perl -0pi -e "s#<VersionPrefix>[^<]+</VersionPrefix>#<VersionPrefix>${next_version}</VersionPrefix>#" "$build_props_file"

unreleased_line="$(grep -n '^## \[Unreleased\]$' "$changelog_file" | head -n 1 | cut -d: -f1)"
first_release_line="$(grep -n '^## \[[0-9]' "$changelog_file" | head -n 1 | cut -d: -f1)"
links_line="$(grep -n '^\[Unreleased\]:' "$changelog_file" | head -n 1 | cut -d: -f1)"

require_value "CHANGELOG unreleased section" "$unreleased_line"
require_value "CHANGELOG first release section" "$first_release_line"
require_value "CHANGELOG links section" "$links_line"

header_file="$(mktemp)"
unreleased_body_file="$(mktemp)"
modified_release_body_file="$(mktemp)"
released_sections_file="$(mktemp)"
trailing_links_file="$(mktemp)"
new_changelog_file="$(mktemp)"

head -n "$unreleased_line" "$changelog_file" > "$header_file"
sed -n "$((unreleased_line + 1)),$((first_release_line - 1))p" "$changelog_file" > "$unreleased_body_file"
sed -n "${first_release_line},$((links_line - 1))p" "$changelog_file" > "$released_sections_file"
sed -n "$((links_line + 1)),\$p" "$changelog_file" > "$trailing_links_file"

awk -v bullet="$changelog_bullet" '
  BEGIN { inserted = 0 }
  /^### Changed$/ {
    print
    print bullet
    inserted = 1
    next
  }
  { print }
  END {
    if (!inserted) {
      print "### Changed"
      print bullet
      print ""
    }
  }
' "$unreleased_body_file" > "$modified_release_body_file"

{
  cat "$header_file"
  echo
  echo "### Added"
  echo
  echo "### Changed"
  echo
  echo "### Deprecated"
  echo
  echo "### Removed"
  echo
  echo "### Fixed"
  echo
  echo "### Security"
  echo
  echo "## [$next_version]"
  cat "$modified_release_body_file"
  cat "$released_sections_file"
  echo
  echo "[Unreleased]: https://github.com/nelknet/Nelknet.LibSQL/compare/v${next_version}...HEAD"
  echo "[$next_version]: https://github.com/nelknet/Nelknet.LibSQL/compare/v${current_version}...v${next_version}"
  cat "$trailing_links_file"
} > "$new_changelog_file"

mv "$new_changelog_file" "$changelog_file"

rm -f \
  "$header_file" \
  "$unreleased_body_file" \
  "$modified_release_body_file" \
  "$released_sections_file" \
  "$trailing_links_file"

echo "next_version=${next_version}"
