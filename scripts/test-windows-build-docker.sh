#!/bin/bash
set -e

# This script tests the Windows build process in a Docker container
# that mimics the GitHub Actions ubuntu-latest environment

echo "Testing Windows build in Docker container..."

# Build and run the Docker container
docker run --rm \
  -v "$(pwd):/workspace" \
  -w /workspace \
  ubuntu:22.04 \
  bash -c '
    set -e
    
    echo "=== Setting up environment ==="
    apt-get update
    apt-get install -y \
      curl \
      git \
      build-essential \
      mingw-w64 \
      pkg-config \
      libssl-dev \
      ca-certificates
    
    echo "=== Installing Rust ==="
    curl --proto "=https" --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y --default-toolchain stable
    source "$HOME/.cargo/env"
    
    echo "=== Adding Windows target ==="
    rustup target add x86_64-pc-windows-gnu
    
    echo "=== Cloning libSQL ==="
    if [ ! -d "temp/libsql" ]; then
      mkdir -p temp
      git clone --depth 1 https://github.com/tursodatabase/libsql.git temp/libsql
    fi
    
    cd temp/libsql/bindings/c
    
    echo "=== Checking for rust-toolchain.toml ==="
    if [ -f "../../rust-toolchain.toml" ]; then
      echo "Found rust-toolchain.toml:"
      cat ../../rust-toolchain.toml
      echo "Temporarily renaming..."
      mv ../../rust-toolchain.toml ../../rust-toolchain.toml.bak
    fi
    
    echo "=== Building libSQL for Windows ==="
    export CC_x86_64_pc_windows_gnu=x86_64-w64-mingw32-gcc
    export AR_x86_64_pc_windows_gnu=x86_64-w64-mingw32-ar
    export CARGO_TARGET_X86_64_PC_WINDOWS_GNU_LINKER=x86_64-w64-mingw32-gcc
    
    LIBSQLITE3_FLAGS="-DSQLITE_ENABLE_COLUMN_METADATA" LIBSQL_BUNDLED=1 \
      cargo build --release --target x86_64-pc-windows-gnu
    
    echo "=== Creating DLL ==="
    cd ../../target/x86_64-pc-windows-gnu/release
    
    echo "Listing all static libraries..."
    find . -name "*.a" -type f | grep -v dll.a
    
    echo "Extracting libsql_experimental.a..."
    ar -x libsql_experimental.a
    
    # Try extracting the SQLite library
    if [ -f "./build/libsql-ffi-9a49ef0ef90397da/out/build/libsqlite3mc_static.a" ]; then
      echo "Found vendored SQLite library, extracting..."
      ar -x "./build/libsql-ffi-9a49ef0ef90397da/out/build/libsqlite3mc_static.a"
    else
      SQLITE_LIB=$(find ./build -name "libsqlite3*.a" -type f | grep -v dll.a | head -1)
      if [ -n "$SQLITE_LIB" ]; then
        echo "Found SQLite library at $SQLITE_LIB, extracting..."
        ar -x "$SQLITE_LIB"
      fi
    fi
    
    echo "Checking for SQLite symbols..."
    nm *.o | grep -c "sqlite3_" || echo "No SQLite symbols found"
    
    echo "Creating DLL..."
    x86_64-w64-mingw32-gcc -shared -o libsql.dll *.o \
      -Wl,--export-all-symbols \
      -lws2_32 -ladvapi32 -luserenv -lbcrypt -lntdll -lcrypt32 \
      -lsecur32 -lkernel32 -lole32 -loleaut32 -luuid -lncrypt \
      -static-libgcc
    
    echo "Checking DLL..."
    ls -lh libsql.dll
    file libsql.dll
    
    # Copy the DLL to the workspace
    cp libsql.dll /workspace/libsql-test.dll
    echo "DLL copied to workspace as libsql-test.dll"
  '