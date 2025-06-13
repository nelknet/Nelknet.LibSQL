using Nelknet.LibSQL.Native;
using System;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class LibSQLBindingsTests
{
    [Fact]
    public void SafeHandles_ShouldCreateAndDisposeCorrectly()
    {
        // Test creating SafeHandles with invalid handles
        using var dbHandle = new LibSQLDatabaseHandle();
        using var connHandle = new LibSQLConnectionHandle();
        using var stmtHandle = new LibSQLStatementHandle();
        using var rowsHandle = new LibSQLRowsHandle();
        using var rowHandle = new LibSQLRowHandle();
        using var stringHandle = new LibSQLStringHandle();
        
        // All should be invalid initially
        Assert.True(dbHandle.IsInvalid);
        Assert.True(connHandle.IsInvalid);
        Assert.True(stmtHandle.IsInvalid);
        Assert.True(rowsHandle.IsInvalid);
        Assert.True(rowHandle.IsInvalid);
        Assert.True(stringHandle.IsInvalid);
    }
    
    [Fact]
    public void LibSQLHelper_IsSuccess_ShouldReturnCorrectResult()
    {
        // Test success case
        Assert.True(LibSQLHelper.IsSuccess(0));
        
        // Test failure cases
        Assert.False(LibSQLHelper.IsSuccess(1));
        Assert.False(LibSQLHelper.IsSuccess(-1));
    }
    
    [Fact]
    public void LibSQLHelper_ThrowIfError_ShouldThrowOnError()
    {
        // Should not throw on success
        LibSQLHelper.ThrowIfError(0);
        
        // Should throw on error
        Assert.Throws<LibSQLException>(() => LibSQLHelper.ThrowIfError(1));
        Assert.Throws<LibSQLException>(() => LibSQLHelper.ThrowIfError(-1, "Custom error"));
    }
    
    [Fact]
    public void LibSQLHelper_GetStringFromPtr_ShouldHandleNullPtr()
    {
        // Should return null for IntPtr.Zero
        var result = LibSQLHelper.GetStringFromPtr(IntPtr.Zero);
        Assert.Null(result);
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
    public void LibSQLException_ShouldCreateCorrectly()
    {
        var ex1 = new LibSQLException("Test message");
        Assert.Equal("Test message", ex1.Message);
        
        var innerEx = new InvalidOperationException("Inner");
        var ex2 = new LibSQLException("Test message", innerEx);
        Assert.Equal("Test message", ex2.Message);
        Assert.Same(innerEx, ex2.InnerException);
    }
}