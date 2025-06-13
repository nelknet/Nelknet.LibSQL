#nullable disable warnings

using Nelknet.LibSQL.Data;
using System;
using System.Data;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class LibSQLTransactionTests
{
    [Fact]
    public void Transaction_WithInvalidIsolationLevel_ShouldThrowNotSupported()
    {
        using var connection = new LibSQLConnection();
        
        Assert.Throws<NotSupportedException>(() => connection.BeginTransaction(IsolationLevel.ReadCommitted));
        Assert.Throws<NotSupportedException>(() => connection.BeginTransaction(IsolationLevel.RepeatableRead));
        Assert.Throws<NotSupportedException>(() => connection.BeginTransaction(IsolationLevel.Snapshot));
        Assert.Throws<NotSupportedException>(() => connection.BeginTransaction(IsolationLevel.Chaos));
    }

    [Fact]
    public void TransactionBehavior_EnumValues_ShouldExist()
    {
        // Test that all expected enum values exist
        Assert.True(Enum.IsDefined(typeof(LibSQLTransactionBehavior), LibSQLTransactionBehavior.Deferred));
        Assert.True(Enum.IsDefined(typeof(LibSQLTransactionBehavior), LibSQLTransactionBehavior.Immediate));
        Assert.True(Enum.IsDefined(typeof(LibSQLTransactionBehavior), LibSQLTransactionBehavior.Exclusive));
        Assert.True(Enum.IsDefined(typeof(LibSQLTransactionBehavior), LibSQLTransactionBehavior.ReadOnly));
    }

    [Fact]
    public void BeginTransaction_WithoutConnection_ShouldRequireOpenConnection()
    {
        using var connection = new LibSQLConnection();
        
        // Should throw because connection is not open
        Assert.Throws<InvalidOperationException>(() => connection.BeginTransaction());
    }

    [Fact]
    public void Connection_MultipleBeginTransactionOverloads_ShouldWork()
    {
        using var connection = new LibSQLConnection();
        
        // Test the various overloads (they will fail due to connection not being open, but overloads should exist)
        Assert.Throws<InvalidOperationException>(() => connection.BeginTransaction());
        Assert.Throws<InvalidOperationException>(() => connection.BeginTransaction(IsolationLevel.Serializable));
        Assert.Throws<InvalidOperationException>(() => connection.BeginTransaction(IsolationLevel.Serializable, LibSQLTransactionBehavior.Immediate));
    }

    [Fact]
    public void Transaction_IsolationLevel_UnspecifiedDefaultsToSerializable()
    {
        using var connection = new LibSQLConnection();
        
        // Test that unspecified isolation level defaults to serializable
        // This will fail to begin the transaction due to no open connection, but should not throw NotSupportedException
        Assert.Throws<InvalidOperationException>(() => connection.BeginTransaction(IsolationLevel.Unspecified));
    }

    [Fact]
    public void Transaction_ValidIsolationLevels_ShouldNotThrowNotSupported()
    {
        using var connection = new LibSQLConnection();
        
        // Test that valid isolation levels don't throw NotSupportedException 
        // (they will throw InvalidOperationException due to connection not being open)
        Assert.Throws<InvalidOperationException>(() => connection.BeginTransaction(IsolationLevel.Serializable));
        Assert.Throws<InvalidOperationException>(() => connection.BeginTransaction(IsolationLevel.ReadUncommitted));
    }

    [Fact]
    public void Connection_Close_ShouldNotThrow()
    {
        using var connection = new LibSQLConnection();
        
        // Closing an already closed connection should not throw
        connection.Close();
        connection.Close(); // Second close should also not throw
    }
}