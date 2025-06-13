param(
    [Parameter()]
    [ValidateSet("ManagedOnly", "Full", "Both")]
    [string]$PackageType = "ManagedOnly",
    
    [Parameter()]
    [switch]$SkipArm,
    
    [Parameter()]
    [string]$Configuration = "Release",
    
    [Parameter()]
    [string]$OutputDirectory = "artifacts"
)

# Ensure output directory exists
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

function Build-And-Pack {
    param(
        [string]$BuildType
    )
    
    Write-Host "Building and packing $BuildType package..." -ForegroundColor Green
    
    $buildArgs = @(
        "pack",
        "src/Nelknet.LibSQL.Data/Nelknet.LibSQL.Data.csproj",
        "-c", $Configuration,
        "-p:BuildType=$BuildType",
        "-o", $OutputDirectory
    )
    
    if ($SkipArm) {
        $buildArgs += "-p:SkipArm=true"
    }
    
    Write-Host "Running: dotnet $($buildArgs -join ' ')" -ForegroundColor Yellow
    & dotnet @buildArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Pack failed for $BuildType"
        exit $LASTEXITCODE
    }
}

switch ($PackageType) {
    "ManagedOnly" {
        Build-And-Pack -BuildType "ManagedOnly"
    }
    "Full" {
        Build-And-Pack -BuildType "Full"
    }
    "Both" {
        Build-And-Pack -BuildType "ManagedOnly"
        Build-And-Pack -BuildType "Full"
    }
}

Write-Host "Packages created in $OutputDirectory" -ForegroundColor Green
Get-ChildItem -Path $OutputDirectory -Filter "*.nupkg" | ForEach-Object {
    Write-Host "  - $($_.Name)" -ForegroundColor Cyan
}