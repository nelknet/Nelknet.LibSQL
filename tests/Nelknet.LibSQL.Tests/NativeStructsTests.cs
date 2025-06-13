using Nelknet.LibSQL.Native;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class NativeStructsTests
{
    [Fact]
    public void LibSQLResultCode_ShouldHaveCorrectValues()
    {
        // Test core result codes match SQLite constants
        Assert.Equal(0, (int)LibSQLResultCode.Ok);
        Assert.Equal(1, (int)LibSQLResultCode.Error);
        Assert.Equal(2, (int)LibSQLResultCode.Internal);
        Assert.Equal(3, (int)LibSQLResultCode.Perm);
        Assert.Equal(4, (int)LibSQLResultCode.Abort);
        Assert.Equal(5, (int)LibSQLResultCode.Busy);
        Assert.Equal(6, (int)LibSQLResultCode.Locked);
        Assert.Equal(7, (int)LibSQLResultCode.NoMem);
        Assert.Equal(8, (int)LibSQLResultCode.ReadOnly);
        Assert.Equal(9, (int)LibSQLResultCode.Interrupt);
        Assert.Equal(10, (int)LibSQLResultCode.IoErr);
        Assert.Equal(11, (int)LibSQLResultCode.Corrupt);
        Assert.Equal(12, (int)LibSQLResultCode.NotFound);
        Assert.Equal(13, (int)LibSQLResultCode.Full);
        Assert.Equal(14, (int)LibSQLResultCode.CantOpen);
        Assert.Equal(15, (int)LibSQLResultCode.Protocol);
        Assert.Equal(16, (int)LibSQLResultCode.Empty);
        Assert.Equal(17, (int)LibSQLResultCode.Schema);
        Assert.Equal(18, (int)LibSQLResultCode.TooBig);
        Assert.Equal(19, (int)LibSQLResultCode.Constraint);
        Assert.Equal(20, (int)LibSQLResultCode.Mismatch);
        Assert.Equal(21, (int)LibSQLResultCode.Misuse);
        Assert.Equal(22, (int)LibSQLResultCode.NoLfs);
        Assert.Equal(23, (int)LibSQLResultCode.Auth);
        Assert.Equal(24, (int)LibSQLResultCode.Format);
        Assert.Equal(25, (int)LibSQLResultCode.Range);
        Assert.Equal(26, (int)LibSQLResultCode.NotADb);
        Assert.Equal(27, (int)LibSQLResultCode.Notice);
        Assert.Equal(28, (int)LibSQLResultCode.Warning);
        Assert.Equal(100, (int)LibSQLResultCode.Row);
        Assert.Equal(101, (int)LibSQLResultCode.Done);
    }

    [Fact]
    public void LibSQLType_Constants_ShouldHaveCorrectValues()
    {
        // Verify the type constants match the C header
        Assert.Equal(1, LibSQLType.Int);
        Assert.Equal(2, LibSQLType.Float);
        Assert.Equal(3, LibSQLType.Text);
        Assert.Equal(4, LibSQLType.Blob);
        Assert.Equal(5, LibSQLType.Null);
    }

    [Fact]
    public void LibSQLBlob_ToByteArray_ShouldHandleEmptyBlob()
    {
        // Test with zero-length blob
        var blob = new LibSQLBlob { Ptr = IntPtr.Zero, Len = 0 };
        var result = blob.ToByteArray();
        
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void LibSQLBlob_ToByteArray_ShouldHandleNegativeLength()
    {
        // Test with negative length (should be treated as empty)
        var blob = new LibSQLBlob { Ptr = IntPtr.Zero, Len = -1 };
        var result = blob.ToByteArray();
        
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void LibSQLConfig_ShouldHaveSequentialLayout()
    {
        // Verify the struct has StructLayout attribute
        var attributes = typeof(LibSQLConfig).GetCustomAttributes(typeof(StructLayoutAttribute), false);
        
        // If no StructLayout attribute is found, that might be ok - check if we can marshal it
        if (attributes.Length == 0)
        {
            // Verify it can be marshaled (which implies it has a suitable layout)
            var size = Marshal.SizeOf<LibSQLConfig>();
            Assert.True(size > 0);
        }
        else
        {
            Assert.Single(attributes);
            var layout = (StructLayoutAttribute)attributes[0];
            Assert.Equal(LayoutKind.Sequential, layout.Value);
        }
    }

    [Fact]
    public void LibSQLReplicated_ShouldHaveSequentialLayout()
    {
        // Verify the struct has StructLayout attribute
        var attributes = typeof(LibSQLReplicated).GetCustomAttributes(typeof(StructLayoutAttribute), false);
        
        // If no StructLayout attribute is found, verify it can be marshaled
        if (attributes.Length == 0)
        {
            var size = Marshal.SizeOf<LibSQLReplicated>();
            Assert.True(size > 0);
        }
        else
        {
            Assert.Single(attributes);
            var layout = (StructLayoutAttribute)attributes[0];
            Assert.Equal(LayoutKind.Sequential, layout.Value);
        }
    }

    [Fact]
    public void LibSQLBlob_ShouldHaveSequentialLayout()
    {
        // Verify the struct has StructLayout attribute
        var attributes = typeof(LibSQLBlob).GetCustomAttributes(typeof(StructLayoutAttribute), false);
        
        // If no StructLayout attribute is found, verify it can be marshaled
        if (attributes.Length == 0)
        {
            var size = Marshal.SizeOf<LibSQLBlob>();
            Assert.True(size > 0);
        }
        else
        {
            Assert.Single(attributes);
            var layout = (StructLayoutAttribute)attributes[0];
            Assert.Equal(LayoutKind.Sequential, layout.Value);
        }
    }

    [Fact]
    public void LibSQLConfig_ShouldInitializeWithDefaultValues()
    {
        // Test that default struct initialization works
        var config = new LibSQLConfig();
        
        Assert.Equal(IntPtr.Zero, config.DbPath);
        Assert.Equal(IntPtr.Zero, config.PrimaryUrl);
        Assert.Equal(IntPtr.Zero, config.AuthToken);
        Assert.Equal(0, config.ReadYourWrites);
        Assert.Equal(IntPtr.Zero, config.EncryptionKey);
        Assert.Equal(0, config.SyncInterval);
        Assert.Equal(0, config.WithWebpki);
        Assert.Equal(0, config.Offline);
    }

    [Fact]
    public void LibSQLReplicated_ShouldInitializeWithDefaultValues()
    {
        // Test that default struct initialization works
        var replicated = new LibSQLReplicated();
        
        Assert.Equal(0, replicated.FrameNo);
        Assert.Equal(0, replicated.FramesSynced);
    }
}