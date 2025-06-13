#!/bin/bash

# Default values
PACKAGE_TYPE="ManagedOnly"
SKIP_ARM=""
CONFIGURATION="Release"
OUTPUT_DIRECTORY="artifacts"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --package-type)
            PACKAGE_TYPE="$2"
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
        --output)
            OUTPUT_DIRECTORY="$2"
            shift 2
            ;;
        --help)
            echo "Usage: $0 [options]"
            echo "Options:"
            echo "  --package-type <ManagedOnly|Full|Both>  Package type (default: ManagedOnly)"
            echo "  --skip-arm                              Skip ARM platforms"
            echo "  --configuration <Debug|Release>         Build configuration (default: Release)"
            echo "  --output <directory>                    Output directory (default: artifacts)"
            echo "  --help                                  Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Ensure output directory exists
mkdir -p "$OUTPUT_DIRECTORY"

build_and_pack() {
    local build_type=$1
    
    echo "Building and packing $build_type package..."
    
    dotnet pack src/Nelknet.LibSQL.Data/Nelknet.LibSQL.Data.csproj \
        -c "$CONFIGURATION" \
        -p:BuildType="$build_type" \
        -o "$OUTPUT_DIRECTORY" \
        $SKIP_ARM
    
    if [ $? -ne 0 ]; then
        echo "Pack failed for $build_type"
        exit 1
    fi
}

case $PACKAGE_TYPE in
    ManagedOnly)
        build_and_pack "ManagedOnly"
        ;;
    Full)
        build_and_pack "Full"
        ;;
    Both)
        build_and_pack "ManagedOnly"
        build_and_pack "Full"
        ;;
    *)
        echo "Invalid package type: $PACKAGE_TYPE"
        echo "Use --help for usage information"
        exit 1
        ;;
esac

echo "Packages created in $OUTPUT_DIRECTORY"
ls -la "$OUTPUT_DIRECTORY"/*.nupkg 2>/dev/null | awk '{print "  - " $9}'