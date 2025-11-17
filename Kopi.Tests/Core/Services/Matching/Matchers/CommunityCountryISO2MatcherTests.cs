using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityCountryISO2MatcherTests
{
    private readonly CommunityCountryISO2Matcher _matcher = new();

    [Fact]
    public void Priority_ShouldReturn10()
    {
        Assert.Equal(10, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnCountryISO2()
    {
        Assert.Equal("country_iso2", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("varchar",  "iso2country", "address", "dbo", true)]
    [InlineData("nvarchar", "countryiso", "location", "person", true)]
    [InlineData("char", "countrycode", "customer", "customer", true)]
    public void IsMatch_WithExactColumnNameAndLength2_ShouldReturnTrue(
        string dataType, string columnName, string tableName, string schemaName, bool expected)
    {
        var column = new ColumnModel { DataType = dataType, MaxLength = "2", ColumnName = columnName };
        var table = new TableModel { TableName = tableName, SchemaName = schemaName };

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("varchar", "countryiso2", "address", "dbo", true)]
    [InlineData("nvarchar", "iso2country", "location", "person", true)]
    public void IsMatch_WithExactColumnNameButNotLength2_ShouldReturnTrue(
        string dataType, string columnName, string tableName, string schemaName, bool expected)
    {
        var column = new ColumnModel { DataType = dataType, MaxLength = "50", ColumnName = columnName };
        var table = new TableModel { TableName = tableName, SchemaName = schemaName };

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("varchar", "customer_iso", "address", "dbo", true)]
    [InlineData("char", "country_name", "location", "person", true)]
    public void IsMatch_WithPartialColumnNameAndLength2_ShouldReturnTrue(
        string dataType, string columnName, string tableName, string schemaName, bool expected)
    {
        var column = new ColumnModel { DataType = dataType, MaxLength = "2", ColumnName = columnName };
        var table = new TableModel { TableName = tableName, SchemaName = schemaName };

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("int", "countryiso2", "address", "dbo", false)]
    [InlineData("bigint", "iso2", "customer", "person", false)]
    [InlineData("datetime", "country", "location", "geo", false)]
    public void IsMatch_WithNonStringDataType_ShouldReturnFalse(
        string dataType, string columnName, string tableName, string schemaName, bool expected)
    {
        var column = new ColumnModel { DataType = dataType, ColumnName = columnName };
        var table = new TableModel { TableName = tableName, SchemaName = schemaName };

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("varchar(50)", "random_column", "random_table", "dbo", false)]
    [InlineData("nvarchar(100)", "id", "products", "sales", false)]
    public void IsMatch_WithNoMatchingCriteria_ShouldReturnFalse(
        string dataType, string columnName, string tableName, string schemaName, bool expected)
    {
        var column = new ColumnModel { DataType = dataType, ColumnName = columnName };
        var table = new TableModel { TableName = tableName, SchemaName = schemaName };

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("varchar", "iso2", "address", "dbo", true)]
    [InlineData("char", "country", "customer", "person", true)]
    [InlineData("varchar", "code", "location", "geo", true)]
    public void IsMatch_WithPartialMatchAndRelevantContext_ShouldReturnTrue(
        string dataType, string columnName, string tableName, string schemaName, bool expected)
    {
        var column = new ColumnModel { DataType = dataType, ColumnName = columnName };
        var table = new TableModel { TableName = tableName, SchemaName = schemaName };

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsMatch_WithScoreExactly20_ShouldReturnTrue()
    {
        var column = new ColumnModel { DataType = "varchar", ColumnName = "iso" };
        var table = new TableModel { TableName = "location", SchemaName = "geo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithScoreLessThan20_ShouldReturnFalse()
    {
        var column = new ColumnModel { DataType = "varchar(50)", ColumnName = "iso" };
        var table = new TableModel { TableName = "random", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }
}