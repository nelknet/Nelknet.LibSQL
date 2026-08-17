namespace Nelknet.LibSQL.Data.Http;

internal static class SqlStatementScanner
{
    internal static bool ContainsMultipleStatements(ReadOnlySpan<char> sql)
    {
        if (sql.IndexOf(';') < 0)
        {
            return false;
        }

        var state = ScannerState.Sql;
        var hasCompletedStatement = false;
        var hasCurrentStatementContent = false;

        for (var index = 0; index < sql.Length; index++)
        {
            var character = sql[index];

            switch (state)
            {
                case ScannerState.Sql:
                    switch (character)
                    {
                        case ';':
                            if (hasCurrentStatementContent)
                            {
                                hasCompletedStatement = true;
                                hasCurrentStatementContent = false;
                            }
                            break;

                        case '\'':
                            if (hasCompletedStatement)
                            {
                                return true;
                            }
                            hasCurrentStatementContent = true;
                            state = ScannerState.SingleQuoted;
                            break;

                        case '"':
                            if (hasCompletedStatement)
                            {
                                return true;
                            }
                            hasCurrentStatementContent = true;
                            state = ScannerState.DoubleQuoted;
                            break;

                        case '`':
                            if (hasCompletedStatement)
                            {
                                return true;
                            }
                            hasCurrentStatementContent = true;
                            state = ScannerState.BacktickQuoted;
                            break;

                        case '[':
                            if (hasCompletedStatement)
                            {
                                return true;
                            }
                            hasCurrentStatementContent = true;
                            state = ScannerState.BracketQuoted;
                            break;

                        case '-' when Peek(sql, index) == '-':
                            state = ScannerState.LineComment;
                            index++;
                            break;

                        case '/' when Peek(sql, index) == '*':
                            state = ScannerState.BlockComment;
                            index++;
                            break;

                        default:
                            if (!char.IsWhiteSpace(character))
                            {
                                if (hasCompletedStatement)
                                {
                                    return true;
                                }
                                hasCurrentStatementContent = true;
                            }
                            break;
                    }
                    break;

                case ScannerState.SingleQuoted:
                    if (character == '\'')
                    {
                        if (Peek(sql, index) == '\'')
                        {
                            index++;
                        }
                        else
                        {
                            state = ScannerState.Sql;
                        }
                    }
                    break;

                case ScannerState.DoubleQuoted:
                    if (character == '"')
                    {
                        if (Peek(sql, index) == '"')
                        {
                            index++;
                        }
                        else
                        {
                            state = ScannerState.Sql;
                        }
                    }
                    break;

                case ScannerState.BacktickQuoted:
                    if (character == '`')
                    {
                        if (Peek(sql, index) == '`')
                        {
                            index++;
                        }
                        else
                        {
                            state = ScannerState.Sql;
                        }
                    }
                    break;

                case ScannerState.BracketQuoted:
                    if (character == ']')
                    {
                        state = ScannerState.Sql;
                    }
                    break;

                case ScannerState.LineComment:
                    if (character is '\r' or '\n')
                    {
                        state = ScannerState.Sql;
                    }
                    break;

                case ScannerState.BlockComment:
                    if (character == '*' && Peek(sql, index) == '/')
                    {
                        state = ScannerState.Sql;
                        index++;
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unknown SQL scanner state: {state}.");
            }
        }

        return false;
    }

    private static char Peek(ReadOnlySpan<char> sql, int index)
        => index + 1 < sql.Length ? sql[index + 1] : '\0';

    private enum ScannerState
    {
        Sql,
        SingleQuoted,
        DoubleQuoted,
        BacktickQuoted,
        BracketQuoted,
        LineComment,
        BlockComment
    }
}
