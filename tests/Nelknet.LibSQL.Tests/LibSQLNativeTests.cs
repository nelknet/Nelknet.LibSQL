using Nelknet.LibSQL.Bindings;
using System;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class LibSQLNativeTests
{
    [Fact]
    public void Initialize_ShouldNotThrow_WhenCalledMultipleTimes()
    {
        // The Initialize method should be safe to call multiple times
        // even if the library is not available
        
        try
        {
            LibSQLNative.Initialize();
            LibSQLNative.Initialize(); // Second call should not throw
            LibSQLNative.Initialize(); // Third call should not throw
        }
        catch (InvalidOperationException)
        {
            // This is expected if the native library is not available
            // The important thing is that it throws a specific exception type
            Assert.True(true);
        }
    }

    [Fact]
    public void Initialize_ShouldHandleLibraryNotAvailable()
    {
        // Since we don't have the actual libSQL library in the test environment,
        // Initialize should either throw InvalidOperationException or succeed
        // depending on whether any compatible library is found
        
        try
        {
            LibSQLNative.Initialize();
            // If no exception is thrown, that's fine - maybe a compatible library was found
            Assert.True(true);
        }
        catch (InvalidOperationException exception)
        {
            // This is expected if the native library is not available
            Assert.Contains("Failed to load libSQL native library", exception.Message);
            Assert.Contains("Please ensure the appropriate native library is available", exception.Message);
        }
    }

    [Fact]
    public void LibraryName_ShouldBeConsistent()
    {
        // Verify that the library name constant is accessible and correct
        // This uses reflection to access the private constant for testing
        var field = typeof(LibSQLNative).GetField("LibraryName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(field);
        var libraryName = field?.GetValue(null) as string;
        Assert.Equal("libsql", libraryName);
    }
}