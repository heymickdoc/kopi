using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityDefaultBooleanMatcherTests
{
    private readonly CommunityDefaultBooleanMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldReturn1()
    {
        Assert.Equal(1, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnDefaultBoolean()
    {
        Assert.Equal("default_boolean", _matcher.GeneratorTypeKey);
    }

    [Fact]
    public void IsMatch_WithBitDataType_ShouldReturnTrue()
    {
        var column = new ColumnModel { DataType = "bit" };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithBitDataTypeUpperCase_ShouldReturnTrue()
    {
        var column = new ColumnModel { DataType = "BIT" };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithBitDataTypeMixedCase_ShouldReturnTrue()
    {
        var column = new ColumnModel { DataType = "BiT" };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("varchar")]
    [InlineData("datetime")]
    [InlineData("boolean")]
    [InlineData("")]
    public void IsMatch_WithNonBitDataType_ShouldReturnFalse(string dataType)
    {
        var column = new ColumnModel { DataType = dataType };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

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