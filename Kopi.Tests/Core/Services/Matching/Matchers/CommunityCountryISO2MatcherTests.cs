using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityCountryISO2MatcherTests
{
    private readonly CommunityCountryISO2Matcher _matcher = new();

    [Fact]
    public void Priority_ShouldBe11()
    {
        Assert.Equal(11, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldBeCountryISO2()
    {
        Assert.Equal("country_iso2", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("char", "CountryCode", "dbo", "Customer", true)]
    [InlineData("varchar", "CountryISO2", "dbo", "Address", true)]
    [InlineData("nchar", "ISO2", "dbo", "Location", true)]
    public void IsMatch_WithValidISO2Columns_ShouldReturnTrue(string dataType, string columnName, string schema, string table, bool expected)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType, MaxLength = "2"};
        var tableContext = new TableModel { SchemaName = schema, TableName = table };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Theory]
    [InlineData("char", "CountryCode", "3")]
    [InlineData("varchar", "Country", "14")]
    [InlineData("nvarchar", "CountryISO2", "MAX")]
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
        var column = new ColumnModel { ColumnName = "CountryCode", DataType = "char", MaxLength = "2"};
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
        var column = new ColumnModel { ColumnName = columnName, DataType = "char", MaxLength = "2"};
        var tableContext = new TableModel { SchemaName = "dbo", TableName = "Address" };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.False(result);
    }

    [Theory]
    [InlineData("iso2")]
    [InlineData("countryiso2")]
    [InlineData("countrycode2")]
    [InlineData("isocode2")]
    [InlineData("countryid")]
    [InlineData("countrycode")]
    [InlineData("countryregioncode")]
    public void IsMatch_WithStrongColumnNames_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "char", MaxLength = "2"};
        var tableContext = new TableModel { SchemaName = "dbo", TableName = "Address" };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithCountryAndCodeTokens_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "Country_Code", DataType = "char", MaxLength = "2"};
        var tableContext = new TableModel { SchemaName = "dbo", TableName = "Customer" };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithCountryAlone_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "Country", DataType = "char", MaxLength = "2"};
        var tableContext = new TableModel { SchemaName = "dbo", TableName = "Address" };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithISO2Token_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "ISO2", DataType = "char", MaxLength = "2"};
        var tableContext = new TableModel { SchemaName = "dbo", TableName = "Location" };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithNationAndIDTokens_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "NationID", DataType = "char", MaxLength = "2"};
        var tableContext = new TableModel { SchemaName = "dbo", TableName = "Customer" };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithUnderscoresAndDashes_ShouldNormalize()
    {
        var column = new ColumnModel { ColumnName = "Country-ISO_2", DataType = "char", MaxLength = "2"};
        var tableContext = new TableModel { SchemaName = "dbo", TableName = "Location" };

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }
}