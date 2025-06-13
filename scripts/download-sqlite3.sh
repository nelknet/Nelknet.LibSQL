#!/bin/bash

# Script to download SQLite3 libraries as a temporary substitute for libSQL
# libSQL is SQLite3-compatible, so this allows us to test our bindings

set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$( cd "$SCRIPT_DIR/.." && pwd )"
LIBS_DIR="$PROJECT_ROOT/native-libs"

# SQLite version to download
SQLITE_VERSION="3460100"  # 3.46.1
SQLITE_YEAR="2024"

echo "Creating native libraries directory..."
mkdir -p "$LIBS_DIR"

# Detect platform
OS=$(uname -s)
ARCH=$(uname -m)

download_sqlite_amalgamation() {
    echo "Downloading SQLite amalgamation source..."
    local url="https://www.sqlite.org/$SQLITE_YEAR/sqlite-amalgamation-$SQLITE_VERSION.zip"
    curl -L -o "$LIBS_DIR/sqlite-amalgamation.zip" "$url"
    unzip -o "$LIBS_DIR/sqlite-amalgamation.zip" -d "$LIBS_DIR"
    rm "$LIBS_DIR/sqlite-amalgamation.zip"
}

build_sqlite() {
    echo "Building SQLite from source..."
    cd "$LIBS_DIR/sqlite-amalgamation-$SQLITE_VERSION"
    
    # Compile with libSQL-compatible flags
    local CFLAGS="-O2 -DSQLITE_ENABLE_COLUMN_METADATA -DSQLITE_ENABLE_FTS3 -DSQLITE_ENABLE_FTS5 -DSQLITE_ENABLE_JSON1 -DSQLITE_ENABLE_RTREE -DSQLITE_THREADSAFE=1"
    
    case "$OS" in
        Darwin)
            echo "Building for macOS..."
            # Build dynamic library
            gcc $CFLAGS -dynamiclib -o libsql.dylib sqlite3.c
            cp libsql.dylib "$LIBS_DIR/"
            
            # Also create sqlite3 variants
            cp libsql.dylib "$LIBS_DIR/libsqlite3.dylib"
            cp libsql.dylib "$LIBS_DIR/sqlite3.dylib"
            ;;
        Linux)
            echo "Building for Linux..."
            # Build shared library
            gcc $CFLAGS -shared -fPIC -o libsql.so sqlite3.c -lpthread -ldl -lm
            cp libsql.so "$LIBS_DIR/"
            
            # Also create sqlite3 variants
            cp libsql.so "$LIBS_DIR/libsqlite3.so"
            cp libsql.so "$LIBS_DIR/sqlite3.so"
            ;;
        MINGW*|MSYS*|CYGWIN*)
            echo "Building for Windows..."
            # Build DLL
            gcc $CFLAGS -shared -o libsql.dll sqlite3.c -Wl,--out-implib,libsql.lib
            cp libsql.dll "$LIBS_DIR/"
            cp libsql.lib "$LIBS_DIR/"
            
            # Also create sqlite3 variants
            cp libsql.dll "$LIBS_DIR/sqlite3.dll"
            ;;
        *)
            echo "Unsupported platform: $OS"
            exit 1
            ;;
    esac
    
    # Copy header file
    cp sqlite3.h "$LIBS_DIR/"
    
    cd "$PROJECT_ROOT"
}

copy_to_test_directory() {
    echo "Copying libraries to test directory..."
    local TEST_BIN_DIR="$PROJECT_ROOT/tests/Nelknet.LibSQL.Tests/bin/Debug/net8.0"
    mkdir -p "$TEST_BIN_DIR"
    
    case "$OS" in
        Darwin)
            cp "$LIBS_DIR"/*.dylib "$TEST_BIN_DIR/" 2>/dev/null || true
            ;;
        Linux)
            cp "$LIBS_DIR"/*.so "$TEST_BIN_DIR/" 2>/dev/null || true
            ;;
        MINGW*|MSYS*|CYGWIN*)
            cp "$LIBS_DIR"/*.dll "$TEST_BIN_DIR/" 2>/dev/null || true
            ;;
    esac
}

# Main execution
echo "=== SQLite3/libSQL Library Setup ==="
echo "Platform: $OS $ARCH"

# Download and build
download_sqlite_amalgamation
build_sqlite
copy_to_test_directory

echo ""
echo "=== Setup Complete ==="
echo "Libraries built in: $LIBS_DIR"
echo "Libraries copied to test directory"
echo ""
echo "Available libraries:"
ls -la "$LIBS_DIR"/*.{so,dylib,dll} 2>/dev/null || true