#!/bin/bash
set -e

# Source cargo environment
source "$HOME/.cargo/env"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}Building native libSQL library for macOS ARM64...${NC}"

# Setup paths
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$( cd "$SCRIPT_DIR/.." && pwd )"
OUTPUT_DIR="$PROJECT_ROOT/src/Nelknet.LibSQL.Bindings/runtimes"
LIBSQL_DIR="$PROJECT_ROOT/temp/libsql"

# Ensure libSQL is cloned
if [ ! -d "$LIBSQL_DIR" ]; then
    echo -e "${YELLOW}Cloning libSQL repository...${NC}"
    mkdir -p "$PROJECT_ROOT/temp"
    git clone --depth 1 https://github.com/tursodatabase/libsql.git "$LIBSQL_DIR"
else
    echo -e "${YELLOW}Using existing libSQL repository...${NC}"
fi

# Build for macOS ARM64
cd "$LIBSQL_DIR/bindings/c"
echo -e "${YELLOW}Building libSQL...${NC}"
cargo build --release --target aarch64-apple-darwin

# Create output directory
mkdir -p "$OUTPUT_DIR/osx-arm64/native"

# Build dynamic library
echo -e "${YELLOW}Creating dynamic library...${NC}"
cd "$LIBSQL_DIR/target/aarch64-apple-darwin/release"

# Clean up any previous attempts
rm -f *.o libsql.dylib

# Extract object files
ar -x libsql_experimental.a

# Create dynamic library
clang -dynamiclib -o libsql.dylib *.o \
    -framework Security -framework CoreFoundation \
    -lSystem -lc -lm

# Copy to output
cp libsql.dylib "$OUTPUT_DIR/osx-arm64/native/"

# Clean up
rm *.o

# Verify the library
echo -e "${GREEN}Library built successfully:${NC}"
ls -lh "$OUTPUT_DIR/osx-arm64/native/libsql.dylib"
file "$OUTPUT_DIR/osx-arm64/native/libsql.dylib"

# Check symbols
echo -e "${YELLOW}Checking for key symbols:${NC}"
nm -gU "$OUTPUT_DIR/osx-arm64/native/libsql.dylib" | grep "T _libsql_open" | head -5

# Create version file
echo "Built locally on $(date)" > "$OUTPUT_DIR/LIBSQL_VERSION"
echo "Platform: macOS ARM64" >> "$OUTPUT_DIR/LIBSQL_VERSION"

echo -e "${GREEN}Build complete!${NC}"
echo -e "${YELLOW}Note: To build for other platforms, use GitHub Actions or set up proper cross-compilation environment.${NC}"