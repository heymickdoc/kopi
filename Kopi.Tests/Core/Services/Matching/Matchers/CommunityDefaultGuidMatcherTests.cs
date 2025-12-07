using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityDefaultGuidMatcherTests
{
    private readonly CommunityDefaultGuidMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldReturn10()
    {
        // Arrange & Act
        var priority = _matcher.Priority;

        // Assert
        Assert.Equal(10, priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnDefaultGuid()
    {
        // Arrange & Act
        var generatorTypeKey = _matcher.GeneratorTypeKey;

        // Assert
        Assert.Equal("default_guid", generatorTypeKey);
    }

    [Fact]
    public void IsMatch_WhenDataTypeIsUniqueIdentifier_ShouldReturnTrue()
    {
        // Arrange
        var column = new ColumnModel { DataType = "uniqueidentifier" };
        var table = new TableModel();

        // Act
        var result = _matcher.IsMatch(column, table);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WhenDataTypeIsUniqueIdentifierUpperCase_ShouldReturnTrue()
    {
        // Arrange
        var column = new ColumnModel { DataType = "UNIQUEIDENTIFIER" };
        var table = new TableModel();

        // Act
        var result = _matcher.IsMatch(column, table);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WhenDataTypeIsUniqueIdentifierMixedCase_ShouldReturnTrue()
    {
        // Arrange
        var column = new ColumnModel { DataType = "UniqueIdentifier" };
        var table = new TableModel();

        // Act
        var result = _matcher.IsMatch(column, table);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WhenDataTypeIsNotUniqueIdentifier_ShouldReturnFalse()
    {
        // Arrange
        var column = new ColumnModel { DataType = "int" };
        var table = new TableModel();

        // Act
        var result = _matcher.IsMatch(column, table);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_WhenDataTypeIsNull_ShouldReturnFalse()
    {
        // Arrange
        var column = new ColumnModel { DataType = null };
        var table = new TableModel();

        // Act
        var result = _matcher.IsMatch(column, table);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_WhenDataTypeIsEmpty_ShouldReturnFalse()
    {
        // Arrange
        var column = new ColumnModel { DataType = "" };
        var table = new TableModel();

        // Act
        var result = _matcher.IsMatch(column, table);

        // Assert
        Assert.False(result);
    }
}