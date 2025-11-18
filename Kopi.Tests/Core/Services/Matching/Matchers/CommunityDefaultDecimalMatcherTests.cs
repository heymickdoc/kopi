using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityDefaultDecimalMatcherTests
{
    private readonly CommunityDefaultDecimalMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldReturn1()
    {
        // Act
        var priority = _matcher.Priority;

        // Assert
        Assert.Equal(1, priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnDefaultDecimal()
    {
        // Act
        var key = _matcher.GeneratorTypeKey;

        // Assert
        Assert.Equal("default_decimal", key);
    }

    [Theory]
    [InlineData("decimal")]
    [InlineData("numeric")]
    [InlineData("money")]
    [InlineData("smallmoney")]
    public void IsMatch_WithDecimalDataType_ShouldReturnTrue(string dataType)
    {
        // Arrange
        var column = new ColumnModel { DataType = dataType };
        var table = new TableModel();

        // Act
        var result = _matcher.IsMatch(column, table);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("varchar")]
    [InlineData("datetime")]
    public void IsMatch_WithNonDecimalDataType_ShouldReturnFalse(string dataType)
    {
        // Arrange
        var column = new ColumnModel { DataType = dataType };
        var table = new TableModel();

        // Act
        var result = _matcher.IsMatch(column, table);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_WithNullDataType_ShouldReturnFalse()
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
    public void IsMatch_WithEmptyDataType_ShouldReturnFalse()
    {
        // Arrange
        var column = new ColumnModel { DataType = string.Empty };
        var table = new TableModel();

        // Act
        var result = _matcher.IsMatch(column, table);

        // Assert
        Assert.False(result);
    }
}