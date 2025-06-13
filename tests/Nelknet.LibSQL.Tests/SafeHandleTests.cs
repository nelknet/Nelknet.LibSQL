using Nelknet.LibSQL.Native;
using System;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class SafeHandleTests
{
    [Fact]
    public void AllSafeHandles_ShouldCreateAndDisposeCorrectly()
    {
        // Test creating SafeHandles with invalid handles
        using var dbHandle = new LibSQLDatabaseHandle();
        using var connHandle = new LibSQLConnectionHandle();
        using var stmtHandle = new LibSQLStatementHandle();
        using var rowsHandle = new LibSQLRowsHandle();
        using var rowHandle = new LibSQLRowHandle();
        using var stringHandle = new LibSQLStringHandle();
        using var allocatedHandle = new LibSQLAllocatedPointerHandle();
        
        // All should be invalid initially
        Assert.True(dbHandle.IsInvalid);
        Assert.True(connHandle.IsInvalid);
        Assert.True(stmtHandle.IsInvalid);
        Assert.True(rowsHandle.IsInvalid);
        Assert.True(rowHandle.IsInvalid);
        Assert.True(stringHandle.IsInvalid);
        Assert.True(allocatedHandle.IsInvalid);
        
        // All should be closeable
        Assert.False(dbHandle.IsClosed);
        Assert.False(connHandle.IsClosed);
        Assert.False(stmtHandle.IsClosed);
        Assert.False(rowsHandle.IsClosed);
        Assert.False(rowHandle.IsClosed);
        Assert.False(stringHandle.IsClosed);
        Assert.False(allocatedHandle.IsClosed);
    }

    [Fact]
    public void SafeHandles_WithValidPointer_ShouldNotBeInvalid()
    {
        // Create handles with non-zero pointers
        var validPointer = new IntPtr(0x12345678); // Fake but non-zero pointer
        
        using var dbHandle = new LibSQLDatabaseHandle(validPointer);
        using var connHandle = new LibSQLConnectionHandle(validPointer);
        using var stmtHandle = new LibSQLStatementHandle(validPointer);
        using var rowsHandle = new LibSQLRowsHandle(validPointer);
        using var rowHandle = new LibSQLRowHandle(validPointer);
        using var stringHandle = new LibSQLStringHandle(validPointer);
        using var allocatedHandle = new LibSQLAllocatedPointerHandle(validPointer);
        
        // Should not be invalid with non-zero pointers
        Assert.False(dbHandle.IsInvalid);
        Assert.False(connHandle.IsInvalid);
        Assert.False(stmtHandle.IsInvalid);
        Assert.False(rowsHandle.IsInvalid);
        Assert.False(rowHandle.IsInvalid);
        Assert.False(stringHandle.IsInvalid);
        Assert.False(allocatedHandle.IsInvalid);
    }

    [Fact]
    public void LibSQLAllocatedPointerHandle_ShouldHandleDisposal()
    {
        // Test that LibSQLAllocatedPointerHandle can be created and disposed
        // without throwing (even though it will try to free an invalid pointer)
        
        var handle = new LibSQLAllocatedPointerHandle();
        Assert.True(handle.IsInvalid);
        
        // Disposal should not throw for invalid handles
        handle.Dispose();
        Assert.True(handle.IsClosed);
    }
}