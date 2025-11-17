using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityAddressPostalcodeMatcherTests
{
    private readonly CommunityAddressPostalcodeMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldBe10()
    {
        Assert.Equal(10, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldBeAddressPostalcode()
    {
        Assert.Equal("address_postalcode", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("PostalCode", "varchar")]
    [InlineData("PostCode", "nvarchar")]
    [InlineData("AddressPostalCode", "varchar")]
    [InlineData("BillingPostalCode", "nvarchar")]
    [InlineData("ShippingPostalCode", "varchar")]
    public void IsMatch_StrongColumnNames_ShouldReturnTrue(string columnName, string dataType)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType };
        var table = new TableModel { SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Postal_Code")]
    [InlineData("Billing_Postal")]
    [InlineData("Customer_Postal")]
    public void IsMatch_ContainsPostal_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Post_Code")]
    [InlineData("Billing_Post_Code")]
    public void IsMatch_ContainsPostAndCode_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("ZipCode")]
    [InlineData("Zip_Code")]
    [InlineData("BillingZip")]
    public void IsMatch_ContainsZip_ShouldReturnFalse(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo" };

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
    public void IsMatch_InvalidSchemaNames_ShouldReturnFalse(string schemaName)
    {
        var column = new ColumnModel { ColumnName = "PostalCode", DataType = "varchar" };
        var table = new TableModel { SchemaName = schemaName };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("PostalCode", "int")]
    [InlineData("PostalCode", "bigint")]
    [InlineData("PostalCode", "decimal")]
    public void IsMatch_NonStringDataType_ShouldReturnFalse(string columnName, string dataType)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType };
        var table = new TableModel { SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("PostTitle")]
    [InlineData("BlogPost")]
    [InlineData("PostDate")]
    public void IsMatch_PostWithoutCode_ShouldReturnFalse(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("postal-code")]
    [InlineData("POSTALCODE")]
    [InlineData("Postal_Code")]
    public void IsMatch_NormalizedMatching_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_CustomerPostalCode_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "CustomerPostalCode", DataType = "nvarchar" };
        var table = new TableModel { SchemaName = "customers" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_UnrelatedColumn_ShouldReturnFalse()
    {
        var column = new ColumnModel { ColumnName = "CustomerName", DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }
}