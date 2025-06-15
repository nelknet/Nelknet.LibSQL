namespace Nelknet.LibSQL.Data;

/// <summary>
/// Specifies the verbosity level for EXPLAIN commands.
/// </summary>
public enum ExplainVerbosity
{
    /// <summary>
    /// Normal EXPLAIN output.
    /// </summary>
    Normal = 0,
    
    /// <summary>
    /// EXPLAIN QUERY PLAN output.
    /// </summary>
    QueryPlan = 1,
    
    /// <summary>
    /// Detailed EXPLAIN output showing opcodes.
    /// </summary>
    Detailed = 2
}