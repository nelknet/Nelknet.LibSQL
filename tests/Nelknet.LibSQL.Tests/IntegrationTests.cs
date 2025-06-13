using Nelknet.LibSQL.Bindings;
using Nelknet.LibSQL.Native;
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class IntegrationTests
{
    [Fact]
    public void LibraryLoading_CompleteWorkflow_ShouldBeConsistent()
    {
        // Test the complete library loading workflow
        
        // 1. Verify we can detect the current platform
        Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                   RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
        
        // 2. Verify the architecture is supported
        var arch = RuntimeInformation.ProcessArchitecture;
        Assert.True(arch == Architecture.X64 || 
                   arch == Architecture.X86 || 
                   arch == Architecture.Arm64 || 
                   arch == Architecture.Arm);
        
        // 3. Test library initialization consistency
        var result1 = LibSQLNativeLibrary.TryInitialize();
        var result2 = LibSQLNativeLibrary.TryInitialize();
        Assert.Equal(result1, result2); // Should be idempotent
        
        // 4. Test that the LibSQLNative.Initialize() behavior is consistent
        // Since we don't have the actual library, both calls should fail the same way
        Exception? exception1 = null;
        Exception? exception2 = null;
        
        try { LibSQLNative.Initialize(); } 
        catch (Exception ex) { exception1 = ex; }
        
        try { LibSQLNative.Initialize(); } 
        catch (Exception ex) { exception2 = ex; }
        
        // Both should either succeed or fail the same way
        Assert.Equal(exception1?.GetType(), exception2?.GetType());
        
        if (exception1 != null && exception2 != null)
        {
            Assert.Equal(exception1.Message, exception2.Message);
        }
    }

    [Fact]
    public void SafeHandles_CompleteLifecycle_ShouldWorkCorrectly()
    {
        // Test complete lifecycle of all SafeHandle types
        
        // Create all handle types
        var handles = new SafeHandleZeroOrMinusOneIsInvalid[]
        {
            new LibSQLDatabaseHandle(),
            new LibSQLConnectionHandle(),
            new LibSQLStatementHandle(),
            new LibSQLRowsHandle(),
            new LibSQLRowHandle(),
            new LibSQLStringHandle(),
            new LibSQLAllocatedPointerHandle()
        };
        
        // All should start invalid
        foreach (var handle in handles)
        {
            Assert.True(handle.IsInvalid);
            Assert.False(handle.IsClosed);
        }
        
        // Dispose all handles
        foreach (var handle in handles)
        {
            handle.Dispose();
            Assert.True(handle.IsClosed);
        }
    }

    [Fact]
    public void NativeStructs_InteropCompatibility_ShouldBeCorrect()
    {
        // Test that our native structs have the correct characteristics for P/Invoke
        
        // LibSQLConfig should be blittable for P/Invoke
        var configSize = Marshal.SizeOf<LibSQLConfig>();
        Assert.True(configSize > 0);
        
        // LibSQLReplicated should be blittable
        var replicatedSize = Marshal.SizeOf<LibSQLReplicated>();
        Assert.True(replicatedSize > 0);
        
        // LibSQLBlob should be blittable
        var blobSize = Marshal.SizeOf<LibSQLBlob>();
        Assert.True(blobSize > 0);
        
        // Verify that structs can be allocated and default-initialized
        var config = new LibSQLConfig();
        var replicated = new LibSQLReplicated();
        var blob = new LibSQLBlob();
        
        // Should not throw and should have sensible default values
        Assert.Equal(IntPtr.Zero, config.DbPath);
        Assert.Equal(0, replicated.FrameNo);
        Assert.Equal(IntPtr.Zero, blob.Ptr);
    }

    [Fact]
    public void ErrorCodes_InteropCompatibility_ShouldMatchSQLiteConstants()
    {
        // Verify that our error codes match the expected SQLite constants
        // These values are critical for interop correctness
        
        Assert.Equal(0, (int)LibSQLResultCode.Ok);
        Assert.Equal(1, (int)LibSQLResultCode.Error);
        Assert.Equal(5, (int)LibSQLResultCode.Busy);
        Assert.Equal(6, (int)LibSQLResultCode.Locked);
        Assert.Equal(7, (int)LibSQLResultCode.NoMem);
        Assert.Equal(19, (int)LibSQLResultCode.Constraint);
        Assert.Equal(100, (int)LibSQLResultCode.Row);
        Assert.Equal(101, (int)LibSQLResultCode.Done);
        
        // Test that casting works correctly
        var okCode = LibSQLResultCode.Ok;
        var errorCode = LibSQLResultCode.Error;
        
        Assert.True((int)okCode == 0);
        Assert.True((int)errorCode != 0);
    }

    [Fact]
    public void Constants_ShouldBeAccessibleAndCorrect()
    {
        // Test that all our constants are accessible and have expected values
        
        // Library name should be correct
        Assert.Equal("libsql", LibSQLNativeLibrary.LibraryName);
        
        // Type constants should be correct
        Assert.Equal(1, LibSQLType.Int);
        Assert.Equal(2, LibSQLType.Float);
        Assert.Equal(3, LibSQLType.Text);
        Assert.Equal(4, LibSQLType.Blob);
        Assert.Equal(5, LibSQLType.Null);
        
        // All constants should be distinct
        var types = new[] { LibSQLType.Int, LibSQLType.Float, LibSQLType.Text, LibSQLType.Blob, LibSQLType.Null };
        for (int i = 0; i < types.Length; i++)
        {
            for (int j = i + 1; j < types.Length; j++)
            {
                Assert.NotEqual(types[i], types[j]);
            }
        }
    }
}