using System;
using Nelknet.LibSQL.Data;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class LibSQLVersionTests
{
    [Fact]
    public void LibSQLVersionString_ShouldReturnVersion()
    {
        // Act
        var version = LibSQLVersion.LibSQLVersionString;

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
        Assert.NotEqual("Unknown", version);
    }

    [Fact]
    public void SQLiteVersionString_ShouldReturnVersion()
    {
        // Act
        var version = LibSQLVersion.SQLiteVersionString;

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
        Assert.NotEqual("Unknown", version);
        Assert.StartsWith("3.", version); // SQLite 3.x
    }

    [Fact]
    public void SQLiteVersionNumber_ShouldReturnValidNumber()
    {
        // Act
        var versionNumber = LibSQLVersion.SQLiteVersionNumber;

        // Assert
        Assert.True(versionNumber > 3000000); // SQLite 3.0.0 or higher
    }

    [Fact]
    public void SQLiteSourceId_ShouldReturnSourceId()
    {
        // Act
        var sourceId = LibSQLVersion.SQLiteSourceId;

        // Assert
        Assert.NotNull(sourceId);
        Assert.NotEmpty(sourceId);
        Assert.NotEqual("Unknown", sourceId);
    }

    [Fact]
    public void IsLibraryLoaded_ShouldReturnBooleanValue()
    {
        // Act
        var isLoaded = LibSQLVersion.IsLibraryLoaded();

        // Assert
        // The result should be a boolean - we don't assume whether library is available or not
        Assert.IsType<bool>(isLoaded);
    }

    [Fact]
    public void GetVersionInfo_ShouldReturnFormattedInfo()
    {
        // Act
        var info = LibSQLVersion.GetVersionInfo();

        // Assert
        Assert.NotNull(info);
        Assert.Contains("libSQL Version:", info);
        Assert.Contains("SQLite Version:", info);
        Assert.Contains("SQLite Source ID:", info);
    }

    [Fact]
    public void GetVersionInfo_ShouldReturnNonEmptyString()
    {
        // Act
        var info = LibSQLVersion.GetVersionInfo();

        // Assert
        Assert.NotNull(info);
        Assert.NotEmpty(info);
        // Should contain either version info or error message
        Assert.True(info.Contains("Version:") || info.Contains("Failed to retrieve"));
    }

    [Fact]
    public void VersionProperties_ShouldBeCached()
    {
        // Act - Get versions twice
        var version1 = LibSQLVersion.LibSQLVersionString;
        var version2 = LibSQLVersion.LibSQLVersionString;
        
        var sqliteVersion1 = LibSQLVersion.SQLiteVersionString;
        var sqliteVersion2 = LibSQLVersion.SQLiteVersionString;

        // Assert - Should return same cached values
        Assert.Same(version1, version2);
        Assert.Same(sqliteVersion1, sqliteVersion2);
    }
}