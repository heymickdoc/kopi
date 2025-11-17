using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityAddressCityMatcherTests
{
    private readonly CommunityAddressCityMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldReturn10()
    {
        Assert.Equal(10, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnAddressCity()
    {
        Assert.Equal("address_city", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("city", "address", "dbo", true)]
    [InlineData("CityName", "customer", "person", true)]
    [InlineData("AddressCity", "location", "address", true)]
    public void IsMatch_WithExactColumnNameMatch_ShouldReturnTrue(string columnName, string tableName, string schemaName, bool expected)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { TableName = tableName, SchemaName = schemaName };

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("mycity", "address", "dbo", true)]
    [InlineData("city_name", "customer", "person", true)]
    public void IsMatch_WithPartialColumnNameMatch_ShouldReturnTrue(string columnName, string tableName, string schemaName, bool expected)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "nvarchar" };
        var table = new TableModel { TableName = tableName, SchemaName = schemaName };

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsMatch_WithNonStringDataType_ShouldReturnFalse()
    {
        var column = new ColumnModel { ColumnName = "city", DataType = "int" };
        var table = new TableModel { TableName = "address", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("random", "other", "dbo", false)]
    [InlineData("id", "product", "inventory", false)]
    public void IsMatch_WithLowScore_ShouldReturnFalse(string columnName, string tableName, string schemaName, bool expected)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { TableName = tableName, SchemaName = schemaName };

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsMatch_WithExactScoreThreshold_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "city", DataType = "text" };
        var table = new TableModel { TableName = "other", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }
}