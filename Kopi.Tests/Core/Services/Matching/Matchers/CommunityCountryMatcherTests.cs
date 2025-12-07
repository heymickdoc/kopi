using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityCountryMatcherTests
{
    private readonly CommunityCountryMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldBe10()
    {
        Assert.Equal(10, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldBeCountryName()
    {
        Assert.Equal("country_name", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("INT")]
    [InlineData("BIGINT")]
    [InlineData("DECIMAL")]
    public void IsMatch_NonStringDataType_ReturnsFalse(string dataType)
    {
        var column = new ColumnModel { ColumnName = "Country", DataType = dataType };
        var table = new TableModel { SchemaName = "dbo" };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("VARCHAR", "2")]
    [InlineData("VARCHAR", "3")]
    [InlineData("CHAR", "2")]
    public void IsMatch_ShortMaxLength_ReturnsFalse(string dataType, string maxLength)
    {
        var column = new ColumnModel { ColumnName = "Country", DataType = dataType, MaxLength = maxLength };
        var table = new TableModel { SchemaName = "dbo" };

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
    public void IsMatch_InvalidSchemaName_ReturnsFalse(string schemaName)
    {
        var column = new ColumnModel { ColumnName = "Country", DataType = "VARCHAR", MaxLength = "100"};
        var table = new TableModel { SchemaName = schemaName };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("CountryCode")]
    [InlineData("CountryID")]
    [InlineData("Country_ISO")]
    [InlineData("CountryKey")]
    public void IsMatch_ExclusionWords_ReturnsFalse(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "VARCHAR", MaxLength = "100" };
        var table = new TableModel { SchemaName = "dbo" };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_County_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "County", DataType = "VARCHAR", MaxLength = "100" };
        var table = new TableModel { SchemaName = "dbo" };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("Country")]
    [InlineData("CountryName")]
    [InlineData("Nation")]
    [InlineData("Nationality")]
    [InlineData("CountryRegion")]
    [InlineData("AddressCountry")]
    [InlineData("BillingCountry")]
    [InlineData("ShippingCountry")]
    [InlineData("MailingCountry")]
    public void IsMatch_StrongColumnNames_ReturnsTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "VARCHAR", MaxLength = "100" };
        var table = new TableModel { SchemaName = "dbo" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("country_name")]
    [InlineData("country-name")]
    [InlineData("COUNTRY_NAME")]
    public void IsMatch_StrongColumnNamesWithSeparators_ReturnsTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "VARCHAR", MaxLength = "100" };
        var table = new TableModel { SchemaName = "dbo" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("CustomerCountry")]
    [InlineData("VendorCountry")]
    [InlineData("EmployeeCountry")]
    public void IsMatch_CountryWithContext_ReturnsTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "VARCHAR", MaxLength = "100" };
        var table = new TableModel { SchemaName = "dbo" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("NationName")]
    [InlineData("CustomerNation")]
    public void IsMatch_NationVariants_ReturnsTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "VARCHAR", MaxLength = "100" };
        var table = new TableModel { SchemaName = "dbo" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("VARCHAR", "100")]
    [InlineData("NVARCHAR", "255")]
    [InlineData("TEXT", "-1")]
    public void IsMatch_ValidStringTypes_ReturnsTrue(string dataType, string maxLength)
    {
        var column = new ColumnModel { ColumnName = "Country", DataType = dataType, MaxLength = maxLength};
        var table = new TableModel { SchemaName = "dbo" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_CountryWithMaxLengthZero_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "Country", DataType = "VARCHAR", MaxLength = "MAX" };
        var table = new TableModel { SchemaName = "dbo" };

        Assert.True(_matcher.IsMatch(column, table));
    }
}