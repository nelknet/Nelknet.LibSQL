#!/bin/bash
# Test different approaches to creating the Windows DLL
# Run this inside the Docker container after building libSQL

set -e

if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <path-to-libsql-target-release>"
    echo "Example: $0 /workspace/temp/libsql/target/x86_64-pc-windows-gnu/release"
    exit 1
fi

RELEASE_DIR="$1"
cd "$RELEASE_DIR"

echo "=== Testing DLL creation approaches ==="
echo "Working directory: $(pwd)"

# Clean up any previous attempts
rm -f *.o libsql-test*.dll

echo ""
echo "=== Approach 1: Just libsql_experimental.a ==="
ar -x libsql_experimental.a
echo "Object files: $(ls *.o | wc -l)"
echo "SQLite symbols: $(nm *.o 2>/dev/null | grep -c "sqlite3_" || echo "0")"

x86_64-w64-mingw32-gcc -shared -o libsql-test1.dll *.o \
    -Wl,--export-all-symbols \
    -lws2_32 -ladvapi32 -luserenv -lbcrypt -lntdll -lcrypt32 \
    -lsecur32 -lkernel32 -lole32 -loleaut32 -luuid -lncrypt \
    -static-libgcc 2>&1 | tee test1-errors.log || true

if [ -f libsql-test1.dll ]; then
    echo "Success! DLL created: $(ls -lh libsql-test1.dll)"
else
    echo "Failed. Undefined symbols:"
    grep "undefined reference" test1-errors.log | cut -d"'" -f2 | sort | uniq -c | head -10
fi

rm -f *.o

echo ""
echo "=== Approach 2: libsql_experimental.a + vendored SQLite ==="
ar -x libsql_experimental.a

# Find and extract SQLite library
SQLITE_LIB=$(find . -name "libsqlite3*.a" -type f | grep -v dll.a | head -1)
if [ -n "$SQLITE_LIB" ]; then
    echo "Found SQLite library: $SQLITE_LIB"
    ar -x "$SQLITE_LIB"
fi

echo "Object files: $(ls *.o | wc -l)"
echo "SQLite symbols: $(nm *.o 2>/dev/null | grep -c "sqlite3_" || echo "0")"

x86_64-w64-mingw32-gcc -shared -o libsql-test2.dll *.o \
    -Wl,--export-all-symbols \
    -lws2_32 -ladvapi32 -luserenv -lbcrypt -lntdll -lcrypt32 \
    -lsecur32 -lkernel32 -lole32 -loleaut32 -luuid -lncrypt \
    -static-libgcc 2>&1 | tee test2-errors.log || true

if [ -f libsql-test2.dll ]; then
    echo "Success! DLL created: $(ls -lh libsql-test2.dll)"
else
    echo "Failed. Undefined symbols:"
    grep "undefined reference" test2-errors.log | cut -d"'" -f2 | sort | uniq -c | head -10
fi

rm -f *.o

echo ""
echo "=== Approach 3: Extract all deps/*.a libraries ==="
ar -x libsql_experimental.a

# Extract all static libraries from deps
for lib in deps/*.a; do
    if [ -f "$lib" ] && [[ ! "$lib" =~ dll\.a$ ]]; then
        echo "Extracting: $lib"
        ar -x "$lib" 2>/dev/null || true
    fi
done

echo "Object files: $(ls *.o | wc -l)"
echo "SQLite symbols: $(nm *.o 2>/dev/null | grep -c "sqlite3_" || echo "0")"

x86_64-w64-mingw32-gcc -shared -o libsql-test3.dll *.o \
    -Wl,--export-all-symbols \
    -lws2_32 -ladvapi32 -luserenv -lbcrypt -lntdll -lcrypt32 \
    -lsecur32 -lkernel32 -lole32 -loleaut32 -luuid -lncrypt \
    -static-libgcc 2>&1 | tee test3-errors.log || true

if [ -f libsql-test3.dll ]; then
    echo "Success! DLL created: $(ls -lh libsql-test3.dll)"
    echo "Copying successful DLL to workspace..."
    cp libsql-test3.dll /workspace/libsql-working.dll
else
    echo "Failed. Undefined symbols:"
    grep "undefined reference" test3-errors.log | cut -d"'" -f2 | sort | uniq -c | head -10
fi

rm -f *.o

echo ""
echo "=== Summary ==="
ls -lh libsql-test*.dll 2>/dev/null || echo "No DLLs were successfully created"

echo ""
echo "=== Checking what static libraries are available ==="
echo "In current directory:"
find . -name "*.a" -type f | grep -v dll.a | sort
echo ""
echo "In deps:"
ls -la deps/*.a 2>/dev/null | grep -v dll.a || echo "No static libraries in deps/"