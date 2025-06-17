#!/bin/bash
set -e

# Build libSQL for macOS
echo "Building libSQL for macOS..."

# Check if we're in the right directory
if [ ! -f "libsql/bindings/c/Cargo.toml" ]; then
    echo "Error: libsql source not found. Please run from project root with libsql cloned."
    exit 1
fi

cd libsql/bindings/c

# Build the static library
cargo build --release --target aarch64-apple-darwin

cd ../../target/aarch64-apple-darwin/release

# Extract object files
ar -x libsql_experimental.a

# Create a symbol map to remove underscore prefixes
echo "Creating symbol map..."
nm -gU libsql_experimental.a | grep " T _libsql" | awk '{print $3 " " substr($3, 2)}' > symbol_map.txt

# Create the dynamic library with proper symbol exports
echo "Creating dynamic library..."
clang -dynamiclib -o libsql.dylib *.o \
    -framework Security -framework CoreFoundation \
    -lSystem -lc -lm \
    -Wl,-alias_list,symbol_map.txt

# Verify symbols
echo "Verifying exported symbols..."
nm -gU libsql.dylib | grep " T libsql" | head -10

echo "Build complete!"