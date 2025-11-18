using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityDefaultDateMatcherTests
{
    private readonly CommunityDefaultDateMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldReturn1()
    {
        // Assert
        Assert.Equal(1, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnDefaultDate()
    {
        // Assert
        Assert.Equal("default_date", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("date")]
    [InlineData("DATE")]
    [InlineData("datetime")]
    [InlineData("DateTime")]
    public void IsMatch_WithDateTypes_ShouldReturnTrue(string dataType)
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
    [InlineData("varchar")]
    [InlineData("int")]
    [InlineData("decimal")]
    [InlineData("boolean")]
    public void IsMatch_WithNonDateTypes_ShouldReturnFalse(string dataType)
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

        // Act & Assert
        var exception = Record.Exception(() => _matcher.IsMatch(column, table));
        Assert.Null(exception);
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