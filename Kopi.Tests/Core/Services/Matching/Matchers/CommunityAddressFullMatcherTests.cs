using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityAddressFullMatcherTests
{
    private readonly CommunityAddressFullMatcher _matcher = new();

    [Fact]
    public void IsMatch_NonStringType_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "fulladdress", DataType = "int" };
        var table = new TableModel { TableName = "address", SchemaName = "person" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_ExactColumnNameFullAddress_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "fulladdress", DataType = "nvarchar" };
        var table = new TableModel { TableName = "orders", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ColumnContainsFullAddressAndTableMatches_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "customer_fulladdress", DataType = "varchar" };
        var table = new TableModel { TableName = "address", SchemaName = "dbo" };
        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ColumnContainsFullAddressWithoutContext_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "customer_fulladdress", DataType = "varchar" };
        var table = new TableModel { TableName = "orders", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }
}