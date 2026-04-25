using System;
using System.Linq;
using Nelknet.LibSQL.Data;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public sealed class SqlParameterLayoutTests
{
    [Fact]
    public void Parse_MixedNumberedAndNamedParameters_RewritesAndResolvesPositions()
    {
        const string sql = "SELECT ?1 || '|' || @b";
        var layout = SqlParameterLayout.Parse(sql);
        var parameters = new LibSQLParameterCollection();
        parameters.AddWithValue("?1", "first");
        parameters.AddWithValue("@b", "second");

        var bindings = layout.ResolveBindings(parameters).ToArray();

        Assert.Equal("SELECT ?1 || '|' || ?2", layout.ToIndexedParameterSql(sql));
        Assert.Equal(2, layout.MaxPosition);
        Assert.Collection(
            bindings,
            binding => Assert.Equal(1, binding.Position),
            binding => Assert.Equal(2, binding.Position));
    }

    [Fact]
    public void Parse_ParameterMarkersInLiteralsAndComments_IgnoresThem()
    {
        const string sql = "SELECT '@a' AS literal, /* :b */ $c -- @d\n";
        var layout = SqlParameterLayout.Parse(sql);
        var parameters = new LibSQLParameterCollection();
        parameters.AddWithValue("$c", 42);

        var bindings = layout.ResolveBindings(parameters).ToArray();

        Assert.Equal("SELECT '@a' AS literal, /* :b */ ?1 -- @d\n", layout.ToIndexedParameterSql(sql));
        Assert.Single(bindings);
        Assert.Equal(1, bindings[0].Position);
    }

    [Fact]
    public void Parse_OverlappingNamedParameters_RewritesOnlyExactMarkers()
    {
        const string sql = "SELECT @id2 || '|' || @id";
        var layout = SqlParameterLayout.Parse(sql);
        var parameters = new LibSQLParameterCollection();
        parameters.AddWithValue("@id", "one");
        parameters.AddWithValue("@id2", "two");

        var bindings = layout.ResolveBindings(parameters).ToArray();

        Assert.Equal("SELECT ?1 || '|' || ?2", layout.ToIndexedParameterSql(sql));
        Assert.Collection(
            bindings,
            binding => Assert.Equal(2, binding.Position),
            binding => Assert.Equal(1, binding.Position));
    }

    [Fact]
    public void ResolveBindings_PurePositionalSql_PreservesCollectionOrder()
    {
        var layout = SqlParameterLayout.Parse("SELECT ? || '|' || ?");
        var parameters = new LibSQLParameterCollection();
        parameters.AddWithValue("@first", "first");
        parameters.AddWithValue("@second", "second");

        var bindings = layout.ResolveBindings(parameters).ToArray();

        Assert.Collection(
            bindings,
            binding => Assert.Equal(1, binding.Position),
            binding => Assert.Equal(2, binding.Position));
    }

    [Fact]
    public void ResolveBindings_MissingSqlParameter_Throws()
    {
        var layout = SqlParameterLayout.Parse("SELECT @a || @b");
        var parameters = new LibSQLParameterCollection();
        parameters.AddWithValue("@a", "AYE");

        var exception = Assert.Throws<InvalidOperationException>(() => layout.ResolveBindings(parameters));

        Assert.Contains("@b", exception.Message, StringComparison.Ordinal);
    }
}
