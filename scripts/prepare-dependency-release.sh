#!/usr/bin/env bash

set -euo pipefail

readonly build_props_file="Directory.Build.props"
readonly changelog_file="CHANGELOG.md"

usage() {
  cat <<'EOF' >&2
Usage: scripts/prepare-dependency-release.sh \
  --version-type <patch|minor> \
  --summary-file <path>
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

version_type=""
summary_file=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version-type)
      version_type="${2:-}"
      shift 2
      ;;
    --summary-file)
      summary_file="${2:-}"
      shift 2
      ;;
    *)
      usage
      ;;
  esac
done

require_value "version type" "$version_type"
require_value "summary file" "$summary_file"

if [[ "$version_type" != "patch" && "$version_type" != "minor" ]]; then
  echo "Version type must be patch or minor." >&2
  exit 1
fi

if [[ ! -f "$summary_file" ]]; then
  echo "Summary file does not exist: $summary_file" >&2
  exit 1
fi

current_version="$(sed -n 's/.*<VersionPrefix>\([^<]*\)<\/VersionPrefix>.*/\1/p' "$build_props_file" | tr -d '[:space:]')"
require_value "current package version" "$current_version"

if grep -q "<VersionSuffix>" "$build_props_file"; then
  echo "VersionSuffix is not supported by automated dependency releases." >&2
  exit 1
fi

IFS='.' read -r current_major current_minor current_patch <<< "$current_version"

case "$version_type" in
  patch)
    next_version="${current_major}.${current_minor}.$((current_patch + 1))"
    ;;
  minor)
    next_version="${current_major}.$((current_minor + 1)).0"
    ;;
esac

release_date="$(date -u +%Y-%m-%d)"

perl -0pi -e "s#<VersionPrefix>[^<]+</VersionPrefix>#<VersionPrefix>${next_version}</VersionPrefix>#" "$build_props_file"

unreleased_line="$(grep -n '^## \[Unreleased\]$' "$changelog_file" | head -n 1 | cut -d: -f1)"
first_release_line="$(grep -n '^## \[[0-9]' "$changelog_file" | head -n 1 | cut -d: -f1)"
links_line="$(grep -n '^\[Unreleased\]:' "$changelog_file" | head -n 1 | cut -d: -f1)"

require_value "CHANGELOG unreleased section" "$unreleased_line"
require_value "CHANGELOG first release section" "$first_release_line"
require_value "CHANGELOG links section" "$links_line"

header_file="$(mktemp)"
unreleased_body_file="$(mktemp)"
summary_body_file="$(mktemp)"
modified_release_body_file="$(mktemp)"
released_sections_file="$(mktemp)"
trailing_links_file="$(mktemp)"
new_changelog_file="$(mktemp)"

head -n "$unreleased_line" "$changelog_file" > "$header_file"
sed -n "$((unreleased_line + 1)),$((first_release_line - 1))p" "$changelog_file" > "$unreleased_body_file"
sed -n "${first_release_line},$((links_line - 1))p" "$changelog_file" > "$released_sections_file"
sed -n "$((links_line + 1)),\$p" "$changelog_file" > "$trailing_links_file"

sed 's/^/- /' "$summary_file" > "$summary_body_file"

awk -v summary_file="$summary_body_file" '
  BEGIN { inserted = 0 }
  /^### Changed$/ {
    print
    while ((getline line < summary_file) > 0) {
      print line
    }
    close(summary_file)
    inserted = 1
    next
  }
  { print }
  END {
    if (!inserted) {
      print "### Changed"
      while ((getline line < summary_file) > 0) {
        print line
      }
      close(summary_file)
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
  echo "## [$next_version] - $release_date"
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
  "$summary_body_file" \
  "$modified_release_body_file" \
  "$released_sections_file" \
  "$trailing_links_file"

echo "next_version=${next_version}"
