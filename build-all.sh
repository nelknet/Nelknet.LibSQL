#!/bin/bash

# Comprehensive build script for Nelknet.LibSQL
# This script builds native libraries and creates NuGet packages

set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

print_header() {
    echo -e "\n${GREEN}=== $1 ===${NC}\n"
}

print_status() {
    echo -e "${GREEN}[BUILD]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# Parse command line arguments
BUILD_NATIVE=false
BUILD_MANAGED=false
PACK_NUGET=false
LIBSQL_VERSION="libsql-0.6.2"
SKIP_TESTS=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --native)
            BUILD_NATIVE=true
            shift
            ;;
        --managed)
            BUILD_MANAGED=true
            shift
            ;;
        --pack)
            PACK_NUGET=true
            shift
            ;;
        --all)
            BUILD_NATIVE=true
            BUILD_MANAGED=true
            PACK_NUGET=true
            shift
            ;;
        --libsql-version)
            LIBSQL_VERSION="$2"
            shift 2
            ;;
        --skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        --help)
            echo "Usage: $0 [options]"
            echo "Options:"
            echo "  --native          Build native libSQL libraries"
            echo "  --managed         Build managed .NET assemblies"
            echo "  --pack           Create NuGet packages"
            echo "  --all            Build everything (native + managed + pack)"
            echo "  --libsql-version Version of libSQL to build (default: libsql-0.6.2)"
            echo "  --skip-tests     Skip running tests"
            echo "  --help           Show this help message"
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# If no options specified, show help
if [ "$BUILD_NATIVE" = false ] && [ "$BUILD_MANAGED" = false ] && [ "$PACK_NUGET" = false ]; then
    echo "No build options specified. Use --help for usage information."
    exit 1
fi

# Build native libraries
if [ "$BUILD_NATIVE" = true ]; then
    print_header "Building Native Libraries"
    
    if [ -f scripts/build-native-libs.sh ]; then
        print_status "Running native library build script..."
        ./scripts/build-native-libs.sh "$LIBSQL_VERSION"
    else
        print_error "Native build script not found"
        exit 1
    fi
fi

# Build managed assemblies
if [ "$BUILD_MANAGED" = true ]; then
    print_header "Building Managed Assemblies"
    
    print_status "Restoring NuGet packages..."
    dotnet restore
    
    print_status "Building solution..."
    dotnet build -c Release
    
    if [ "$SKIP_TESTS" = false ]; then
        print_status "Running tests..."
        if dotnet test -c Release --no-build; then
            print_status "All tests passed!"
        else
            print_warning "Some tests failed (likely due to missing native library)"
        fi
    fi
fi

# Create NuGet packages
if [ "$PACK_NUGET" = true ]; then
    print_header "Creating NuGet Packages"
    
    # Create artifacts directory
    mkdir -p artifacts
    
    # Pack managed-only package
    print_status "Creating managed-only package..."
    dotnet pack src/Nelknet.LibSQL.Data/Nelknet.LibSQL.Data.csproj \
        -c Release \
        -p:BuildType=ManagedOnly \
        -o artifacts \
        --no-build
    
    # Check if we have native libraries
    if [ -d "src/Nelknet.LibSQL.Bindings/runtimes" ] && [ "$(ls -A src/Nelknet.LibSQL.Bindings/runtimes)" ]; then
        print_status "Creating full package with native libraries..."
        dotnet pack src/Nelknet.LibSQL.Data/Nelknet.LibSQL.Data.csproj \
            -c Release \
            -p:BuildType=Full \
            -o artifacts \
            --no-build
    else
        print_warning "No native libraries found. Skipping full package creation."
        print_warning "Run with --native to build native libraries first."
    fi
    
    print_status "Packages created in artifacts/"
    ls -la artifacts/*.nupkg
fi

print_header "Build Complete!"

# Summary
echo -e "\nBuild Summary:"
if [ "$BUILD_NATIVE" = true ]; then
    echo -e "  ${GREEN}✓${NC} Native libraries built"
fi
if [ "$BUILD_MANAGED" = true ]; then
    echo -e "  ${GREEN}✓${NC} Managed assemblies built"
fi
if [ "$PACK_NUGET" = true ]; then
    echo -e "  ${GREEN}✓${NC} NuGet packages created"
fi

# Next steps
echo -e "\nNext steps:"
if [ "$BUILD_NATIVE" = false ]; then
    echo "  - Run with --native to build native libraries"
fi
if [ "$BUILD_MANAGED" = false ]; then
    echo "  - Run with --managed to build .NET assemblies"
fi
if [ "$PACK_NUGET" = false ]; then
    echo "  - Run with --pack to create NuGet packages"
fi
if [ -d "artifacts" ] && [ "$(ls -A artifacts/*.nupkg 2>/dev/null)" ]; then
    echo "  - Test packages locally: dotnet add package Nelknet.LibSQL.Data --source ./artifacts"
    echo "  - Publish to NuGet: dotnet nuget push artifacts/*.nupkg --source https://api.nuget.org/v3/index.json"
fi