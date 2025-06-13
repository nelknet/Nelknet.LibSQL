using System.Data.Common;
using Nelknet.LibSQL.Data;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class LibSQLFactoryTests
{
    [Fact]
    public void Instance_ShouldBeSingleton()
    {
        // Arrange & Act
        var instance1 = LibSQLFactory.Instance;
        var instance2 = LibSQLFactory.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void ProviderInvariantName_ShouldBeCorrect()
    {
        // Assert
        Assert.Equal("Nelknet.LibSQL.Data", LibSQLFactory.ProviderInvariantName);
    }

    [Fact]
    public void CreateConnection_ShouldReturnLibSQLConnection()
    {
        // Arrange
        var factory = LibSQLFactory.Instance;

        // Act
        var connection = factory.CreateConnection();

        // Assert
        Assert.NotNull(connection);
        Assert.IsType<LibSQLConnection>(connection);
    }

    [Fact]
    public void CreateCommand_ShouldReturnLibSQLCommand()
    {
        // Arrange
        var factory = LibSQLFactory.Instance;

        // Act
        var command = factory.CreateCommand();

        // Assert
        Assert.NotNull(command);
        Assert.IsType<LibSQLCommand>(command);
    }

    [Fact]
    public void CreateParameter_ShouldReturnLibSQLParameter()
    {
        // Arrange
        var factory = LibSQLFactory.Instance;

        // Act
        var parameter = factory.CreateParameter();

        // Assert
        Assert.NotNull(parameter);
        Assert.IsType<LibSQLParameter>(parameter);
    }

    [Fact]
    public void CreateConnectionStringBuilder_ShouldReturnLibSQLConnectionStringBuilder()
    {
        // Arrange
        var factory = LibSQLFactory.Instance;

        // Act
        var builder = factory.CreateConnectionStringBuilder();

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<LibSQLConnectionStringBuilder>(builder);
    }

    [Fact]
    public void CanCreateProperties_ShouldReturnFalse()
    {
        // Arrange
        var factory = LibSQLFactory.Instance;

        // Assert
        Assert.False(factory.CanCreateCommandBuilder);
        Assert.False(factory.CanCreateDataAdapter);
        Assert.False(factory.CanCreateDataSourceEnumerator);
    }

    [Fact]
    public void CreateCommandBuilder_ShouldThrowNotSupportedException()
    {
        // Arrange
        var factory = LibSQLFactory.Instance;

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => factory.CreateCommandBuilder());
    }

    [Fact]
    public void CreateDataAdapter_ShouldThrowNotSupportedException()
    {
        // Arrange
        var factory = LibSQLFactory.Instance;

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => factory.CreateDataAdapter());
    }

    [Fact]
    public void RegisterFactory_ShouldRegisterWithDbProviderFactories()
    {
        // Arrange
        LibSQLFactory.UnregisterFactory(); // Ensure clean state

        // Act
        LibSQLFactory.RegisterFactory();

        // Assert
        var registeredFactory = DbProviderFactories.GetFactory(LibSQLFactory.ProviderInvariantName);
        Assert.NotNull(registeredFactory);
        Assert.Same(LibSQLFactory.Instance, registeredFactory);

        // Cleanup
        LibSQLFactory.UnregisterFactory();
    }

    [Fact]
    public void UnregisterFactory_ShouldRemoveFromDbProviderFactories()
    {
        // Arrange
        LibSQLFactory.RegisterFactory();

        // Act
        LibSQLFactory.UnregisterFactory();

        // Assert
        Assert.Throws<ArgumentException>(() => DbProviderFactories.GetFactory(LibSQLFactory.ProviderInvariantName));
    }
}