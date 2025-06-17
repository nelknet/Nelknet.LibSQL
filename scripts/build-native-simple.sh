#!/bin/bash
set -e

export PATH="$HOME/.cargo/bin:$PATH"

echo "Building native libSQL libraries..."

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$( cd "$SCRIPT_DIR/.." && pwd )"
OUTPUT_DIR="$PROJECT_ROOT/src/Nelknet.LibSQL.Bindings/runtimes"
LIBSQL_DIR="$PROJECT_ROOT/temp/libsql"

# Clone or update libSQL
if [ ! -d "$LIBSQL_DIR" ]; then
    echo "Cloning libSQL..."
    mkdir -p "$PROJECT_ROOT/temp"
    git clone --depth 1 https://github.com/tursodatabase/libsql.git "$LIBSQL_DIR"
else
    echo "Updating libSQL..."
    cd "$LIBSQL_DIR" && git pull
fi

cd "$LIBSQL_DIR/bindings/c"

# Build for macOS ARM64 (native)
echo "Building for macOS ARM64..."
cargo build --release
mkdir -p "$OUTPUT_DIR/osx-arm64/native"
cd "$LIBSQL_DIR/target/release"
ar -x libsql_experimental.a
clang -dynamiclib -o libsql.dylib *.o \
    -framework Security -framework CoreFoundation \
    -lSystem -lc -lm
cp libsql.dylib "$OUTPUT_DIR/osx-arm64/native/"
rm *.o
echo "macOS ARM64: $(ls -lh "$OUTPUT_DIR/osx-arm64/native/libsql.dylib" | awk '{print $5}')"

# For cross-compilation, we need additional tools
echo ""
echo "For Linux and Windows cross-compilation, you need:"
echo "  - cargo install cross"
echo "  - brew install mingw-w64 (for Windows)"
echo ""
echo "Would you like to install these tools? (y/n)"
read -r response

if [[ "$response" =~ ^[Yy]$ ]]; then
    echo "Installing cross..."
    cargo install cross
    
    echo "Installing mingw-w64..."
    brew install mingw-w64
    
    # Build for Linux x64
    echo "Building for Linux x64..."
    cd "$LIBSQL_DIR/bindings/c"
    cross build --release --target x86_64-unknown-linux-gnu
    mkdir -p "$OUTPUT_DIR/linux-x64/native"
    
    # Build for Windows x64
    echo "Building for Windows x64..."
    cargo build --release --target x86_64-pc-windows-gnu
    mkdir -p "$OUTPUT_DIR/win-x64/native"
fi

echo "Done! Native libraries built:"
find "$OUTPUT_DIR" -name "libsql.*" -exec ls -lh {} \;