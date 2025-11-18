using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityDefaultMoneyMatcherTests
{
    private readonly CommunityDefaultMoneyMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldReturn5()
    {
        Assert.Equal(5, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnDefaultMoney()
    {
        Assert.Equal("default_money", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("money")]
    [InlineData("smallmoney")]
    public void IsMatch_WithMoneyType_ShouldReturnTrue(string dataType)
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
    [InlineData("bit")]
    public void IsMatch_WithNonMoneyType_ShouldReturnFalse(string dataType)
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
        var column = new ColumnModel { DataType = string.Empty };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }
}