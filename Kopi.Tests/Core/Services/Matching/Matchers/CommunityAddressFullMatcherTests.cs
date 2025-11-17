using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityAddressFullMatcherTests
{
    private readonly CommunityAddressFullMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldBe10()
    {
        Assert.Equal(10, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldBeAddressFull()
    {
        Assert.Equal("address_full", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("address", "varchar")]
    [InlineData("fulladdress", "nvarchar")]
    [InlineData("streetaddress", "text")]
    [InlineData("mailingaddress", "char")]
    public void IsMatch_ExactColumnNameMatch_ReturnsTrue(string columnName, string dataType)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType };
        var table = new TableModel { TableName = "customer", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_NonStringDataType_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "address", DataType = "int" };
        var table = new TableModel { TableName = "customer", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("user_address", "varchar")]
    [InlineData("primary_fulladdress", "nvarchar")]
    [InlineData("customer_streetaddress", "text")]
    public void IsMatch_PartialColumnNameMatch_ReturnsTrue(string columnName, string dataType)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType };
        var table = new TableModel { TableName = "customer", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_MatchingTableName_IncreasesScore()
    {
        var column = new ColumnModel { ColumnName = "addr", DataType = "varchar" };
        var table = new TableModel { TableName = "customer", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_MatchingSchemaName_IncreasesScore()
    {
        var column = new ColumnModel { ColumnName = "addr", DataType = "varchar" };
        var table = new TableModel { TableName = "orders", SchemaName = "person" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_CaseInsensitive_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "ADDRESS", DataType = "VARCHAR" };
        var table = new TableModel { TableName = "CUSTOMER", SchemaName = "DBO" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ColumnNameWithSpecialCharacters_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "full_address", DataType = "varchar" };
        var table = new TableModel { TableName = "customer", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_InsufficientScore_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "name", DataType = "varchar" };
        var table = new TableModel { TableName = "orders", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_ExactMatchWithContextBoost_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "address", DataType = "nvarchar" };
        var table = new TableModel { TableName = "customer", SchemaName = "person" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("billingaddress")]
    [InlineData("addressbilling")]
    [InlineData("invoiceaddress")]
    [InlineData("addressinvoice")]
    public void IsMatch_BillingAndInvoiceAddresses_ReturnsTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { TableName = "customer", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }
}