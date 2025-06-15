using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Nelknet.LibSQL.Bindings;
using Nelknet.LibSQL.Data.Exceptions;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Base class for implementing custom SQL scalar functions.
/// </summary>
public abstract class LibSQLFunction
{
    /// <summary>
    /// Gets the name of the function as it will appear in SQL.
    /// </summary>
    public abstract string Name { get; }
    
    /// <summary>
    /// Gets the number of arguments the function accepts.
    /// Use -1 for variable number of arguments.
    /// </summary>
    public virtual int ArgumentCount => -1;
    
    /// <summary>
    /// Gets whether the function is deterministic.
    /// Deterministic functions always return the same result for the same input.
    /// </summary>
    public virtual bool IsDeterministic => false;
    
    /// <summary>
    /// Gets whether the function should only be usable directly in SQL statements.
    /// DirectOnly functions cannot be used in views, triggers, or check constraints.
    /// </summary>
    public virtual bool IsDirectOnly => false;
    
    /// <summary>
    /// Executes the scalar function.
    /// </summary>
    /// <param name="args">The function arguments</param>
    /// <returns>The function result</returns>
    public abstract object? Invoke(object?[] args);
}

/// <summary>
/// Base class for implementing custom SQL aggregate functions.
/// </summary>
public abstract class LibSQLAggregate
{
    /// <summary>
    /// Gets the name of the aggregate function as it will appear in SQL.
    /// </summary>
    public abstract string Name { get; }
    
    /// <summary>
    /// Gets the number of arguments the aggregate accepts.
    /// Use -1 for variable number of arguments.
    /// </summary>
    public virtual int ArgumentCount => -1;
    
    /// <summary>
    /// Called for each row in the aggregation.
    /// </summary>
    /// <param name="args">The arguments for this row</param>
    public abstract void Step(object?[] args);
    
    /// <summary>
    /// Called after all rows have been processed to get the final result.
    /// </summary>
    /// <returns>The aggregate result</returns>
    public abstract object? Final();
    
    /// <summary>
    /// Resets the aggregate to its initial state.
    /// Called before starting a new aggregation.
    /// </summary>
    public virtual void Reset() { }
}

/// <summary>
/// Manages custom functions and aggregates for a connection.
/// </summary>
internal class LibSQLFunctionManager : IDisposable
{
    private readonly Dictionary<string, FunctionRegistration> _functions = new();
    private readonly Dictionary<string, AggregateRegistration> _aggregates = new();
    private readonly object _lock = new();
    private bool _disposed;
    
    private class FunctionRegistration
    {
        public LibSQLFunction Function { get; set; } = null!;
        public GCHandle Handle { get; set; }
        public IntPtr CallbackPtr { get; set; }
    }
    
    private class AggregateRegistration
    {
        public Type AggregateType { get; set; } = null!;
        public GCHandle StepHandle { get; set; }
        public GCHandle FinalHandle { get; set; }
        public IntPtr StepCallbackPtr { get; set; }
        public IntPtr FinalCallbackPtr { get; set; }
    }
    
    // Delegate types for callbacks
    private delegate void ScalarFunctionCallback(IntPtr context, int argc, IntPtr argv);
    private delegate void AggregateStepCallback(IntPtr context, int argc, IntPtr argv);
    private delegate void AggregateFinalCallback(IntPtr context);
    
    /// <summary>
    /// Registers a custom scalar function.
    /// </summary>
    public void RegisterFunction(IntPtr db, LibSQLFunction function)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        lock (_lock)
        {
            UnregisterFunction(db, function.Name);
            
            var callback = new ScalarFunctionCallback((context, argc, argv) => ExecuteFunction(function, context, argc, argv));
            var handle = GCHandle.Alloc(callback);
            var callbackPtr = Marshal.GetFunctionPointerForDelegate(callback);
            
            var flags = LibSQLNative.SQLITE_UTF8;
            if (function.IsDeterministic)
                flags |= LibSQLNative.SQLITE_DETERMINISTIC;
            if (function.IsDirectOnly)
                flags |= LibSQLNative.SQLITE_DIRECTONLY;
            
            var result = LibSQLNative.sqlite3_create_function_v2(
                db,
                function.Name,
                function.ArgumentCount,
                flags,
                IntPtr.Zero,
                callbackPtr,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero
            );
            
            if (result != 0)
            {
                handle.Free();
                throw LibSQLException.FromErrorCode(result, $"Failed to register function '{function.Name}'");
            }
            
            _functions[function.Name] = new FunctionRegistration
            {
                Function = function,
                Handle = handle,
                CallbackPtr = callbackPtr
            };
        }
    }
    
    /// <summary>
    /// Registers a custom aggregate function.
    /// </summary>
    public void RegisterAggregate<TAggregate>(IntPtr db) where TAggregate : LibSQLAggregate, new()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        var sample = new TAggregate();
        var name = sample.Name;
        var argCount = sample.ArgumentCount;
        
        lock (_lock)
        {
            UnregisterAggregate(db, name);
            
            var stepCallback = new AggregateStepCallback((context, argc, argv) => ExecuteAggregateStep<TAggregate>(context, argc, argv));
            var finalCallback = new AggregateFinalCallback(context => ExecuteAggregateFinal<TAggregate>(context));
            
            var stepHandle = GCHandle.Alloc(stepCallback);
            var finalHandle = GCHandle.Alloc(finalCallback);
            
            var stepPtr = Marshal.GetFunctionPointerForDelegate(stepCallback);
            var finalPtr = Marshal.GetFunctionPointerForDelegate(finalCallback);
            
            var result = LibSQLNative.sqlite3_create_function_v2(
                db,
                name,
                argCount,
                LibSQLNative.SQLITE_UTF8,
                IntPtr.Zero,
                IntPtr.Zero,
                stepPtr,
                finalPtr,
                IntPtr.Zero
            );
            
            if (result != 0)
            {
                stepHandle.Free();
                finalHandle.Free();
                throw LibSQLException.FromErrorCode(result, $"Failed to register aggregate '{name}'");
            }
            
            _aggregates[name] = new AggregateRegistration
            {
                AggregateType = typeof(TAggregate),
                StepHandle = stepHandle,
                FinalHandle = finalHandle,
                StepCallbackPtr = stepPtr,
                FinalCallbackPtr = finalPtr
            };
        }
    }
    
    /// <summary>
    /// Unregisters a custom function.
    /// </summary>
    public void UnregisterFunction(IntPtr db, string name)
    {
        lock (_lock)
        {
            if (_functions.TryGetValue(name, out var registration))
            {
                LibSQLNative.sqlite3_create_function_v2(
                    db, name, registration.Function.ArgumentCount,
                    LibSQLNative.SQLITE_UTF8, IntPtr.Zero,
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                
                registration.Handle.Free();
                _functions.Remove(name);
            }
        }
    }
    
    /// <summary>
    /// Unregisters a custom aggregate.
    /// </summary>
    public void UnregisterAggregate(IntPtr db, string name)
    {
        lock (_lock)
        {
            if (_aggregates.TryGetValue(name, out var registration))
            {
                var sample = Activator.CreateInstance(registration.AggregateType) as LibSQLAggregate;
                LibSQLNative.sqlite3_create_function_v2(
                    db, name, sample!.ArgumentCount,
                    LibSQLNative.SQLITE_UTF8, IntPtr.Zero,
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                
                registration.StepHandle.Free();
                registration.FinalHandle.Free();
                _aggregates.Remove(name);
            }
        }
    }
    
    /// <summary>
    /// Unregisters all functions and aggregates.
    /// </summary>
    public void Clear(IntPtr db)
    {
        lock (_lock)
        {
            foreach (var name in _functions.Keys.ToArray())
            {
                UnregisterFunction(db, name);
            }
            
            foreach (var name in _aggregates.Keys.ToArray())
            {
                UnregisterAggregate(db, name);
            }
        }
    }
    
    private static void ExecuteFunction(LibSQLFunction function, IntPtr context, int argc, IntPtr argv)
    {
        try
        {
            var args = new object?[argc];
            var argPtrs = new IntPtr[argc];
            Marshal.Copy(argv, argPtrs, 0, argc);
            
            for (int i = 0; i < argc; i++)
            {
                args[i] = GetValue(argPtrs[i]);
            }
            
            var result = function.Invoke(args);
            SetResult(context, result);
        }
        catch (Exception ex)
        {
            var errorMsg = ex.Message ?? "Unknown error in custom function";
            LibSQLNative.sqlite3_result_error(context, errorMsg, -1);
        }
    }
    
    private static void ExecuteAggregateStep<TAggregate>(IntPtr context, int argc, IntPtr argv)
        where TAggregate : LibSQLAggregate, new()
    {
        try
        {
            var aggregatePtr = LibSQLNative.sqlite3_aggregate_context(context, IntPtr.Size);
            if (aggregatePtr == IntPtr.Zero)
                return;
            
            var handlePtr = Marshal.ReadIntPtr(aggregatePtr);
            LibSQLAggregate aggregate;
            
            if (handlePtr == IntPtr.Zero)
            {
                aggregate = new TAggregate();
                aggregate.Reset();
                var handle = GCHandle.Alloc(aggregate);
                Marshal.WriteIntPtr(aggregatePtr, GCHandle.ToIntPtr(handle));
            }
            else
            {
                var handle = GCHandle.FromIntPtr(handlePtr);
                aggregate = (LibSQLAggregate)handle.Target!;
            }
            
            var args = new object?[argc];
            var argPtrs = new IntPtr[argc];
            Marshal.Copy(argv, argPtrs, 0, argc);
            
            for (int i = 0; i < argc; i++)
            {
                args[i] = GetValue(argPtrs[i]);
            }
            
            aggregate.Step(args);
        }
        catch (Exception ex)
        {
            var errorMsg = ex.Message ?? "Unknown error in aggregate step";
            LibSQLNative.sqlite3_result_error(context, errorMsg, -1);
        }
    }
    
    private static void ExecuteAggregateFinal<TAggregate>(IntPtr context)
        where TAggregate : LibSQLAggregate, new()
    {
        try
        {
            var aggregatePtr = LibSQLNative.sqlite3_aggregate_context(context, 0);
            if (aggregatePtr == IntPtr.Zero)
            {
                SetResult(context, null);
                return;
            }
            
            var handlePtr = Marshal.ReadIntPtr(aggregatePtr);
            if (handlePtr == IntPtr.Zero)
            {
                var aggregate = new TAggregate();
                aggregate.Reset();
                var result = aggregate.Final();
                SetResult(context, result);
            }
            else
            {
                var handle = GCHandle.FromIntPtr(handlePtr);
                var aggregate = (LibSQLAggregate)handle.Target!;
                var result = aggregate.Final();
                SetResult(context, result);
                handle.Free();
            }
        }
        catch (Exception ex)
        {
            var errorMsg = ex.Message ?? "Unknown error in aggregate final";
            LibSQLNative.sqlite3_result_error(context, errorMsg, -1);
        }
    }
    
    private static object? GetValue(IntPtr valuePtr)
    {
        var type = LibSQLNative.sqlite3_value_type(valuePtr);
        
        return type switch
        {
            1 => LibSQLNative.sqlite3_value_int64(valuePtr), // SQLITE_INTEGER
            2 => LibSQLNative.sqlite3_value_double(valuePtr), // SQLITE_FLOAT
            3 => Marshal.PtrToStringUTF8(LibSQLNative.sqlite3_value_text(valuePtr)), // SQLITE_TEXT
            4 => GetBlob(valuePtr), // SQLITE_BLOB
            5 => null, // SQLITE_NULL
            _ => null
        };
    }
    
    private static byte[] GetBlob(IntPtr valuePtr)
    {
        var blobPtr = LibSQLNative.sqlite3_value_blob(valuePtr);
        var size = LibSQLNative.sqlite3_value_bytes(valuePtr);
        
        if (blobPtr == IntPtr.Zero || size <= 0)
            return Array.Empty<byte>();
        
        var result = new byte[size];
        Marshal.Copy(blobPtr, result, 0, size);
        return result;
    }
    
    private static void SetResult(IntPtr context, object? value)
    {
        switch (value)
        {
            case null:
                LibSQLNative.sqlite3_result_null(context);
                break;
            case long l:
                LibSQLNative.sqlite3_result_int64(context, l);
                break;
            case int i:
                LibSQLNative.sqlite3_result_int64(context, i);
                break;
            case double d:
                LibSQLNative.sqlite3_result_double(context, d);
                break;
            case float f:
                LibSQLNative.sqlite3_result_double(context, f);
                break;
            case string s:
                var bytes = Encoding.UTF8.GetBytes(s);
                var ptr = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
                LibSQLNative.sqlite3_result_text(context, s, bytes.Length, ptr);
                break;
            case byte[] b:
                var blobPtr = Marshal.AllocHGlobal(b.Length);
                Marshal.Copy(b, 0, blobPtr, b.Length);
                LibSQLNative.sqlite3_result_blob(context, blobPtr, b.Length, blobPtr);
                break;
            case bool bl:
                LibSQLNative.sqlite3_result_int64(context, bl ? 1 : 0);
                break;
            default:
                LibSQLNative.sqlite3_result_text(context, value.ToString() ?? string.Empty, -1, LibSQLNative.SQLITE_TRANSIENT);
                break;
        }
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
        
        lock (_lock)
        {
            foreach (var registration in _functions.Values)
            {
                registration.Handle.Free();
            }
            _functions.Clear();
            
            foreach (var registration in _aggregates.Values)
            {
                registration.StepHandle.Free();
                registration.FinalHandle.Free();
            }
            _aggregates.Clear();
            
            _disposed = true;
        }
    }
}