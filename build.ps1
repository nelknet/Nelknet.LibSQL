param(
    [Parameter()]
    [ValidateSet("ManagedOnly", "Full")]
    [string]$BuildType = "ManagedOnly",
    
    [Parameter()]
    [switch]$SkipArm,
    
    [Parameter()]
    [string]$Configuration = "Release"
)

Write-Host "Building Nelknet.LibSQL with BuildType=$BuildType" -ForegroundColor Green

$buildArgs = @(
    "build",
    "-c", $Configuration,
    "-p:BuildType=$BuildType"
)

if ($SkipArm) {
    $buildArgs += "-p:SkipArm=true"
}

Write-Host "Running: dotnet $($buildArgs -join ' ')" -ForegroundColor Yellow
& dotnet @buildArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit $LASTEXITCODE
}

Write-Host "Build completed successfully" -ForegroundColor Green