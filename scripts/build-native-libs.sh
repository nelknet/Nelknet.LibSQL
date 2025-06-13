#!/bin/bash

# Script to build libSQL native libraries for all supported platforms
# Based on go-libsql's approach but adapted for .NET

set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$( cd "$SCRIPT_DIR/.." && pwd )"
BUILD_DIR="$PROJECT_ROOT/build-temp"
OUTPUT_DIR="$PROJECT_ROOT/src/Nelknet.LibSQL.Bindings/runtimes"

# libSQL version to build (can be overridden by command line)
LIBSQL_TAG="${1:-libsql-0.6.2}"

echo "=== Building libSQL Native Libraries ==="
echo "libSQL version: $LIBSQL_TAG"
echo "Build directory: $BUILD_DIR"
echo "Output directory: $OUTPUT_DIR"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[BUILD]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# Clean and create directories
print_status "Preparing build environment..."
rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR"
mkdir -p "$OUTPUT_DIR"

# Clone libSQL
print_status "Cloning libSQL repository..."
cd "$BUILD_DIR"
git clone https://github.com/tursodatabase/libsql.git
cd libsql
git checkout "$LIBSQL_TAG"

# Detect current platform
OS=$(uname -s)
ARCH=$(uname -m)

print_status "Detected platform: $OS $ARCH"

# Function to build for current platform
build_native() {
    local target_dir="$1"
    local build_args="$2"
    
    print_status "Building libSQL C bindings..."
    cd "$BUILD_DIR/libsql/bindings/c"
    
    # Build the library
    if [ -n "$build_args" ]; then
        cargo build --release $build_args
    else
        cargo build --release
    fi
}

# Function to copy built libraries
copy_library() {
    local src_path="$1"
    local dst_rid="$2"
    local lib_name="$3"
    
    local dst_dir="$OUTPUT_DIR/$dst_rid/native"
    mkdir -p "$dst_dir"
    
    if [ -f "$src_path" ]; then
        cp "$src_path" "$dst_dir/$lib_name"
        print_status "Copied $lib_name to $dst_rid"
        
        # Also copy header file for reference
        if [ -f "$BUILD_DIR/libsql/bindings/c/include/libsql.h" ]; then
            cp "$BUILD_DIR/libsql/bindings/c/include/libsql.h" "$PROJECT_ROOT/src/Nelknet.LibSQL.Bindings/"
        fi
    else
        print_error "Library not found at $src_path"
        return 1
    fi
}

# Build based on platform
case "$OS" in
    Darwin)
        print_status "Building for macOS..."
        
        # Check if we have required tools
        if ! command -v cargo &> /dev/null; then
            print_error "Rust/Cargo not found. Please install Rust from https://rustup.rs"
            exit 1
        fi
        
        # Build for current architecture
        build_native
        
        # The C bindings produce a static library, but we need a dynamic library
        # We'll need to convert it
        cd "$BUILD_DIR/libsql/bindings/c"
        
        # Create dynamic library from static library
        print_status "Creating dynamic library..."
        
        # Extract object files from static library
        mkdir -p "$BUILD_DIR/obj"
        cd "$BUILD_DIR/obj"
        ar -x "$BUILD_DIR/libsql/target/release/libsql_experimental.a"
        
        # Link into dynamic library
        if [ "$ARCH" = "arm64" ]; then
            # macOS ARM64 (Apple Silicon)
            clang -dynamiclib -o libsql.dylib *.o \
                -framework Security -framework CoreFoundation \
                -lSystem -lc -lm
            copy_library "$BUILD_DIR/obj/libsql.dylib" "osx-arm64" "libsql.dylib"
        else
            # macOS x64
            clang -dynamiclib -o libsql.dylib *.o \
                -framework Security -framework CoreFoundation \
                -lSystem -lc -lm
            copy_library "$BUILD_DIR/obj/libsql.dylib" "osx-x64" "libsql.dylib"
        fi
        ;;
        
    Linux)
        print_status "Building for Linux..."
        
        # Check for cross compilation tools
        if command -v cross &> /dev/null; then
            print_status "Using 'cross' for cross-compilation"
            USE_CROSS=true
        else
            print_warning "'cross' not found. Install it for cross-compilation support:"
            print_warning "  cargo install cross"
            USE_CROSS=false
        fi
        
        # Build for current architecture
        build_native
        
        # Create shared library
        cd "$BUILD_DIR/libsql/bindings/c"
        
        # Extract and link into shared library
        mkdir -p "$BUILD_DIR/obj"
        cd "$BUILD_DIR/obj"
        ar -x "$BUILD_DIR/libsql/target/release/libsql_experimental.a"
        
        if [ "$ARCH" = "x86_64" ]; then
            # Linux x64
            gcc -shared -o libsql.so *.o -lpthread -ldl -lm
            copy_library "$BUILD_DIR/obj/libsql.so" "linux-x64" "libsql.so"
        elif [ "$ARCH" = "aarch64" ]; then
            # Linux ARM64
            gcc -shared -o libsql.so *.o -lpthread -ldl -lm
            copy_library "$BUILD_DIR/obj/libsql.so" "linux-arm64" "libsql.so"
        fi
        
        # If cross is available, build for other architectures
        if [ "$USE_CROSS" = true ]; then
            print_status "Cross-compiling for other Linux architectures..."
            # Add cross-compilation targets as needed
        fi
        ;;
        
    MINGW*|MSYS*|CYGWIN*)
        print_status "Building for Windows..."
        print_error "Windows build requires Visual Studio or MinGW. Please build on Windows directly."
        exit 1
        ;;
        
    *)
        print_error "Unsupported platform: $OS"
        exit 1
        ;;
esac

# Clean up build directory
print_status "Cleaning up..."
cd "$PROJECT_ROOT"
rm -rf "$BUILD_DIR"

print_status "Build complete! Native libraries are in:"
find "$OUTPUT_DIR" -name "*.so" -o -name "*.dylib" -o -name "*.dll" | while read -r file; do
    echo "  - $file"
done

# Create a version file
echo "$LIBSQL_TAG" > "$OUTPUT_DIR/LIBSQL_VERSION"

print_status "Done!"