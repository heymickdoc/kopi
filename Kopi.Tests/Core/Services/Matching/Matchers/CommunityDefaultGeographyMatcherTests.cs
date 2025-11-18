using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityDefaultGeographyMatcherTests
{
    private readonly CommunityDefaultGeographyMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldReturn20()
    {
        // Act
        var priority = _matcher.Priority;

        // Assert
        Assert.Equal(20, priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnDefaultGeography()
    {
        // Act
        var key = _matcher.GeneratorTypeKey;

        // Assert
        Assert.Equal("default_geography", key);
    }

    [Fact]
    public void IsMatch_WithGeographyDataType_ShouldReturnTrue()
    {
        // Arrange
        var column = new ColumnModel { DataType = "geography" };
        var table = new TableModel();

        // Act
        var result = _matcher.IsMatch(column, table);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("GEOGRAPHY")]
    [InlineData("Geography")]
    [InlineData("GeoGraphy")]
    public void IsMatch_WithGeographyDataTypeDifferentCasing_ShouldReturnTrue(string dataType)
    {
        // Arrange
        var column = new ColumnModel { DataType = dataType };
        var table = new TableModel();

        // Act
        var result = _matcher.IsMatch(column, table);

        // Assert
        Assert.True(result);
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

    [Theory]
    [InlineData("geometry")]
    [InlineData("varchar")]
    [InlineData("int")]
    [InlineData("nvarchar")]
    public void IsMatch_WithNonGeographyDataType_ShouldReturnFalse(string dataType)
    {
        // Arrange
        var column = new ColumnModel { DataType = dataType };
        var table = new TableModel();

        // Act
        var result = _matcher.IsMatch(column, table);

        // Assert
        Assert.False(result);
    }
}