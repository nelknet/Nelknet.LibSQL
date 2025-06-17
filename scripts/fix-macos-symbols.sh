#!/bin/bash
set -e

# This script creates a macOS dynamic library with proper symbol exports
# It removes the underscore prefix that macOS adds to C symbols

if [ "$#" -ne 2 ]; then
    echo "Usage: $0 <input.a> <output.dylib>"
    exit 1
fi

INPUT_LIB="$1"
OUTPUT_LIB="$2"

# Create a temporary directory
TEMP_DIR=$(mktemp -d)
cd "$TEMP_DIR"

# Extract object files
ar -x "$INPUT_LIB"

# Get all libsql symbols and create an exports file
nm -gU "$INPUT_LIB" | grep " T _libsql" | awk '{print substr($3, 2)}' > exports.txt

# Create the dynamic library with explicit exports
clang -dynamiclib -o "$OUTPUT_LIB" *.o \
    -framework Security -framework CoreFoundation \
    -lSystem -lc -lm \
    -exported_symbols_list exports.txt

# Clean up
cd - > /dev/null
rm -rf "$TEMP_DIR"

echo "Created $OUTPUT_LIB with exported symbols:"
nm -gU "$OUTPUT_LIB" | grep " T " | head -10