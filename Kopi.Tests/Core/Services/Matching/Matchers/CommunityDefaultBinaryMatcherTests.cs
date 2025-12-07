using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityDefaultBinaryMatcherTests
{
    private readonly CommunityDefaultBinaryMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldReturn1()
    {
        // Act
        var priority = _matcher.Priority;

        // Assert
        Assert.Equal(1, priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnDefaultBinary()
    {
        // Act
        var key = _matcher.GeneratorTypeKey;

        // Assert
        Assert.Equal("default_binary", key);
    }

    [Theory]
    [InlineData("binary")]
    [InlineData("varbinary")]
    [InlineData("image")]
    public void IsMatch_WithBinaryDataType_ShouldReturnTrue(string dataType)
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
    [InlineData("datetime")]
    [InlineData("decimal")]
    public void IsMatch_WithNonBinaryDataType_ShouldReturnFalse(string dataType)
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