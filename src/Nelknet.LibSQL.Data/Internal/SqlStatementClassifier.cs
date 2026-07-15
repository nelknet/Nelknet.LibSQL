using System;

namespace Nelknet.LibSQL.Data.Internal;

internal static class SqlStatementClassifier
{
    internal static bool RequiresDrainOnReaderClose(string sql)
    {
        var hasDataModification = false;
        var hasReturningClause = false;
        var isFirstToken = true;
        var index = 0;

        while (index < sql.Length)
        {
            var current = sql[index];

            if (current == ';')
                break;

            if (current == '-' && index + 1 < sql.Length && sql[index + 1] == '-')
            {
                var newline = sql.IndexOf('\n', index + 2);
                index = newline < 0 ? sql.Length : newline + 1;
                continue;
            }

            if (current == '/' && index + 1 < sql.Length && sql[index + 1] == '*')
            {
                var commentEnd = sql.IndexOf("*/", index + 2, StringComparison.Ordinal);
                index = commentEnd < 0 ? sql.Length : commentEnd + 2;
                continue;
            }

            if (current is '\'' or '"' or '`')
            {
                index = SkipQuoted(sql, index, current);
                continue;
            }

            if (current == '[')
            {
                var identifierEnd = sql.IndexOf(']', index + 1);
                index = identifierEnd < 0 ? sql.Length : identifierEnd + 1;
                continue;
            }

            if (!char.IsLetter(current))
            {
                index++;
                continue;
            }

            var tokenStart = index++;
            while (index < sql.Length && (char.IsLetterOrDigit(sql[index]) || sql[index] == '_'))
            {
                index++;
            }

            var token = sql.AsSpan(tokenStart, index - tokenStart);
            if (isFirstToken && token.Equals("EXPLAIN", StringComparison.OrdinalIgnoreCase))
                return false;

            isFirstToken = false;
            hasDataModification |= IsDataModification(token);
            hasReturningClause |= token.Equals("RETURNING", StringComparison.OrdinalIgnoreCase);

            if (hasDataModification && hasReturningClause)
                return true;
        }

        return false;
    }

    private static bool IsDataModification(ReadOnlySpan<char> token)
    {
        return token.Equals("INSERT", StringComparison.OrdinalIgnoreCase)
            || token.Equals("UPDATE", StringComparison.OrdinalIgnoreCase)
            || token.Equals("DELETE", StringComparison.OrdinalIgnoreCase)
            || token.Equals("REPLACE", StringComparison.OrdinalIgnoreCase);
    }

    private static int SkipQuoted(string sql, int start, char quote)
    {
        var index = start + 1;
        while (index < sql.Length)
        {
            if (sql[index] != quote)
            {
                index++;
                continue;
            }

            if (index + 1 < sql.Length && sql[index + 1] == quote)
            {
                index += 2;
                continue;
            }

            return index + 1;
        }

        return sql.Length;
    }
}
