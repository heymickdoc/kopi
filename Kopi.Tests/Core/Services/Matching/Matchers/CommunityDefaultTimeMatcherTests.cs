using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityDefaultTimeMatcherTests
{
    private readonly CommunityDefaultTimeMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldReturn2()
    {
        Assert.Equal(2, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnDefaultTime()
    {
        Assert.Equal("default_time", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("time")]
    [InlineData("TIME")]
    [InlineData("time2")]
    [InlineData("TIME2")]
    public void IsMatch_WithTimeDataTypes_ShouldReturnTrue(string dataType)
    {
        var column = new ColumnModel { DataType = dataType };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("varchar")]
    [InlineData("int")]
    [InlineData("datetime")]
    [InlineData("date")]
    [InlineData("timestamp")]
    [InlineData("")]
    public void IsMatch_WithNonTimeDataTypes_ShouldReturnFalse(string dataType)
    {
        var column = new ColumnModel { DataType = dataType };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_WithNullDataType_ShouldReturnFalse()
    {
        var column = new ColumnModel { DataType = null };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

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