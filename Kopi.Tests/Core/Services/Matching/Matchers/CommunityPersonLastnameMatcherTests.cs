using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityPersonLastnameMatcherTests
{
    private readonly CommunityPersonLastnameMatcher _matcher = new();
    
    [Fact]
    public void Priority_Returns25()
    {
        Assert.Equal(25, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ReturnsLastName()
    {
        Assert.Equal("person_lastname", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("LastName", "nvarchar", "100", "dbo", "Users")]
    [InlineData("Surname", "varchar", "50", "dbo", "Customers")]
    [InlineData("FamilyName", "nvarchar", "50", "dbo", "Contacts")]
    [InlineData("LName", "varchar", "75", "dbo", "Employees")]
    [InlineData("Last", "nvarchar", "100", "dbo", "Members")]
    public void IsMatch_StrongColumnNames_ReturnsTrue(string columnName, string dataType, string maxLength, string schema, string table)
    {
        var column = CreateColumn(columnName, dataType, maxLength);
        var tableContext = CreateTable(schema, table);

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Cust_Last_Name", "nvarchar", "75", "dbo", "Customers")]
    [InlineData("Contact_Last_Nm", "varchar", "100", "dbo", "Contacts")]
    [InlineData("User_Last_Name", "nvarchar", "50", "dbo", "Users")]
    public void IsMatch_LastNameTokens_ReturnsTrue(string columnName, string dataType, string maxLength, string schema, string table)
    {
        var column = CreateColumn(columnName, dataType, maxLength);
        var tableContext = CreateTable(schema, table);

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Family_Name", "nvarchar", "50", "dbo", "Persons")]
    [InlineData("FamilyName", "varchar", "100", "dbo", "Profiles")]
    public void IsMatch_FamilyNameTokens_ReturnsTrue(string columnName, string dataType, string maxLength, string schema, string table)
    {
        var column = CreateColumn(columnName, dataType, maxLength);
        var tableContext = CreateTable(schema, table);

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Theory]
    [InlineData("LastLogin", "datetime", "dbo", "Users")]
    [InlineData("LastModified", "datetime", "dbo", "Customers")]
    [InlineData("LastSeen", "datetime", "dbo", "Members")]
    [InlineData("LastUpdated", "datetime", "dbo", "Profiles")]
    [InlineData("LastChanged", "datetime", "dbo", "Accounts")]
    public void IsMatch_ExclusionWords_ReturnsFalse(string columnName, string dataType, string schema, string table)
    {
        var column = CreateColumn(columnName, dataType, "100");
        var tableContext = CreateTable(schema, table);

        var result = _matcher.IsMatch(column, tableContext);

        Assert.False(result);
    }

    [Theory]
    [InlineData("FirstLastName", "nvarchar", "100", "dbo", "Users")]
    [InlineData("FullName", "nvarchar", "200", "dbo", "Customers")]
    [InlineData("LastInitial", "char", "1", "dbo", "Contacts")]
    [InlineData("MiddleName", "nvarchar", "50", "dbo", "Persons")]
    public void IsMatch_ConflictingWords_ReturnsFalse(string columnName, string dataType, string maxLength, string schema, string table)
    {
        var column = CreateColumn(columnName, dataType, maxLength);
        var tableContext = CreateTable(schema, table);

        var result = _matcher.IsMatch(column, tableContext);

        Assert.False(result);
    }

    [Theory]
    [InlineData("LastName", "int", "dbo", "Users")]
    [InlineData("Surname", "bigint", "dbo", "Customers")]
    [InlineData("LName", "decimal", "dbo", "Contacts")]
    public void IsMatch_NonStringDataType_ReturnsFalse(string columnName, string dataType, string schema, string table)
    {
        var column = CreateColumn(columnName, dataType, "0");
        var tableContext = CreateTable(schema, table);

        var result = _matcher.IsMatch(column, tableContext);

        Assert.False(result);
    }

    [Theory]
    [InlineData("LastName", "char", "dbo", "Users")]
    [InlineData("Surname", "varchar", "dbo", "Customers")]
    public void IsMatch_SingleCharLength_ReturnsFalse(string columnName, string dataType, string schema, string table)
    {
        var column = CreateColumn(columnName, dataType, "1");
        var tableContext = CreateTable(schema, table);

        var result = _matcher.IsMatch(column, tableContext);

        Assert.False(result);
    }

    [Theory]
    [InlineData("LastName", "nvarchar", "50", "production", "Items")]
    [InlineData("Surname", "varchar", "100", "inventory", "Products")]
    [InlineData("LName", "nvarchar", "75", "log", "Errors")]
    public void IsMatch_InvalidSchemaNames_ReturnsFalse(string columnName, string dataType, string maxLength, string schema, string table)
    {
        var column = CreateColumn(columnName, dataType, maxLength);
        var tableContext = CreateTable(schema, table);

        var result = _matcher.IsMatch(column, tableContext);

        Assert.False(result);
    }
    
    private static ColumnModel CreateColumn(string name, string dataType, string maxLength)
    {
        return new ColumnModel
        {
            ColumnName = name,
            DataType = dataType,
            MaxLength = maxLength
        };
    }

    private static TableModel CreateTable(string schema, string table)
    {
        return new TableModel
        {
            SchemaName = schema,
            TableName = table
        };
    }
}