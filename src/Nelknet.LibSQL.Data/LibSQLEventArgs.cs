using System;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Provides data for the Progress event.
/// </summary>
public class LibSQLProgressEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLProgressEventArgs"/> class.
    /// </summary>
    /// <param name="current">The current progress value.</param>
    /// <param name="total">The total progress value.</param>
    /// <param name="message">An optional progress message.</param>
    public LibSQLProgressEventArgs(int current, int total, string? message = null)
    {
        Current = current;
        Total = total;
        Message = message;
    }
    
    /// <summary>
    /// Gets the current progress value.
    /// </summary>
    public int Current { get; }
    
    /// <summary>
    /// Gets the total progress value.
    /// </summary>
    public int Total { get; }
    
    /// <summary>
    /// Gets the progress as a percentage (0-100).
    /// </summary>
    public double PercentComplete => Total > 0 ? (Current * 100.0 / Total) : 0;
    
    /// <summary>
    /// Gets an optional progress message.
    /// </summary>
    public string? Message { get; }
}

/// <summary>
/// Provides data for command execution events.
/// </summary>
public class LibSQLCommandEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLCommandEventArgs"/> class.
    /// </summary>
    /// <param name="commandText">The command text being executed.</param>
    /// <param name="duration">The execution duration (only for CommandExecuted event).</param>
    public LibSQLCommandEventArgs(string commandText, TimeSpan? duration = null)
    {
        CommandText = commandText;
        Duration = duration;
    }
    
    /// <summary>
    /// Gets the command text.
    /// </summary>
    public string CommandText { get; }
    
    /// <summary>
    /// Gets the execution duration (only available for CommandExecuted event).
    /// </summary>
    public TimeSpan? Duration { get; }
    
    /// <summary>
    /// Gets or sets whether to cancel the command execution (only for CommandExecuting event).
    /// </summary>
    public bool Cancel { get; set; }
}