using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityAddressLine2MatcherTests
{
    private readonly CommunityAddressLine2Matcher _matcher = new();

    [Fact]
    public void IsMatch_NonStringType_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "addressline2", DataType = "int" };
        var table = new TableModel { TableName = "address", SchemaName = "person" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_ExactColumnNameAddressLine2_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "addressline2", DataType = "nvarchar" };
        var table = new TableModel { TableName = "orders", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ColumnContainsAddressLine2AndTableMatches_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "invoiceaddress2", DataType = "varchar" };
        var table = new TableModel { TableName = "address", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ColumnContainsAddressLine2WithoutContext_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "shipping_addressline2", DataType = "varchar" };
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