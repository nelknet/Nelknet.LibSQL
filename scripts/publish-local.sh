#!/bin/bash
set -e

# This script publishes packages locally - DO NOT COMMIT API KEY!
# Usage: ./scripts/publish-local.sh YOUR_API_KEY

if [ $# -eq 0 ]; then
    echo "Usage: $0 <NUGET_API_KEY>"
    echo "Example: $0 your-api-key-here"
    exit 1
fi

API_KEY=$1

echo "Publishing NuGet packages..."

# Ensure we have the latest packages built
echo "Building packages..."
dotnet pack src/Nelknet.LibSQL.Bindings/Nelknet.LibSQL.Bindings.csproj -c Release -o ./nupkgs
dotnet pack src/Nelknet.LibSQL.Data/Nelknet.LibSQL.Data.csproj -c Release -o ./nupkgs
dotnet pack src/Nelknet.LibSQL.Data/Nelknet.LibSQL.Data.csproj -c Release -p:BuildType=Full -o ./nupkgs

echo ""
echo "Packages to publish:"
ls -la ./nupkgs/*.nupkg

echo ""
echo "Publishing to NuGet.org..."

# Push each package
for package in ./nupkgs/*.nupkg; do
    echo "Pushing $package..."
    dotnet nuget push "$package" \
        --api-key "$API_KEY" \
        --source https://api.nuget.org/v3/index.json \
        --skip-duplicate
done

echo ""
echo "✅ All packages published successfully!"
echo ""
echo "View your packages at:"
echo "  https://www.nuget.org/packages/Nelknet.LibSQL.Bindings/"
echo "  https://www.nuget.org/packages/Nelknet.LibSQL.Data/"
echo "  https://www.nuget.org/packages/Nelknet.LibSQL.Data.Full/"