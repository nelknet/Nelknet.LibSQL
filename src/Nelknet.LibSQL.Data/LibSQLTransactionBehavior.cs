namespace Nelknet.LibSQL.Data;

/// <summary>
/// Specifies the transaction behavior for a libSQL transaction.
/// These behaviors control how locks are acquired, not SQL isolation levels.
/// </summary>
public enum LibSQLTransactionBehavior
{
    /// <summary>
    /// Deferred transaction - locks are not acquired until the first database access.
    /// This is the default behavior and allows multiple deferred transactions to coexist.
    /// </summary>
    Deferred,

    /// <summary>
    /// Immediate transaction - acquires a reserved lock immediately to prevent deadlocks.
    /// Only one immediate or exclusive transaction can be active at a time.
    /// </summary>
    Immediate,

    /// <summary>
    /// Exclusive transaction - acquires an exclusive lock immediately.
    /// No other transactions (read or write) can access the database while this is active.
    /// </summary>
    Exclusive,

    /// <summary>
    /// Read-only transaction - libSQL extension for read-only transactions.
    /// Cannot perform any write operations.
    /// </summary>
    ReadOnly
}