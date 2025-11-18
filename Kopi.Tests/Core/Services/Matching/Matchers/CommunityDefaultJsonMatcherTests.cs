using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityDefaultJsonMatcherTests
{
    private readonly CommunityDefaultJsonMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldReturn10()
    {
        Assert.Equal(10, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnDefaultJson()
    {
        Assert.Equal("default_json", _matcher.GeneratorTypeKey);
    }

    [Fact]
    public void IsMatch_WithJsonDataType_ShouldReturnTrue()
    {
        var column = new ColumnModel { DataType = "json" };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithJsonDataTypeUpperCase_ShouldReturnTrue()
    {
        var column = new ColumnModel { DataType = "JSON" };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithMixedCaseJsonDataType_ShouldReturnTrue()
    {
        var column = new ColumnModel { DataType = "Json" };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithNonJsonDataType_ShouldReturnFalse()
    {
        var column = new ColumnModel { DataType = "varchar" };
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
        var column = new ColumnModel { DataType = "" };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_WithWhitespaceDataType_ShouldReturnFalse()
    {
        var column = new ColumnModel { DataType = "   " };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }
}