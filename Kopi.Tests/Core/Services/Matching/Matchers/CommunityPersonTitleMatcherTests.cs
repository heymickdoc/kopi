using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityPersonTitleMatcherTests
{
    private readonly CommunityPersonTitleMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldBe23()
    {
        Assert.Equal(23, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldBePersonTitle()
    {
        Assert.Equal("person_title", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("decimal")]
    [InlineData("bigint")]
    public void IsMatch_NonStringDataType_ShouldReturnFalse(string dataType)
    {
        var column = new ColumnModel { ColumnName = "Salutation", DataType = dataType };
        var table = new TableModel { TableName = "Users", SchemaName = "dbo" };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("production")]
    [InlineData("inventory")]
    [InlineData("product")]
    [InlineData("cms")]
    public void IsMatch_InvalidSchemaName_ShouldReturnFalse(string schemaName)
    {
        var column = new ColumnModel { ColumnName = "Salutation", DataType = "nvarchar" };
        var table = new TableModel { TableName = "Users", SchemaName = schemaName };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("Salutation")]
    [InlineData("Honorific")]
    [InlineData("CourtesyTitle")]
    [InlineData("PersonTitle")]
    [InlineData("NameTitle")]
    [InlineData("TitleOfCourtesy")]
    public void IsMatch_StrongColumnNames_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "nvarchar" };
        var table = new TableModel { TableName = "Users", SchemaName = "dbo" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("JobTitle")]
    [InlineData("PageTitle")]
    [InlineData("BookTitle")]
    [InlineData("ProjectTitle")]
    [InlineData("WorkTitle")]
    public void IsMatch_ExclusionWords_ShouldReturnFalse(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "nvarchar" };
        var table = new TableModel { TableName = "Users", SchemaName = "dbo" };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_CourtesyTitle_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "CourtesyTitle", DataType = "nvarchar" };
        var table = new TableModel { TableName = "Contacts", SchemaName = "dbo" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_PersonTitle_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "PersonTitle", DataType = "varchar" };
        var table = new TableModel { TableName = "Members", SchemaName = "dbo" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("Title", "Users")]
    [InlineData("Title", "Customers")]
    [InlineData("Title", "Contacts")]
    [InlineData("Title", "Employees")]
    [InlineData("Title", "Members")]
    public void IsMatch_TitleWithPersonContext_ShouldReturnTrue(string columnName, string tableName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "nvarchar" };
        var table = new TableModel { TableName = tableName, SchemaName = "dbo" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_TitleWithoutPersonContext_ShouldReturnFalse()
    {
        var column = new ColumnModel { ColumnName = "Title", DataType = "nvarchar" };
        var table = new TableModel { TableName = "Products", SchemaName = "dbo" };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_TitleWithPersonContextInSchema_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "Title", DataType = "nvarchar" };
        var table = new TableModel { TableName = "Details", SchemaName = "Customer" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("salutation")]
    [InlineData("SALUTATION")]
    [InlineData("Salutation")]
    public void IsMatch_NormalizedStrongColumnNames_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "nvarchar" };
        var table = new TableModel { TableName = "AnyTable", SchemaName = "dbo" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_UnderscoreSeparatedCourtesyTitle_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "Courtesy_Title", DataType = "nvarchar" };
        var table = new TableModel { TableName = "Users", SchemaName = "dbo" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_HyphenSeparatedPersonTitle_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "Person-Title", DataType = "varchar" };
        var table = new TableModel { TableName = "Contacts", SchemaName = "dbo" };

        Assert.True(_matcher.IsMatch(column, table));
    }
}