using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunitySpecialNameMatcherTests
{
    private readonly CommunitySpecialNameMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldReturn50()
    {
        Assert.Equal(50, _matcher.Priority);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("Name")]
    [InlineData("[Name]")]
    [InlineData("[name]")]
    public void IsMatch_WithNameColumn_ShouldMatchCorrectly(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar", MaxLength = "100"};
        var table = new TableModel { TableName = "Product" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
        Assert.Equal("product_name", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("FirstName")]
    [InlineData("ProductName")]
    [InlineData("FullName")]
    [InlineData("username")]
    public void IsMatch_WithNonStrictNameColumn_ShouldReturnFalse(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar", MaxLength = "100"};
        var table = new TableModel { TableName = "Product" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_WithNonStringType_ShouldReturnFalse()
    {
        var column = new ColumnModel { ColumnName = "name", DataType = "int" };
        var table = new TableModel { TableName = "Product" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_ProductTable_ShouldSetProductNameGenerator()
    {
        var column = new ColumnModel { ColumnName = "name", DataType = "varchar", MaxLength = "200" };
        var table = new TableModel { TableName = "Product" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
        Assert.Equal("product_name", _matcher.GeneratorTypeKey);
    }

    [Fact]
    public void IsMatch_PersonTable_ShouldSetFullNameGenerator()
    {
        var column = new ColumnModel { ColumnName = "name", DataType = "varchar", MaxLength = "150" };
        var table = new TableModel { TableName = "Person" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
        Assert.Equal("full_name", _matcher.GeneratorTypeKey);
    }

    [Fact]
    public void IsMatch_CountryTable_ShouldSetCountryNameGenerator()
    {
        var column = new ColumnModel { ColumnName = "name", DataType = "varchar", MaxLength = "100" };
        var table = new TableModel { TableName = "Country" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
        Assert.Equal("country_name", _matcher.GeneratorTypeKey);
    }

    [Fact]
    public void IsMatch_StateTable_ShouldSetStateGenerator()
    {
        var column = new ColumnModel { ColumnName = "name", DataType = "varchar", MaxLength = "50" };
        var table = new TableModel { TableName = "State" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
        Assert.Equal("address_state", _matcher.GeneratorTypeKey);
    }

    [Fact]
    public void IsMatch_RegionTable_ShouldSetRegionGenerator()
    {
        var column = new ColumnModel { ColumnName = "name", DataType = "varchar", MaxLength = "100"};
        var table = new TableModel { TableName = "Region" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
        Assert.Equal("address_region", _matcher.GeneratorTypeKey);
    }

    [Fact]
    public void IsMatch_CityTable_ShouldSetCityGenerator()
    {
        var column = new ColumnModel { ColumnName = "name", DataType = "varchar", MaxLength = "100" };
        var table = new TableModel { TableName = "City" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
        Assert.Equal("address_city", _matcher.GeneratorTypeKey);
    }

    [Fact]
    public void IsMatch_UnknownTable_ShouldReturnFalse()
    {
        var column = new ColumnModel { ColumnName = "name", DataType = "varchar", MaxLength = "100" };
        var table = new TableModel { TableName = "UnknownTable" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }
}