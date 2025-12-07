using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityDefaultIntegerMatcherTests
{
    private readonly CommunityDefaultIntegerMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldReturn5()
    {
        // Act
        var priority = _matcher.Priority;

        // Assert
        Assert.Equal(5, priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnDefaultInteger()
    {
        // Act
        var generatorTypeKey = _matcher.GeneratorTypeKey;

        // Assert
        Assert.Equal("default_integer", generatorTypeKey);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("bigint")]
    [InlineData("smallint")]
    [InlineData("tinyint")]
    public void IsMatch_WithIntegerDataType_ShouldReturnTrue(string dataType)
    {
        // Arrange
        var column = new ColumnModel { DataType = dataType };
        var tableContext = new TableModel();

        // Act
        var result = _matcher.IsMatch(column, tableContext);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("varchar")]
    [InlineData("nvarchar")]
    [InlineData("decimal")]
    [InlineData("datetime")]
    public void IsMatch_WithNonIntegerDataType_ShouldReturnFalse(string dataType)
    {
        // Arrange
        var column = new ColumnModel { DataType = dataType };
        var tableContext = new TableModel();

        // Act
        var result = _matcher.IsMatch(column, tableContext);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_WithNullDataType_ShouldReturnFalse()
    {
        // Arrange
        var column = new ColumnModel { DataType = null };
        var tableContext = new TableModel();

        // Act
        var result = _matcher.IsMatch(column, tableContext);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_WithEmptyDataType_ShouldReturnFalse()
    {
        // Arrange
        var column = new ColumnModel { DataType = string.Empty };
        var tableContext = new TableModel();

        // Act
        var result = _matcher.IsMatch(column, tableContext);

        // Assert
        Assert.False(result);
    }
}