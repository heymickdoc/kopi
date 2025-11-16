using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityAddressPostalcodeMatcherTests
{
    private readonly CommunityAddressPostalcodeMatcher _matcher = new();

    [Fact]
    public void IsMatch_NonStringType_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "postalcode", DataType = "int" };
        var table = new TableModel { TableName = "address", SchemaName = "person" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_ExactColumnNamePostalCode_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "postalcode", DataType = "nvarchar" };
        var table = new TableModel { TableName = "orders", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ColumnContainsPostalCodeAndTableMatches_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "billing_postalcode", DataType = "varchar" };
        var table = new TableModel { TableName = "address", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }
    
    [Fact]
    public void IsMatch_TableAndSchemaButNoColumnMatch_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "city", DataType = "nvarchar" };
        var table = new TableModel { TableName = "address", SchemaName = "person" };

        var result = _matcher.IsMatch(column, table);
        Assert.False(result);

    }
}