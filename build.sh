#!/bin/bash

# Default values
BUILD_TYPE="ManagedOnly"
SKIP_ARM=""
CONFIGURATION="Release"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --build-type)
            BUILD_TYPE="$2"
            shift 2
            ;;
        --skip-arm)
            SKIP_ARM="-p:SkipArm=true"
            shift
            ;;
        --configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --help)
            echo "Usage: $0 [options]"
            echo "Options:"
            echo "  --build-type <ManagedOnly|Full>  Build type (default: ManagedOnly)"
            echo "  --skip-arm                       Skip ARM platforms"
            echo "  --configuration <Debug|Release>  Build configuration (default: Release)"
            echo "  --help                          Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

echo "Building Nelknet.LibSQL with BuildType=$BUILD_TYPE"

dotnet build -c "$CONFIGURATION" -p:BuildType="$BUILD_TYPE" $SKIP_ARM

if [ $? -ne 0 ]; then
    echo "Build failed"
    exit 1
fi

echo "Build completed successfully"