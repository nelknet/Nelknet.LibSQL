#!/bin/bash
set -e

# Source cargo/rust environment if available
if [ -f "$HOME/.cargo/env" ]; then
    source "$HOME/.cargo/env"
fi

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}Building native libSQL libraries for all platforms...${NC}"

# Create output directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$( cd "$SCRIPT_DIR/.." && pwd )"
OUTPUT_DIR="$PROJECT_ROOT/src/Nelknet.LibSQL.Bindings/runtimes"

# Clone libSQL if not exists
LIBSQL_DIR="$PROJECT_ROOT/temp/libsql"
if [ ! -d "$LIBSQL_DIR" ]; then
    echo -e "${YELLOW}Cloning libSQL repository...${NC}"
    mkdir -p "$PROJECT_ROOT/temp"
    git clone --depth 1 https://github.com/tursodatabase/libsql.git "$LIBSQL_DIR"
else
    echo -e "${YELLOW}Updating libSQL repository...${NC}"
    cd "$LIBSQL_DIR"
    git pull
fi

# Build for each platform
cd "$LIBSQL_DIR/bindings/c"

# macOS ARM64 (native on M1/M2)
echo -e "${YELLOW}Building for macOS ARM64...${NC}"
cargo build --release --target aarch64-apple-darwin
mkdir -p "$OUTPUT_DIR/osx-arm64/native"
cd "$LIBSQL_DIR/target/aarch64-apple-darwin/release"
ar -x libsql_experimental.a
clang -dynamiclib -o libsql.dylib *.o \
    -framework Security -framework CoreFoundation \
    -lSystem -lc -lm
cp libsql.dylib "$OUTPUT_DIR/osx-arm64/native/"
rm *.o
cd "$LIBSQL_DIR/bindings/c"

# Linux x64
echo -e "${YELLOW}Building for Linux x64...${NC}"
if command -v cross &> /dev/null; then
    cross build --release --target x86_64-unknown-linux-gnu
else
    echo -e "${RED}Warning: 'cross' not installed. Installing...${NC}"
    cargo install cross --git https://github.com/cross-rs/cross
    cross build --release --target x86_64-unknown-linux-gnu
fi
mkdir -p "$OUTPUT_DIR/linux-x64/native"
cd "$LIBSQL_DIR/target/x86_64-unknown-linux-gnu/release"
ar -x libsql_experimental.a
# Use cross-compiled gcc if available, otherwise try native
if command -v x86_64-linux-gnu-gcc &> /dev/null; then
    x86_64-linux-gnu-gcc -shared -o libsql.so *.o -lpthread -ldl -lm
else
    echo -e "${YELLOW}Using clang for Linux library...${NC}"
    clang -target x86_64-unknown-linux-gnu -shared -o libsql.so *.o -lpthread -ldl -lm
fi
cp libsql.so "$OUTPUT_DIR/linux-x64/native/"
rm *.o
cd "$LIBSQL_DIR/bindings/c"

# Windows x64
echo -e "${YELLOW}Building for Windows x64...${NC}"
cargo build --release --target x86_64-pc-windows-gnu
mkdir -p "$OUTPUT_DIR/win-x64/native"
cd "$LIBSQL_DIR/target/x86_64-pc-windows-gnu/release"
# For Windows, we need to create a DLL from the static library
# This is more complex and might need MinGW
if command -v x86_64-w64-mingw32-gcc &> /dev/null; then
    ar -x libsql_experimental.a
    x86_64-w64-mingw32-gcc -shared -o libsql.dll *.o \
        -lws2_32 -ladvapi32 -luserenv -lbcrypt -static-libgcc
    cp libsql.dll "$OUTPUT_DIR/win-x64/native/"
    rm *.o
else
    echo -e "${RED}Warning: MinGW not found. Windows DLL not built.${NC}"
    echo -e "${RED}Install with: brew install mingw-w64${NC}"
fi

# Create version file
cd "$LIBSQL_DIR"
GIT_REV=$(git rev-parse HEAD)
echo "Built from commit: $GIT_REV" > "$OUTPUT_DIR/LIBSQL_VERSION"
date >> "$OUTPUT_DIR/LIBSQL_VERSION"

# Show results
echo -e "${GREEN}Build complete! Native libraries:${NC}"
find "$OUTPUT_DIR" -name "libsql.*" -exec ls -lh {} \;