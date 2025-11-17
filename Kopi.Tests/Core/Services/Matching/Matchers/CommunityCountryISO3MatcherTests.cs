using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityCountryISO3MatcherTests
{
    private readonly CommunityCountryISO3Matcher _matcher = new();

    [Fact]
    public void Priority_ShouldBe11()
    {
        Assert.Equal(11, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldBeCountryISO3()
    {
        Assert.Equal("country_iso3", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("char", "CountryCode", "dbo", "Customer")]
    [InlineData("varchar", "CountryISO3", "dbo", "Address")]
    [InlineData("nchar", "ISO3", "dbo", "Location")]
    public void IsMatch_WithValidISO3Columns_ShouldReturnTrue(string dataType, string columnName, string schema, string table)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType, MaxLength = "3"};
        var tableContext = new TableModel { SchemaName = schema, TableName = table };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Theory]
    [InlineData("char", "CountryCode", "8")]
    [InlineData("varchar", "Country", "14")]
    [InlineData("nvarchar", "CountryISO3", "MAX")]
    public void IsMatch_WithIncorrectMaxLength_ShouldReturnFalse(string dataType, string columnName, string maxLength)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType, MaxLength = maxLength};
        var tableContext = new TableModel { SchemaName = "dbo", TableName = "Address" };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.False(result);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("bigint")]
    [InlineData("decimal")]
    public void IsMatch_WithNonStringDataType_ShouldReturnFalse(string dataType)
    {
        var column = new ColumnModel { ColumnName = "CountryCode", DataType = dataType };
        var tableContext = new TableModel { SchemaName = "dbo", TableName = "Address" };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.False(result);
    }

    [Theory]
    [InlineData("production")]
    [InlineData("inventory")]
    [InlineData("product")]
    [InlineData("log")]
    [InlineData("system")]
    public void IsMatch_WithInvalidSchemaNames_ShouldReturnFalse(string schema)
    {
        var column = new ColumnModel { ColumnName = "CountryCode", DataType = "char", MaxLength = "3"};
        var tableContext = new TableModel { SchemaName = schema, TableName = "SomeTable" };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.False(result);
    }

    [Theory]
    [InlineData("StateCode")]
    [InlineData("ProvinceCode")]
    [InlineData("LanguageCode")]
    public void IsMatch_WithExclusionWords_ShouldReturnFalse(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "char", MaxLength = "3"};
        var tableContext = new TableModel { SchemaName = "dbo", TableName = "Address" };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.False(result);
    }

    [Theory]
    [InlineData("iso3")]
    [InlineData("countryiso3")]
    [InlineData("countrycode3")]
    [InlineData("isocode3")]
    [InlineData("countryid")]
    [InlineData("countrycode")]
    [InlineData("countryregioncode")]
    public void IsMatch_WithStrongColumnNames_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "char", MaxLength = "3"};
        var tableContext = new TableModel { SchemaName = "dbo", TableName = "Address" };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithCountryAndCodeTokens_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "Country_Code", DataType = "char", MaxLength = "3"};
        var tableContext = new TableModel { SchemaName = "dbo", TableName = "Customer" };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithCountryAlone_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "Country", DataType = "char", MaxLength = "3"};
        var tableContext = new TableModel { SchemaName = "dbo", TableName = "Address" };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithISO3Token_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "ISO3", DataType = "char", MaxLength = "3"};
        var tableContext = new TableModel { SchemaName = "dbo", TableName = "Location" };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithNationAndIDTokens_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "NationID", DataType = "char", MaxLength = "3"};
        var tableContext = new TableModel { SchemaName = "dbo", TableName = "Customer" };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithUnderscoresAndDashes_ShouldNormalize()
    {
        var column = new ColumnModel { ColumnName = "Country-ISO_3", DataType = "char", MaxLength = "3"};
        var tableContext = new TableModel { SchemaName = "dbo", TableName = "Location" };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }
}