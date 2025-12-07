using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityPersonFullnameMatcherTests
{
    private readonly CommunityPersonFullnameMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldBe20()
    {
        Assert.Equal(20, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldBeFullName()
    {
        Assert.Equal("person_fullname", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("nvarchar")]
    [InlineData("varchar")]
    [InlineData("text")]
    public void IsMatch_WithStringDataType_ShouldNotRejectImmediately(string dataType)
    {
        var column = new ColumnModel { ColumnName = "FullName", DataType = dataType };
        var table = new TableModel { SchemaName = "dbo", TableName = "Customer" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("bigint")]
    [InlineData("datetime")]
    public void IsMatch_WithNonStringDataType_ShouldReturnFalse(string dataType)
    {
        var column = new ColumnModel { ColumnName = "FullName", DataType = dataType };
        var table = new TableModel { SchemaName = "dbo", TableName = "Customer" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("FullName")]
    [InlineData("full_name")]
    [InlineData("CompleteName")]
    [InlineData("PersonName")]
    [InlineData("DisplayName")]
    public void IsMatch_WithStrongColumnNames_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "nvarchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "SomeTable" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("production")]
    [InlineData("inventory")]
    [InlineData("product")]
    [InlineData("log")]
    [InlineData("system")]
    public void IsMatch_WithInvalidSchemaNames_ShouldReturnFalse(string schemaName)
    {
        var column = new ColumnModel { ColumnName = "FullName", DataType = "nvarchar" };
        var table = new TableModel { SchemaName = schemaName, TableName = "Customer" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("FirstName")]
    [InlineData("LastName")]
    [InlineData("MiddleName")]
    [InlineData("UserName")]
    [InlineData("FileName")]
    [InlineData("ProductName")]
    public void IsMatch_WithExclusionWords_ShouldReturnFalse(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "nvarchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Customer" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("CustomerName")]
    [InlineData("EmployeeName")]
    [InlineData("PersonName")]
    [InlineData("ContactName")]
    public void IsMatch_WithPersonContextInColumnName_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "nvarchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "SomeTable" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Person")]
    [InlineData("Employee")]
    [InlineData("Contact")]
    [InlineData("User")]
    public void IsMatch_WithPersonTableContext_AndNameColumn_ShouldReturnTrue(string tableName)
    {
        var column = new ColumnModel { ColumnName = "Name", DataType = "nvarchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = tableName };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Product")]
    [InlineData("Item")]
    [InlineData("Category")]
    public void IsMatch_WithoutPersonTableContext_AndNameColumn_ShouldReturnFalse(string tableName)
    {
        var column = new ColumnModel { ColumnName = "Name", DataType = "nvarchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = tableName };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_WithPersonSchemaContext_AndNameColumn_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "Name", DataType = "nvarchar" };
        var table = new TableModel { SchemaName = "Customer", TableName = "Details" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ComplexScenario_CustomerTableWithFullName_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "full_name", DataType = "varchar", MaxLength = "200"};
        var table = new TableModel { SchemaName = "sales", TableName = "customers" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }
}