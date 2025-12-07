using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityPersonDateOfBirthMatcherTests
{
    private readonly CommunityPersonDateOfBirthMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldBe25()
    {
        Assert.Equal(25, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldBePersonDob()
    {
        Assert.Equal("person_dob", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("varchar")]
    [InlineData("int")]
    [InlineData("nvarchar")]
    public void IsMatch_WithNonDateType_ShouldReturnFalse(string dataType)
    {
        var column = new ColumnModel { ColumnName = "DateOfBirth", DataType = dataType };
        var table = new TableModel { TableName = "Users", SchemaName = "dbo" };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("DateOfBirth")]
    [InlineData("dob")]
    [InlineData("BirthDate")]
    [InlineData("birth_date")]
    [InlineData("DOBirth")]
    [InlineData("d_birth")]
    public void IsMatch_WithStrongColumnName_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "date" };
        var table = new TableModel { TableName = "Users", SchemaName = "dbo" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("production")]
    [InlineData("inventory")]
    [InlineData("product")]
    [InlineData("log")]
    public void IsMatch_WithInvalidSchema_ShouldReturnFalse(string schemaName)
    {
        var column = new ColumnModel { ColumnName = "DateOfBirth", DataType = "date" };
        var table = new TableModel { TableName = "Users", SchemaName = schemaName };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("age")]
    [InlineData("created")]
    [InlineData("modified")]
    [InlineData("updated")]
    [InlineData("dateadded")]
    public void IsMatch_WithExclusionWords_ShouldReturnFalse(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "date" };
        var table = new TableModel { TableName = "Users", SchemaName = "dbo" };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("Users", "birth")]
    [InlineData("Customers", "BirthYear")]
    [InlineData("Employees", "birth_info")]
    public void IsMatch_WithPersonTableAndBirthColumn_ShouldReturnTrue(string tableName, string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "date" };
        var table = new TableModel { TableName = tableName, SchemaName = "dbo" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("Orders", "birth")]
    public void IsMatch_WithNonPersonTableAndBirthColumn_ShouldReturnFalse(string tableName, string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "date" };
        var table = new TableModel { TableName = tableName, SchemaName = "dbo" };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_WithPersonSchemaAndBirthColumn_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "birth", DataType = "datetime" };
        var table = new TableModel { TableName = "Data", SchemaName = "person" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_WithMixedCaseStrongColumnName_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "DATE_OF_BIRTH", DataType = "date" };
        var table = new TableModel { TableName = "SomeTable", SchemaName = "dbo" };

        Assert.True(_matcher.IsMatch(column, table));
    }
}