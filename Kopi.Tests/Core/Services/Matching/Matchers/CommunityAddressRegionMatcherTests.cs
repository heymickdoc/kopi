using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityAddressRegionMatcherTests
{
    private readonly CommunityAddressRegionMatcher _matcher;

    public CommunityAddressRegionMatcherTests()
    {
        _matcher = new CommunityAddressRegionMatcher();
    }

    [Fact]
    public void Priority_ShouldReturn10()
    {
        Assert.Equal(10, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnAddressRegion()
    {
        Assert.Equal("address_region", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("Region")]
    [InlineData("RegionName")]
    [InlineData("SalesRegion")]
    [InlineData("SalesTerritory")]
    [InlineData("Territory")]
    [InlineData("TerritoryName")]
    [InlineData("GeoRegion")]
    public void IsMatch_StrongColumnNames_ShouldReturnTrue(string columnName)
    {
        var column = CreateColumn(columnName, "varchar");
        var table = CreateTable("dbo", "Customers");

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("region")]
    [InlineData("sales_region")]
    [InlineData("territory")]
    [InlineData("sales_territory")]
    public void IsMatch_TokenBasedMatches_ShouldReturnTrue(string columnName)
    {
        var column = CreateColumn(columnName, "varchar");
        var table = CreateTable("dbo", "Sales");

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("CountryRegion")]
    [InlineData("StateRegion")]
    [InlineData("ProvinceRegion")]
    [InlineData("CountyRegion")]
    [InlineData("RegionID")]
    [InlineData("RegionCode")]
    public void IsMatch_ExclusionWords_ShouldReturnFalse(string columnName)
    {
        var column = CreateColumn(columnName, "varchar");
        var table = CreateTable("dbo", "Geography");

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
        var column = CreateColumn("Region", "varchar");
        var table = CreateTable(schemaName, "Data");

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("bigint")]
    [InlineData("decimal")]
    [InlineData("datetime")]
    [InlineData("bit")]
    public void IsMatch_NonStringDataType_ShouldReturnFalse(string dataType)
    {
        var column = CreateColumn("Region", dataType);
        var table = CreateTable("dbo", "Sales");

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("CustomerName")]
    [InlineData("Address")]
    [InlineData("City")]
    [InlineData("PhoneNumber")]
    public void IsMatch_UnrelatedColumnNames_ShouldReturnFalse(string columnName)
    {
        var column = CreateColumn(columnName, "varchar");
        var table = CreateTable("dbo", "Customers");

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("region-name")]
    [InlineData("sales_territory")]
    [InlineData("GeoRegion")]
    public void IsMatch_VariousNamingConventions_ShouldReturnTrue(string columnName)
    {
        var column = CreateColumn(columnName, "nvarchar");
        var table = CreateTable("dbo", "Sales");

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    private static ColumnModel CreateColumn(string columnName, string dataType)
    {
        return new ColumnModel
        {
            ColumnName = columnName,
            DataType = dataType
        };
    }

    private static TableModel CreateTable(string schemaName, string tableName)
    {
        return new TableModel
        {
            SchemaName = schemaName,
            TableName = tableName
        };
    }
}