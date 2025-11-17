using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityAddressLine1MatcherTests
{
    private readonly CommunityAddressLine1Matcher _matcher = new();

    [Theory]
    [InlineData("Address1")]
    [InlineData("address1")]
    [InlineData("Addr1")]
    [InlineData("Street1")]
    [InlineData("StreetAddress1")]
    [InlineData("AddressLine1")]
    [InlineData("AddrLine1")]
    [InlineData("address_1")]
    [InlineData("address-1")]
    public void IsMatch_StrongColumnNames_ReturnsTrue(string columnName)
    {
        var column = CreateColumn(columnName, "nvarchar");
        var table = CreateTable("dbo", "AnyTable");

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Address", "1")]
    [InlineData("Street", "1")]
    [InlineData("Addr", "One")]
    public void IsMatch_AddressWordWithNumberOne_ReturnsTrue(string addressWord, string number)
    {
        var column = CreateColumn($"{addressWord} {number}", "varchar");
        var table = CreateTable("dbo", "AnyTable");

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Customers")]
    [InlineData("CustomerAddress")]
    [InlineData("ShippingLocations")]
    [InlineData("BillingInfo")]
    public void IsMatch_Line1WithAddressTableContext_ReturnsTrue(string tableName)
    {
        var column = CreateColumn("Line1", "nvarchar");
        var table = CreateTable("dbo", tableName);

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_Line1WithAddressSchemaContext_ReturnsTrue()
    {
        var column = CreateColumn("Line1", "nvarchar");
        var table = CreateTable("CustomerSchema", "Orders");

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_Line1WithoutTableContext_ReturnsFalse()
    {
        var column = CreateColumn("Line1", "nvarchar");
        var table = CreateTable("dbo", "Orders");

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("production")]
    [InlineData("inventory")]
    [InlineData("product")]
    [InlineData("log")]
    [InlineData("system")]
    [InlineData("error")]
    [InlineData("auth")]
    public void IsMatch_InvalidSchemaNames_ReturnsFalse(string schemaName)
    {
        var column = CreateColumn("Address1", "nvarchar");
        var table = CreateTable(schemaName, "AnyTable");

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_AddressWithoutNumberOne_ReturnsFalse()
    {
        var column = CreateColumn("Address", "varchar");
        var table = CreateTable("dbo", "AnyTable");

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("bigint")]
    [InlineData("decimal")]
    [InlineData("datetime")]
    public void IsMatch_NonStringDataType_ReturnsFalse(string dataType)
    {
        var column = CreateColumn("Address1", dataType);
        var table = CreateTable("dbo", "Customers");

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void Priority_Returns11()
    {
        Assert.Equal(11, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ReturnsAddressLine1()
    {
        Assert.Equal("address_line1", _matcher.GeneratorTypeKey);
    }

    private static ColumnModel CreateColumn(string name, string dataType)
    {
        return new ColumnModel
        {
            ColumnName = name,
            DataType = dataType
        };
    }

    private static TableModel CreateTable(string schema, string table)
    {
        return new TableModel
        {
            SchemaName = schema,
            TableName = table
        };
    }
}