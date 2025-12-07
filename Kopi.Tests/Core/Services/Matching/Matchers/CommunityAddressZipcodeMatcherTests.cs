using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityAddressZipcodeMatcherTests
{
    private readonly CommunityAddressZipcodeMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldReturn10()
    {
        Assert.Equal(10, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnAddressZipcode()
    {
        Assert.Equal("address_zipcode", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("ZipCode", "varchar")]
    [InlineData("Zip", "nvarchar")]
    [InlineData("AddressZip", "char")]
    [InlineData("BillingZip", "varchar")]
    [InlineData("ShippingZip", "nvarchar")]
    [InlineData("MailingZip", "varchar")]
    public void IsMatch_StrongColumnNames_ReturnsTrue(string columnName, string dataType)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType };
        var table = new TableModel { SchemaName = "dbo", TableName = "Customer" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("customer_zip", "varchar")]
    [InlineData("billing_zip_code", "nvarchar")]
    [InlineData("shipping_zip", "char")]
    public void IsMatch_ColumnWithZipToken_ReturnsTrue(string columnName, string dataType)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType };
        var table = new TableModel { SchemaName = "dbo", TableName = "Address" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("ZipCode", "int")]
    [InlineData("Zip", "bigint")]
    [InlineData("AddressZip", "decimal")]
    public void IsMatch_NonStringDataType_ReturnsFalse(string columnName, string dataType)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType };
        var table = new TableModel { SchemaName = "dbo", TableName = "Customer" };

        Assert.False(_matcher.IsMatch(column, table));
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
        var column = new ColumnModel { ColumnName = "ZipCode", DataType = "varchar" };
        var table = new TableModel { SchemaName = schemaName, TableName = "SomeTable" };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("PostalCode", "varchar")]
    [InlineData("Code", "varchar")]
    [InlineData("Area", "varchar")]
    [InlineData("Region", "nvarchar")]
    public void IsMatch_UnrelatedColumnNames_ReturnsFalse(string columnName, string dataType)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType };
        var table = new TableModel { SchemaName = "dbo", TableName = "Customer" };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("zip-code", "varchar")]
    [InlineData("zip_code", "nvarchar")]
    [InlineData("ZIP_CODE", "varchar")]
    public void IsMatch_NormalizedColumnNames_ReturnsTrue(string columnName, string dataType)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType };
        var table = new TableModel { SchemaName = "dbo", TableName = "Address" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_ZipInMiddleOfColumnName_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "CustomerZipField", DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Customer" };

        Assert.True(_matcher.IsMatch(column, table));
    }
}