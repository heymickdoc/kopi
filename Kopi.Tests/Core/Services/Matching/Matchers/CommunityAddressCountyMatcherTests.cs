using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityAddressCountyMatcherTests
{
    private readonly CommunityAddressCountyMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldBe10()
    {
        Assert.Equal(10, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldBeAddressCounty()
    {
        Assert.Equal("address_county", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("county")]
    [InlineData("addresscounty")]
    [InlineData("addrcounty")]
    [InlineData("regioncounty")]
    public void IsMatch_ExactColumnNameMatch_ReturnsTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { TableName = "test", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_NonStringDataType_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "county", DataType = "int" };
        var table = new TableModel { TableName = "address", SchemaName = "person" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("address")]
    [InlineData("location")]
    [InlineData("customer")]
    [InlineData("user")]
    public void IsMatch_WithMatchingTableName_IncreasesScore(string tableName)
    {
        var column = new ColumnModel { ColumnName = "county", DataType = "nvarchar" };
        var table = new TableModel { TableName = tableName, SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("person")]
    [InlineData("customer")]
    [InlineData("location")]
    [InlineData("address")]
    public void IsMatch_WithMatchingSchemaName_IncreasesScore(string schemaName)
    {
        var column = new ColumnModel { ColumnName = "county", DataType = "varchar" };
        var table = new TableModel { TableName = "test", SchemaName = schemaName };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_PartialColumnNameMatch_WithSufficientScore_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "customer_county_name", DataType = "varchar" };
        var table = new TableModel { TableName = "address", SchemaName = "person" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_InsufficientScore_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "biscuit", DataType = "varchar" };
        var table = new TableModel { TableName = "banana", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_CaseInsensitive_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "COUNTY", DataType = "VARCHAR" };
        var table = new TableModel { TableName = "ADDRESS", SchemaName = "PERSON" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ColumnNameWithSpecialCharacters_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "addr_county", DataType = "nvarchar" };
        var table = new TableModel { TableName = "customer", SchemaName = "person" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }
}