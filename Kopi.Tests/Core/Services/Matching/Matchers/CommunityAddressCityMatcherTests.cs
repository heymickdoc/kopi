using Xunit;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityAddressCityMatcherTests
{
    private readonly CommunityAddressCityMatcher _matcher = new();

    [Fact]
    public void IsMatch_NonStringType_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "city", DataType = "int" };
        var table = new TableModel { TableName = "address", SchemaName = "person" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_ExactColumnNameCity_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "city", DataType = "nvarchar" };
        var table = new TableModel { TableName = "orders", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ColumnContainsCityAndTableMatches_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "home_city", DataType = "varchar" };
        var table = new TableModel { TableName = "address", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ColumnContainsCityWithoutContext_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "home_city", DataType = "varchar" };
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