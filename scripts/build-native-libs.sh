#!/bin/bash
set -e

# Script to build and organize native libSQL libraries
# This should be run on each platform to build the appropriate native library

echo "Building native libSQL libraries..."

# Detect OS and architecture
OS=$(uname -s | tr '[:upper:]' '[:lower:]')
ARCH=$(uname -m)

# Map to .NET RID format
case "$OS" in
    darwin)
        OS_RID="osx"
        LIB_EXT="dylib"
        ;;
    linux)
        OS_RID="linux"
        LIB_EXT="so"
        ;;
    mingw*|msys*|cygwin*)
        OS_RID="win"
        LIB_EXT="dll"
        ;;
    *)
        echo "Unsupported OS: $OS"
        exit 1
        ;;
esac

case "$ARCH" in
    x86_64|amd64)
        ARCH_RID="x64"
        ;;
    aarch64|arm64)
        ARCH_RID="arm64"
        ;;
    armv7l|arm)
        ARCH_RID="arm"
        ;;
    i686|i386)
        ARCH_RID="x86"
        ;;
    *)
        echo "Unsupported architecture: $ARCH"
        exit 1
        ;;
esac

RID="${OS_RID}-${ARCH_RID}"
echo "Building for RID: $RID"

# Define paths
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
REPO_ROOT="$( cd "$SCRIPT_DIR/.." && pwd )"
NATIVE_LIBS_DIR="$REPO_ROOT/native-libs"
BINDINGS_DIR="$REPO_ROOT/src/Nelknet.LibSQL.Bindings"
RUNTIME_DIR="$BINDINGS_DIR/runtimes/$RID/native"

# Create runtime directory
mkdir -p "$RUNTIME_DIR"

# Function to build libSQL from source
build_libsql() {
    echo "Building libSQL from source..."
    
    # Clone libSQL if not already present
    if [ ! -d "$NATIVE_LIBS_DIR/libsql" ]; then
        echo "Cloning libSQL repository..."
        cd "$NATIVE_LIBS_DIR"
        git clone https://github.com/tursodatabase/libsql.git
    fi
    
    cd "$NATIVE_LIBS_DIR/libsql"
    
    # Update to latest
    git pull
    
    # Build libSQL
    cargo build --release
    
    # Copy the built library
    if [ "$OS_RID" = "win" ]; then
        cp target/release/libsql.dll "$RUNTIME_DIR/"
    elif [ "$OS_RID" = "osx" ]; then
        cp target/release/liblibsql.dylib "$RUNTIME_DIR/libsql.dylib"
    else
        cp target/release/liblibsql.so "$RUNTIME_DIR/libsql.so"
    fi
}

# Function to use existing library (for development/testing)
use_existing_library() {
    echo "Using existing library from native-libs directory..."
    
    if [ "$OS_RID" = "osx" ] && [ -f "$NATIVE_LIBS_DIR/libsql.dylib" ]; then
        echo "Copying existing macOS library..."
        cp "$NATIVE_LIBS_DIR/libsql.dylib" "$RUNTIME_DIR/"
    elif [ "$OS_RID" = "linux" ] && [ -f "$NATIVE_LIBS_DIR/libsql.so" ]; then
        echo "Copying existing Linux library..."
        cp "$NATIVE_LIBS_DIR/libsql.so" "$RUNTIME_DIR/"
    elif [ "$OS_RID" = "win" ] && [ -f "$NATIVE_LIBS_DIR/libsql.dll" ]; then
        echo "Copying existing Windows library..."
        cp "$NATIVE_LIBS_DIR/libsql.dll" "$RUNTIME_DIR/"
    else
        echo "No existing library found for $RID"
        return 1
    fi
}

# Main build logic
if command -v cargo &> /dev/null; then
    echo "Rust toolchain detected. Building from source..."
    build_libsql
else
    echo "Rust toolchain not found. Trying to use existing library..."
    if ! use_existing_library; then
        echo "ERROR: No Rust toolchain and no existing library found."
        echo "Please install Rust (https://rustup.rs/) to build libSQL from source."
        exit 1
    fi
fi

# Update version file
echo "libsql-$(date +%Y%m%d)" > "$BINDINGS_DIR/runtimes/LIBSQL_VERSION"

echo "Native library installed to: $RUNTIME_DIR"
echo ""
echo "To build for other platforms:"
echo "1. Run this script on each target platform"
echo "2. Commit the resulting files in src/Nelknet.LibSQL.Bindings/runtimes/"
echo ""
echo "Current status:"
find "$BINDINGS_DIR/runtimes" -name "libsql.*" -o -name "*.dll" -o -name "*.so" -o -name "*.dylib" | sort