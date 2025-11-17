using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityAddressLine2MatcherTests
{
    private readonly CommunityAddressLine2Matcher _matcher = new();

    [Theory]
    [InlineData("Address2")]
    [InlineData("address2")]
    [InlineData("Addr2")]
    [InlineData("Street2")]
    [InlineData("StreetAddress2")]
    [InlineData("AddressLine2")]
    [InlineData("AddrLine2")]
    [InlineData("address_2")]
    [InlineData("address-2")]
    public void IsMatch_StrongColumnNames_ReturnsTrue(string columnName)
    {
        var column = CreateColumn(columnName, "nvarchar");
        var table = CreateTable("dbo", "AnyTable");

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Address", "2")]
    [InlineData("Street", "2")]
    [InlineData("Addr", "Two")]
    public void IsMatch_AddressWordWithNumberTwo_ReturnsTrue(string addressWord, string number)
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
    public void IsMatch_Line2WithAddressTableContext_ReturnsTrue(string tableName)
    {
        var column = CreateColumn("Line2", "nvarchar");
        var table = CreateTable("dbo", tableName);

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_Line2WithAddressSchemaContext_ReturnsTrue()
    {
        var column = CreateColumn("Line2", "nvarchar");
        var table = CreateTable("CustomerSchema", "Orders");

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_Line2WithoutTableContext_ReturnsFalse()
    {
        var column = CreateColumn("Line2", "nvarchar");
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
        var column = CreateColumn("Address2", "nvarchar");
        var table = CreateTable(schemaName, "AnyTable");

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_AddressWithoutNumberTwo_ReturnsFalse()
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
        var column = CreateColumn("Address2", dataType);
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
    public void GeneratorTypeKey_ReturnsAddressLine2()
    {
        Assert.Equal("address_line2", _matcher.GeneratorTypeKey);
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