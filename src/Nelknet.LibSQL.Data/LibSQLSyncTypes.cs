using System;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Represents the result of a sync operation.
/// </summary>
public class LibSQLSyncResult
{
    /// <summary>
    /// Gets or sets the current frame number after sync.
    /// </summary>
    public int FrameNo { get; set; }
    
    /// <summary>
    /// Gets or sets the number of frames that were synced.
    /// </summary>
    public int FramesSynced { get; set; }
    
    /// <summary>
    /// Gets or sets the duration of the sync operation.
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Provides data for the SyncCompleted event.
/// </summary>
public class LibSQLSyncCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the sync result.
    /// </summary>
    public LibSQLSyncResult Result { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLSyncCompletedEventArgs"/> class.
    /// </summary>
    /// <param name="result">The sync result.</param>
    public LibSQLSyncCompletedEventArgs(LibSQLSyncResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        Result = result;
    }
}

/// <summary>
/// Provides data for the SyncFailed event.
/// </summary>
public class LibSQLSyncFailedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the exception that caused the sync to fail.
    /// </summary>
    public Exception Exception { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLSyncFailedEventArgs"/> class.
    /// </summary>
    /// <param name="exception">The exception that caused the sync to fail.</param>
    public LibSQLSyncFailedEventArgs(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Exception = exception;
    }
}