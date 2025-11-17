using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityCountryISO3MatcherTests
{
    private readonly CommunityCountryISO3Matcher _matcher = new();
    
    [Fact]
    public void IsMatch_NonStringType_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "iso3", DataType = "int" };
        var table = new TableModel { TableName = "address", SchemaName = "person" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }
    
    [Fact]
    public void IsMatch_ExactColumnNameCountryISO3_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "countryiso3", DataType = "nvarchar" };
        var table = new TableModel { TableName = "orders", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ColumnContainsCountryISO3AndTableMatches_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "countryiso", DataType = "varchar" };
        var table = new TableModel { TableName = "location", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    
    [Fact]
    public void IsMatch_TableAndSchemaButNoColumnMatch_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "nationcode", DataType = "nvarchar" };
        var table = new TableModel { TableName = "country", SchemaName = "location" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }
}