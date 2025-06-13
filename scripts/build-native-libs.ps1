param(
    [string]$LibSQLTag = "libsql-0.6.2"
)

# Script to build libSQL native libraries for Windows
# Based on go-libsql's approach but adapted for .NET

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$BuildDir = Join-Path $ProjectRoot "build-temp"
$OutputDir = Join-Path $ProjectRoot "src\Nelknet.LibSQL.Bindings\runtimes"

Write-Host "=== Building libSQL Native Libraries ===" -ForegroundColor Green
Write-Host "libSQL version: $LibSQLTag"
Write-Host "Build directory: $BuildDir"
Write-Host "Output directory: $OutputDir"

function Write-Status {
    param([string]$Message)
    Write-Host "[BUILD] " -ForegroundColor Green -NoNewline
    Write-Host $Message
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] " -ForegroundColor Red -NoNewline
    Write-Host $Message
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] " -ForegroundColor Yellow -NoNewline
    Write-Host $Message
}

# Clean and create directories
Write-Status "Preparing build environment..."
if (Test-Path $BuildDir) {
    Remove-Item -Recurse -Force $BuildDir
}
New-Item -ItemType Directory -Force -Path $BuildDir | Out-Null
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Clone libSQL
Write-Status "Cloning libSQL repository..."
Set-Location $BuildDir
git clone https://github.com/tursodatabase/libsql.git
Set-Location (Join-Path $BuildDir "libsql")
git checkout $LibSQLTag

# Detect architecture
$Arch = if ([Environment]::Is64BitProcess) { "x64" } else { "x86" }
Write-Status "Detected architecture: Windows $Arch"

# Check for Rust
if (!(Get-Command cargo -ErrorAction SilentlyContinue)) {
    Write-Error "Rust/Cargo not found. Please install Rust from https://rustup.rs"
    exit 1
}

# Build libSQL C bindings
Write-Status "Building libSQL C bindings..."
Set-Location (Join-Path $BuildDir "libsql\bindings\c")

# Build the library
cargo build --release

# Check if we have the Visual Studio tools needed to create a DLL
$VSWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $VSWhere) {
    $VSPath = & $VSWhere -latest -property installationPath
    $VCVarsPath = Join-Path $VSPath "VC\Auxiliary\Build\vcvars64.bat"
    
    if (Test-Path $VCVarsPath) {
        Write-Status "Found Visual Studio, creating DLL..."
        
        # Create a temporary directory for object files
        $ObjDir = Join-Path $BuildDir "obj"
        New-Item -ItemType Directory -Force -Path $ObjDir | Out-Null
        
        # Extract object files from the static library
        Set-Location $ObjDir
        $StaticLib = Join-Path $BuildDir "libsql\target\release\sql_experimental.lib"
        
        # Use lib.exe to extract object files
        & cmd /c "`"$VCVarsPath`" && lib /list `"$StaticLib`"" | ForEach-Object {
            if ($_ -match '\.o$') {
                & cmd /c "`"$VCVarsPath`" && lib /extract:$_ `"$StaticLib`""
            }
        }
        
        # Link into DLL
        $RID = if ($Arch -eq "x64") { "win-x64" } else { "win-x86" }
        $OutputPath = Join-Path $OutputDir "$RID\native"
        New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null
        
        & cmd /c "`"$VCVarsPath`" && link /dll /out:libsql.dll *.o advapi32.lib bcrypt.lib kernel32.lib userenv.lib ws2_32.lib msvcrt.lib"
        
        if (Test-Path "libsql.dll") {
            Copy-Item "libsql.dll" (Join-Path $OutputPath "libsql.dll")
            Write-Status "Created libsql.dll for $RID"
        } else {
            Write-Error "Failed to create DLL"
        }
    } else {
        Write-Warning "Visual Studio tools not found at expected location"
    }
} else {
    Write-Warning "Visual Studio not found. Attempting MinGW build..."
    
    # Try MinGW if available
    if (Get-Command gcc -ErrorAction SilentlyContinue) {
        Write-Status "Using MinGW to create DLL..."
        
        $ObjDir = Join-Path $BuildDir "obj"
        New-Item -ItemType Directory -Force -Path $ObjDir | Out-Null
        Set-Location $ObjDir
        
        # Extract object files
        $StaticLib = Join-Path $BuildDir "libsql\target\release\libsql_experimental.a"
        ar -x $StaticLib
        
        # Create DLL
        gcc -shared -o libsql.dll *.o -lws2_32 -ladvapi32 -lbcrypt -lkernel32 -luserenv
        
        $RID = if ($Arch -eq "x64") { "win-x64" } else { "win-x86" }
        $OutputPath = Join-Path $OutputDir "$RID\native"
        New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null
        
        if (Test-Path "libsql.dll") {
            Copy-Item "libsql.dll" (Join-Path $OutputPath "libsql.dll")
            Write-Status "Created libsql.dll for $RID"
        }
    } else {
        Write-Error "No suitable compiler found (Visual Studio or MinGW)"
        exit 1
    }
}

# Copy header file
$HeaderSrc = Join-Path $BuildDir "libsql\bindings\c\include\libsql.h"
$HeaderDst = Join-Path $ProjectRoot "src\Nelknet.LibSQL.Bindings\libsql.h"
if (Test-Path $HeaderSrc) {
    Copy-Item $HeaderSrc $HeaderDst
    Write-Status "Copied libsql.h header file"
}

# Clean up
Write-Status "Cleaning up..."
Set-Location $ProjectRoot
Remove-Item -Recurse -Force $BuildDir

Write-Status "Build complete! Native libraries are in:"
Get-ChildItem -Path $OutputDir -Recurse -Include "*.dll" | ForEach-Object {
    Write-Host "  - $_"
}

# Create version file
Set-Content -Path (Join-Path $OutputDir "LIBSQL_VERSION") -Value $LibSQLTag

Write-Status "Done!"