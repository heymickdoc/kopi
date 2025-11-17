using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityCountryMatcherTests
{
    private readonly CommunityCountryMatcher _matcher = new();

    [Fact]
    public void IsMatch_NonStringType_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "country", DataType = "int" };
        var table = new TableModel { TableName = "address", SchemaName = "person" };
        var result = _matcher.IsMatch(column, table);
        Assert.False(result);
    }


    [Fact]
    public void IsMatch_ExactColumnNameCountry_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "country", DataType = "nvarchar" };
        var table = new TableModel { TableName = "orders", SchemaName = "dbo" };
        var result = _matcher.IsMatch(column, table);
        Assert.True(result);

    }

    [Fact]
    public void IsMatch_ColumnContainsCountryAndTableMatches_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "usercountry", DataType = "varchar" };
        var table = new TableModel { TableName = "location", SchemaName = "dbo" };
        var result = _matcher.IsMatch(column, table);
        Assert.True(result);

    }

    [Fact]
    public void IsMatch_TableAndSchemaButNoColumnMatch_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "nation", DataType = "nvarchar" };
        var table = new TableModel { TableName = "countryinfo", SchemaName = "location" };
        var result = _matcher.IsMatch(column, table);
        Assert.False(result);

    }
}