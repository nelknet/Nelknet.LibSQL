using Nelknet.LibSQL.Bindings;
using System;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class LibSQLBindingsTests
{
    [Fact]
    public void LibSQLResultCode_SuccessCase_ShouldBeOk()
    {
        // Test that success case is represented correctly
        Assert.Equal(0, (int)LibSQLResultCode.Ok);
    }
    
    [Fact]
    public void LibSQLResultCode_ErrorCases_ShouldBeNonZero()
    {
        // Test that error cases are non-zero
        Assert.NotEqual(0, (int)LibSQLResultCode.Error);
        Assert.NotEqual(0, (int)LibSQLResultCode.Busy);
        Assert.NotEqual(0, (int)LibSQLResultCode.Locked);
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
}