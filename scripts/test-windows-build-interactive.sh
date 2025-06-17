#!/bin/bash
set -e

# This script starts an interactive Docker container for testing Windows builds

echo "Starting interactive Docker container for Windows build testing..."

docker run --rm -it \
  -v "$(pwd):/workspace" \
  -w /workspace \
  ubuntu:22.04 \
  bash -c '
    echo "=== Setting up environment ==="
    apt-get update
    apt-get install -y \
      curl \
      git \
      build-essential \
      mingw-w64 \
      pkg-config \
      libssl-dev \
      ca-certificates \
      vim \
      less
    
    echo "=== Installing Rust ==="
    curl --proto "=https" --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y --default-toolchain stable
    source "$HOME/.cargo/env"
    
    echo "=== Adding Windows target ==="
    rustup target add x86_64-pc-windows-gnu
    
    echo "=== Environment ready! ==="
    echo ""
    echo "To test the Windows build:"
    echo "1. cd temp/libsql/bindings/c"
    echo "2. Set environment variables:"
    echo "   export CC_x86_64_pc_windows_gnu=x86_64-w64-mingw32-gcc"
    echo "   export AR_x86_64_pc_windows_gnu=x86_64-w64-mingw32-ar"
    echo "   export CARGO_TARGET_X86_64_PC_WINDOWS_GNU_LINKER=x86_64-w64-mingw32-gcc"
    echo "3. Build: LIBSQL_BUNDLED=1 cargo build --release --target x86_64-pc-windows-gnu"
    echo ""
    
    exec bash
  '