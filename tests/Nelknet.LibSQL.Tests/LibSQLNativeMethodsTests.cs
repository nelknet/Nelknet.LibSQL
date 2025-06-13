using Nelknet.LibSQL.Bindings;
using Nelknet.LibSQL.Native;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class LibSQLNativeMethodsTests
{
    [Fact]
    public void DatabaseMethods_ShouldHaveCorrectSignatures()
    {
        // Test that the database management methods have proper signatures
        // This verifies the P/Invoke declarations are syntactically correct
        
        // These should not throw during reflection
        var openExtMethod = typeof(LibSQLNative).GetMethod("libsql_open_ext", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var openFileMethod = typeof(LibSQLNative).GetMethod("libsql_open_file",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var closeMethod = typeof(LibSQLNative).GetMethod("libsql_close",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(openExtMethod);
        Assert.NotNull(openFileMethod);
        Assert.NotNull(closeMethod);
        
        // Verify return types
        Assert.Equal(typeof(int), openExtMethod.ReturnType);
        Assert.Equal(typeof(int), openFileMethod.ReturnType);
        Assert.Equal(typeof(void), closeMethod.ReturnType);
    }
    
    [Fact]
    public void ConnectionMethods_ShouldHaveCorrectSignatures()
    {
        // Test connection management method signatures
        var connectMethod = typeof(LibSQLNative).GetMethod("libsql_connect",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var disconnectMethod = typeof(LibSQLNative).GetMethod("libsql_disconnect",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var resetMethod = typeof(LibSQLNative).GetMethod("libsql_reset",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(connectMethod);
        Assert.NotNull(disconnectMethod);
        Assert.NotNull(resetMethod);
        
        // Verify return types
        Assert.Equal(typeof(int), connectMethod.ReturnType);
        Assert.Equal(typeof(void), disconnectMethod.ReturnType);
        Assert.Equal(typeof(int), resetMethod.ReturnType);
    }
    
    [Fact]
    public void StatementMethods_ShouldHaveCorrectSignatures()
    {
        // Test statement execution method signatures
        var prepareMethod = typeof(LibSQLNative).GetMethod("libsql_prepare",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var executeMethod = typeof(LibSQLNative).GetMethod("libsql_execute",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var queryMethod = typeof(LibSQLNative).GetMethod("libsql_query",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var executeStmtMethod = typeof(LibSQLNative).GetMethod("libsql_execute_stmt",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(prepareMethod);
        Assert.NotNull(executeMethod);
        Assert.NotNull(queryMethod);
        Assert.NotNull(executeStmtMethod);
        
        // All should return int (result code)
        Assert.Equal(typeof(int), prepareMethod.ReturnType);
        Assert.Equal(typeof(int), executeMethod.ReturnType);
        Assert.Equal(typeof(int), queryMethod.ReturnType);
        Assert.Equal(typeof(int), executeStmtMethod.ReturnType);
    }
    
    [Fact]
    public void ParameterBindingMethods_ShouldHaveCorrectSignatures()
    {
        // Test parameter binding method signatures
        var bindIntMethod = typeof(LibSQLNative).GetMethod("libsql_bind_int",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var bindFloatMethod = typeof(LibSQLNative).GetMethod("libsql_bind_float",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var bindStringMethod = typeof(LibSQLNative).GetMethod("libsql_bind_string",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var bindBlobMethod = typeof(LibSQLNative).GetMethod("libsql_bind_blob",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var bindNullMethod = typeof(LibSQLNative).GetMethod("libsql_bind_null",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(bindIntMethod);
        Assert.NotNull(bindFloatMethod);
        Assert.NotNull(bindStringMethod);
        Assert.NotNull(bindBlobMethod);
        Assert.NotNull(bindNullMethod);
        
        // All should return int (result code)
        Assert.Equal(typeof(int), bindIntMethod.ReturnType);
        Assert.Equal(typeof(int), bindFloatMethod.ReturnType);
        Assert.Equal(typeof(int), bindStringMethod.ReturnType);
        Assert.Equal(typeof(int), bindBlobMethod.ReturnType);
        Assert.Equal(typeof(int), bindNullMethod.ReturnType);
    }
    
    [Fact]
    public void ResultProcessingMethods_ShouldHaveCorrectSignatures()
    {
        // Test result processing method signatures
        var columnCountMethod = typeof(LibSQLNative).GetMethod("libsql_column_count",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var columnNameMethod = typeof(LibSQLNative).GetMethod("libsql_column_name",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var nextRowMethod = typeof(LibSQLNative).GetMethod("libsql_next_row",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var getStringMethod = typeof(LibSQLNative).GetMethod("libsql_get_string",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var getIntMethod = typeof(LibSQLNative).GetMethod("libsql_get_int",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var getFloatMethod = typeof(LibSQLNative).GetMethod("libsql_get_float",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var getBlobMethod = typeof(LibSQLNative).GetMethod("libsql_get_blob",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(columnCountMethod);
        Assert.NotNull(columnNameMethod);
        Assert.NotNull(nextRowMethod);
        Assert.NotNull(getStringMethod);
        Assert.NotNull(getIntMethod);
        Assert.NotNull(getFloatMethod);
        Assert.NotNull(getBlobMethod);
        
        // Verify return types
        Assert.Equal(typeof(int), columnCountMethod.ReturnType);
        Assert.Equal(typeof(int), columnNameMethod.ReturnType);
        Assert.Equal(typeof(int), nextRowMethod.ReturnType);
        Assert.Equal(typeof(int), getStringMethod.ReturnType);
        Assert.Equal(typeof(int), getIntMethod.ReturnType);
        Assert.Equal(typeof(int), getFloatMethod.ReturnType);
        Assert.Equal(typeof(int), getBlobMethod.ReturnType);
    }
    
    [Fact]
    public void UtilityMethods_ShouldHaveCorrectSignatures()
    {
        // Test utility method signatures
        var changesMethod = typeof(LibSQLNative).GetMethod("libsql_changes",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var lastInsertRowidMethod = typeof(LibSQLNative).GetMethod("libsql_last_insert_rowid",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var loadExtensionMethod = typeof(LibSQLNative).GetMethod("libsql_load_extension",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(changesMethod);
        Assert.NotNull(lastInsertRowidMethod);
        Assert.NotNull(loadExtensionMethod);
        
        // Verify return types
        Assert.Equal(typeof(ulong), changesMethod.ReturnType);
        Assert.Equal(typeof(long), lastInsertRowidMethod.ReturnType);
        Assert.Equal(typeof(int), loadExtensionMethod.ReturnType);
    }
    
    [Fact]
    public void ErrorHandlingMethods_ShouldHaveCorrectSignatures()
    {
        // Test error handling method signatures
        var freeErrorMsgMethod = typeof(LibSQLNative).GetMethod("libsql_free_error_msg",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(freeErrorMsgMethod);
        Assert.Equal(typeof(void), freeErrorMsgMethod.ReturnType);
    }
    
    [Fact]
    public void TransactionMethods_ShouldHaveCorrectSignatures()
    {
        // Test transaction control method signatures
        var beginMethod = typeof(LibSQLNative).GetMethod("libsql_begin_transaction",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var commitMethod = typeof(LibSQLNative).GetMethod("libsql_commit_transaction",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var rollbackMethod = typeof(LibSQLNative).GetMethod("libsql_rollback_transaction",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(beginMethod);
        Assert.NotNull(commitMethod);
        Assert.NotNull(rollbackMethod);
        
        // All should return int (result code)
        Assert.Equal(typeof(int), beginMethod.ReturnType);
        Assert.Equal(typeof(int), commitMethod.ReturnType);
        Assert.Equal(typeof(int), rollbackMethod.ReturnType);
    }
    
    [Fact]
    public void SyncMethods_ShouldHaveCorrectSignatures()
    {
        // Test sync/replication method signatures
        var syncMethod = typeof(LibSQLNative).GetMethod("libsql_sync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var sync2Method = typeof(LibSQLNative).GetMethod("libsql_sync2",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(syncMethod);
        Assert.NotNull(sync2Method);
        
        // Both should return int (result code)
        Assert.Equal(typeof(int), syncMethod.ReturnType);
        Assert.Equal(typeof(int), sync2Method.ReturnType);
    }
    
    [Fact]
    public void TracingMethods_ShouldHaveCorrectSignatures()
    {
        // Test tracing method signatures
        var enableTracingMethod = typeof(LibSQLNative).GetMethod("libsql_enable_internal_tracing",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(enableTracingMethod);
        Assert.Equal(typeof(int), enableTracingMethod.ReturnType);
    }
    
    [Fact]
    public void MemoryManagementMethods_ShouldHaveCorrectSignatures()
    {
        // Test memory management method signatures
        var freeRowsMethod = typeof(LibSQLNative).GetMethod("libsql_free_rows",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var freeRowMethod = typeof(LibSQLNative).GetMethod("libsql_free_row",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var freeStringMethod = typeof(LibSQLNative).GetMethod("libsql_free_string",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var freeBlobMethod = typeof(LibSQLNative).GetMethod("libsql_free_blob",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var freeStmtMethod = typeof(LibSQLNative).GetMethod("libsql_free_stmt",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(freeRowsMethod);
        Assert.NotNull(freeRowMethod);
        Assert.NotNull(freeStringMethod);
        Assert.NotNull(freeBlobMethod);
        Assert.NotNull(freeStmtMethod);
        
        // All memory management methods should return void
        Assert.Equal(typeof(void), freeRowsMethod.ReturnType);
        Assert.Equal(typeof(void), freeRowMethod.ReturnType);
        Assert.Equal(typeof(void), freeStringMethod.ReturnType);
        Assert.Equal(typeof(void), freeBlobMethod.ReturnType);
        Assert.Equal(typeof(void), freeStmtMethod.ReturnType);
    }
    
    [Fact]
    public void AllNativeMethods_ShouldHaveLibraryImportAttribute()
    {
        // Verify that all native methods have the LibraryImport attribute
        var nativeType = typeof(LibSQLNative);
        var methods = nativeType.GetMethods(System.Reflection.BindingFlags.NonPublic | 
                                           System.Reflection.BindingFlags.Static);
        
        var nativeMethods = 0;
        foreach (var method in methods)
        {
            // Skip non-native methods like Initialize()
            if (method.Name.StartsWith("libsql_") && method.Name != "Initialize")
            {
                nativeMethods++;
                var attribute = method.GetCustomAttribute<LibraryImportAttribute>();
                Assert.NotNull(attribute);
                Assert.Equal(LibSQLNativeLibrary.LibraryName, attribute.LibraryName);
            }
        }
        
        // We should have found a reasonable number of native methods
        Assert.True(nativeMethods >= 30, $"Expected at least 30 native methods, found {nativeMethods}");
    }
    
    [Fact]
    public void StringMarshallingMethods_ShouldHaveCorrectAttribute()
    {
        // Test that methods dealing with strings have proper string marshalling
        var methodsWithStrings = new[]
        {
            "libsql_open_ext",
            "libsql_open_file", 
            "libsql_open_remote",
            "libsql_prepare",
            "libsql_execute",
            "libsql_query",
            "libsql_bind_string",
            "libsql_load_extension",
            "libsql_begin_transaction",
            "libsql_commit_transaction",
            "libsql_rollback_transaction"
        };
        
        var nativeType = typeof(LibSQLNative);
        
        foreach (var methodName in methodsWithStrings)
        {
            var method = nativeType.GetMethod(methodName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);
            
            var attribute = method.GetCustomAttribute<LibraryImportAttribute>();
            Assert.NotNull(attribute);
            Assert.Equal(StringMarshalling.Utf8, attribute.StringMarshalling);
        }
    }
    
    [Fact]
    public void Initialize_ShouldCallNativeLibraryInitialization()
    {
        // Test that the Initialize method properly calls the library initialization
        // This verifies the connection between LibSQLNative and LibSQLNativeLibrary
        
        // The Initialize method should either succeed or throw InvalidOperationException
        try
        {
            LibSQLNative.Initialize();
            // If it succeeds, that's fine - the library was available
        }
        catch (InvalidOperationException ex)
        {
            // If it fails, it should be due to library not being available
            Assert.Contains("Failed to load libSQL native library", ex.Message);
            Assert.Contains("Please ensure the appropriate native library is available", ex.Message);
        }
        catch (Exception ex)
        {
            // Any other exception type is unexpected
            Assert.Fail($"Unexpected exception type: {ex.GetType()}, Message: {ex.Message}");
        }
    }
}