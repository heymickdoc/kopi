using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityEmailAddressMatcherTests
{
    private readonly CommunityEmailAddressMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldBe20()
    {
        Assert.Equal(20, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldBeEmailAddress()
    {
        Assert.Equal("email_address", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("email", "nvarchar", "255")]
    [InlineData("EmailAddress", "varchar", "100")]
    [InlineData("user_email", "nvarchar", "max")]
    [InlineData("contactemail", "varchar", "200")]
    public void IsMatch_ShouldReturnTrue_ForStrongEmailColumns(string columnName, string dataType, string maxLength)
    {
        var column = CreateColumn(columnName, dataType, maxLength);
        var table = CreateTable("dbo", "Users");

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("EmailBody")]
    [InlineData("EmailSubject")]
    [InlineData("EmailContent")]
    [InlineData("email_message")]
    [InlineData("EmailSentDate")]
    [InlineData("EmailStatus")]
    public void IsMatch_ShouldReturnFalse_ForExcludedEmailColumns(string columnName)
    {
        var column = CreateColumn(columnName, "nvarchar", "max");
        var table = CreateTable("dbo", "Users");

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("int")]
    [InlineData("bigint")]
    [InlineData("bit")]
    [InlineData("datetime")]
    public void IsMatch_ShouldReturnFalse_ForNonStringDataTypes(string dataType)
    {
        var column = CreateColumn("Email", dataType);
        var table = CreateTable("dbo", "Users");

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("production")]
    [InlineData("inventory")]
    [InlineData("log")]
    [InlineData("system")]
    public void IsMatch_ShouldReturnFalse_ForInvalidSchemas(string schemaName)
    {
        var column = CreateColumn("Email", "nvarchar(255)");
        var table = CreateTable(schemaName, "Users");

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("PrimaryEmail")]
    [InlineData("SecondaryEmail")]
    [InlineData("PersonalEmail")]
    [InlineData("WorkEmail")]
    public void IsMatch_ShouldReturnTrue_ForSpecificEmailTypes(string columnName)
    {
        var column = CreateColumn(columnName, "varchar", "250");
        var table = CreateTable("dbo", "Contacts");

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_ShouldReturnTrue_ForNormalizedColumnNames()
    {
        var column = CreateColumn("email_address", "nvarchar", "150");
        var table = CreateTable("dbo", "Users");

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("EmailId")]
    [InlineData("EmailKey")]
    public void IsMatch_ShouldReturnFalse_ForIdOrKeyColumns(string columnName)
    {
        var column = CreateColumn(columnName, "nvarchar", "50");
        var table = CreateTable("dbo", "Users");

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_ShouldReturnTrue_ForEmailInPersonContext()
    {
        var column = CreateColumn("Email", "varchar", "255");
        var table = CreateTable("dbo", "Customers");

        Assert.True(_matcher.IsMatch(column, table));
    }
    
    [Fact]
    public void IsMatch_ShouldReturnFalse_ForShortMaxLength()
    {
        var column = CreateColumn("Email", "varchar", "4");
        var table = CreateTable("dbo", "Users");
        Assert.False(_matcher.IsMatch(column, table));
    }

    private static ColumnModel CreateColumn(string name, string dataType, string maxLength = "0")
    {
        return new ColumnModel
        {
            ColumnName = name,
            DataType = dataType,
            MaxLength = maxLength
        };
    }

    private static TableModel CreateTable(string schema, string tableName)
    {
        return new TableModel
        {
            SchemaName = schema,
            TableName = tableName
        };
    }
}