using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityDefaultStringMatcherTests
{
    private readonly CommunityDefaultStringMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldReturn1()
    {
        // Act
        var result = _matcher.Priority;

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnDefaultString()
    {
        // Act
        var result = _matcher.GeneratorTypeKey;

        // Assert
        Assert.Equal("default_string", result);
    }

    [Theory]
    [InlineData("varchar")]
    [InlineData("nvarchar")]
    [InlineData("char")]
    [InlineData("nchar")]
    [InlineData("text")]
    public void IsMatch_WithStringDataType_ShouldReturnTrue(string dataType)
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
    [InlineData("int")]
    [InlineData("bigint")]
    [InlineData("decimal")]
    [InlineData("datetime")]
    public void IsMatch_WithNonStringDataType_ShouldReturnFalse(string dataType)
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