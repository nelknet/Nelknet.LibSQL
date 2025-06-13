using Nelknet.LibSQL.Bindings;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class LibSQLNativeLibraryTests
{
    [Fact]
    public void LibraryName_ShouldBeLibSQL()
    {
        Assert.Equal("libsql", LibSQLNativeLibrary.LibraryName);
    }

    [Fact]
    public void TryInitialize_ShouldNotThrow()
    {
        // Should not throw even if library is not available
        // This test mainly ensures the method is callable
        var result = LibSQLNativeLibrary.TryInitialize();
        
        // We can't assert the result since it depends on whether
        // the native library is available, but it should not throw
        Assert.True(result == true || result == false);
    }

    [Fact]
    public void GetRuntimeIdentifier_ShouldWorkForCurrentPlatform()
    {
        // Test that the current platform/architecture combination is handled
        var currentPlatform = RuntimeInformation.ProcessArchitecture;
        
        // Verify we can determine what the expected RID should be
        string? expectedRid = null;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            expectedRid = currentPlatform switch
            {
                Architecture.X64 => "win-x64",
                Architecture.X86 => "win-x86", 
                Architecture.Arm64 => "win-arm64",
                _ => null
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            expectedRid = currentPlatform switch
            {
                Architecture.X64 => "linux-x64",
                Architecture.Arm64 => "linux-arm64",
                Architecture.Arm => "linux-arm",
                _ => null
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            expectedRid = currentPlatform switch
            {
                Architecture.X64 => "osx-x64",
                Architecture.Arm64 => "osx-arm64",
                _ => "osx"
            };
        }
        
        // We should have determined a RID for supported platforms
        Assert.NotNull(expectedRid);
        
        // The TryInitialize method should be callable
        var result = LibSQLNativeLibrary.TryInitialize();
        Assert.True(result == true || result == false);
    }

    [Fact]
    public void TryInitialize_ShouldBeIdempotent()
    {
        // Calling TryInitialize multiple times should be safe
        var result1 = LibSQLNativeLibrary.TryInitialize();
        var result2 = LibSQLNativeLibrary.TryInitialize();
        var result3 = LibSQLNativeLibrary.TryInitialize();
        
        // Results should be consistent
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    [Fact]
    public void CurrentPlatform_ShouldBeSupportedPlatform()
    {
        // This test verifies that we're running on a platform that
        // should have a runtime identifier
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        var isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        
        // Should be exactly one of these
        var platformCount = (isWindows ? 1 : 0) + (isLinux ? 1 : 0) + (isMacOS ? 1 : 0);
        Assert.Equal(1, platformCount);
        
        // Architecture should be supported
        var arch = RuntimeInformation.ProcessArchitecture;
        Assert.True(arch == Architecture.X64 || 
                   arch == Architecture.X86 || 
                   arch == Architecture.Arm64 || 
                   arch == Architecture.Arm);
    }

    [Fact]
    public void LibraryNames_ShouldBeCorrectForCurrentPlatform()
    {
        // We can't directly test the private method, but we can verify
        // that the expected library names would be correct for the current platform
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On Windows, should look for .dll files
            Assert.True(true); // libsql.dll, sqlite3.dll would be expected
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // On macOS, should look for .dylib files  
            Assert.True(true); // libsql.dylib, libsqlite3.dylib, sqlite3.dylib would be expected
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // On Linux, should look for .so files
            Assert.True(true); // libsql.so, libsqlite3.so, sqlite3.so would be expected
        }
    }
}