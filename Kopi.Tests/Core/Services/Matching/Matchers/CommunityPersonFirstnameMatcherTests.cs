using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityPersonFirstnameMatcherTests
{
    private readonly CommunityPersonFirstnameMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldBe25()
    {
        Assert.Equal(25, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldBeFirstName()
    {
        Assert.Equal("first_name", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("FirstName", "nvarchar", "50", true)]
    [InlineData("FName", "varchar", "100", true)]
    [InlineData("GivenName", "nvarchar", "50", true)]
    [InlineData("Forename", "varchar", "255", true)]
    [InlineData("ChristianName", "nvarchar", "100", true)]
    [InlineData("First", "varchar", "50", true)]
    public void IsMatch_StrongColumnNames_ShouldReturnTrue(string columnName, string dataType, string maxLength, bool expected)
    {
        var column = CreateColumn(columnName, dataType, maxLength);
        var table = CreateTable("Users", "dbo");

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("First_Name", "varchar", true)]
    [InlineData("FirstNm", "nvarchar", true)]
    [InlineData("Given_Name", "varchar", true)]
    [InlineData("Fore_Name", "nvarchar", true)]
    public void IsMatch_TokenizedMatches_ShouldReturnTrue(string columnName, string dataType, bool expected)
    {
        var column = CreateColumn(columnName, dataType, "50");
        var table = CreateTable("Customers", "dbo");

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("LastName", "varchar", "50", false)]
    [InlineData("MiddleName", "nvarchar", "100", false)]
    [InlineData("FirstInitial", "char", "1", false)]
    [InlineData("FullName", "varchar", "200", false)]
    [InlineData("SurName", "nvarchar", "100", false)]
    [InlineData("FamilyName", "varchar", "100", false)]
    public void IsMatch_ExclusionWords_ShouldReturnFalse(string columnName, string dataType, string maxLength, bool expected)
    {
        var column = CreateColumn(columnName, dataType, maxLength);
        var table = CreateTable("Users", "dbo");

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsMatch_SingleCharacterMaxLength_ShouldReturnFalse()
    {
        var column = CreateColumn("FirstName", "char(1)");
        var table = CreateTable("Users", "dbo");

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("int", false)]
    [InlineData("bigint", false)]
    [InlineData("decimal", false)]
    [InlineData("datetime", false)]
    [InlineData("bit", false)]
    public void IsMatch_NonStringDataTypes_ShouldReturnFalse(string dataType, bool expected)
    {
        var column = CreateColumn("FirstName", dataType);
        var table = CreateTable("Users", "dbo");

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Production", false)]
    [InlineData("Inventory", false)]
    [InlineData("Product", false)]
    [InlineData("Log", false)]
    [InlineData("System", false)]
    [InlineData("Error", false)]
    [InlineData("Auth", false)]
    [InlineData("Config", false)]
    [InlineData("Setting", false)]
    public void IsMatch_InvalidSchemaNames_ShouldReturnFalse(string schemaName, bool expected)
    {
        var column = CreateColumn("FirstName", "varchar(50)");
        var table = CreateTable("SomeTable", schemaName);

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("first-name", "varchar", true)]
    [InlineData("first_name", "nvarchar", true)]
    [InlineData("FIRSTNAME", "varchar", true)]
    [InlineData("FiRsTnAmE", "nvarchar", true)]
    public void IsMatch_NormalizedColumnNames_ShouldReturnTrue(string columnName, string dataType, bool expected)
    {
        var column = CreateColumn(columnName, dataType, "75");
        var table = CreateTable("Contacts", "dbo");

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Name", "varchar", false)]
    [InlineData("Title", "nvarchar", false)]
    [InlineData("Description", "varchar", false)]
    public void IsMatch_GenericColumnNames_ShouldReturnFalse(string columnName, string dataType, bool expected)
    {
        var column = CreateColumn(columnName, dataType, "100");
        var table = CreateTable("Users", "dbo");

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    private ColumnModel CreateColumn(string columnName, string dataType, string maxLength = "0")
    {
        return new ColumnModel
        {
            ColumnName = columnName,
            DataType = dataType,
            MaxLength = maxLength == "0" ? "255" : maxLength
        };
    }

    private TableModel CreateTable(string tableName, string schemaName)
    {
        return new TableModel
        {
            TableName = tableName,
            SchemaName = schemaName
        };
    }
}