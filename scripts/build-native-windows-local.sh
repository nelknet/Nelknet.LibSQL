#!/bin/bash
# Local Windows build script for testing SSL certificate fixes
# Run this in Git Bash or MSYS2 on Windows

set -e

echo "Building libSQL native library for Windows..."

# Clone libSQL if not already present
if [ ! -d "libsql" ]; then
    echo "Cloning libSQL repository..."
    git clone https://github.com/tursodatabase/libsql.git
fi

cd libsql
git fetch
git checkout main
git pull

# Configure Git for Windows SSL
git config --global http.sslBackend schannel
git config --global http.sslVerify true

# Build using cargo
cd bindings/c
echo "Building with cargo..."
cargo build --release --target x86_64-pc-windows-gnu

# Create DLL
cd ../../target/x86_64-pc-windows-gnu/release
echo "Creating DLL..."
ar -x libsql_experimental.a
gcc -shared -o libsql.dll *.o \
    -Wl,--export-all-symbols \
    -lws2_32 -ladvapi32 -luserenv -lbcrypt -lntdll -lcrypt32 \
    -lsecur32 -lkernel32 -lole32 -loleaut32 -luuid -lncrypt \
    -static-libgcc

# Copy to output
mkdir -p ../../../../../src/Nelknet.LibSQL.Bindings/runtimes/win-x64/native
cp libsql.dll ../../../../../src/Nelknet.LibSQL.Bindings/runtimes/win-x64/native/

echo "Windows native library built successfully!"
echo "Library location: src/Nelknet.LibSQL.Bindings/runtimes/win-x64/native/libsql.dll"