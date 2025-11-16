using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityAddressCountyMatcherTests
{
    private readonly CommunityAddressCountyMatcher _matcher = new();

    [Fact]
    public void IsMatch_NonStringType_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "county", DataType = "int" };
        var table = new TableModel { TableName = "address", SchemaName = "person" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_ExactColumnNameCounty_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "county", DataType = "nvarchar" };
        var table = new TableModel { TableName = "orders", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ColumnContainsCountyAndTableMatches_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "billing_county", DataType = "varchar" };
        var table = new TableModel { TableName = "address", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ColumnContainsCountyWithoutContext_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "billing_county", DataType = "varchar" };
        var table = new TableModel { TableName = "orders", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_TableAndSchemaButNoColumnMatch_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "postalcode", DataType = "nvarchar" };
        var table = new TableModel { TableName = "address", SchemaName = "person" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }
}