using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityPersonSuffixMatcherTests
{
    private readonly CommunityPersonSuffixMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldBe23()
    {
        Assert.Equal(23, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldBePersonSuffix()
    {
        Assert.Equal("person_suffix", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("bigint")]
    [InlineData("decimal")]
    [InlineData("datetime")]
    public void IsMatch_NonStringDataType_ReturnsFalse(string dataType)
    {
        var column = CreateColumn("Suffix", dataType);
        var table = CreateTable("dbo", "Person");

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("varchar", "20")]
    [InlineData("varchar", "100")]
    [InlineData("nvarchar", "50")]
    public void IsMatch_StringTooLong_ReturnsFalse(string dataType, string maxLength)
    {
        var column = CreateColumn("Suffix", dataType, maxLength);
        var table = CreateTable("dbo", "Person");

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("varchar", "10")]
    [InlineData("varchar", "15")]
    [InlineData("nvarchar", "5")]
    public void IsMatch_ValidStringLength_Passes(string dataType, string maxLength)
    {
        var column = CreateColumn("Suffix", dataType, maxLength);
        var table = CreateTable("dbo", "Person");

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("production")]
    [InlineData("inventory")]
    [InlineData("product")]
    [InlineData("log")]
    [InlineData("system")]
    [InlineData("file")]
    [InlineData("document")]
    public void IsMatch_InvalidSchemaName_ReturnsFalse(string schemaName)
    {
        var column = CreateColumn("Suffix", "varchar", "10");
        var table = CreateTable(schemaName, "Person");

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("FileSuffix")]
    [InlineData("PathSuffix")]
    [InlineData("UrlSuffix")]
    [InlineData("DomainSuffix")]
    [InlineData("StreetSuffix")]
    [InlineData("AddressSuffix")]
    [InlineData("AccountSuffix")]
    [InlineData("CardSuffix")]
    public void IsMatch_ExclusionWords_ReturnsFalse(string columnName)
    {
        var column = CreateColumn(columnName, "varchar", "10");
        var table = CreateTable("dbo", "Data");

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("Suffix")]
    [InlineData("PersonSuffix")]
    [InlineData("NameSuffix")]
    [InlineData("CourtesySuffix")]
    [InlineData("GenerationSuffix")]
    [InlineData("person_suffix")]
    [InlineData("name-suffix")]
    public void IsMatch_StrongColumnNames_WithPersonContext_ReturnsTrue(string columnName)
    {
        var column = CreateColumn(columnName, "varchar", "10");
        var table = CreateTable("dbo", "Person");

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_GenericSuffix_WithoutPersonContext_ReturnsFalse()
    {
        var column = CreateColumn("Suffix", "varchar", "10");
        var table = CreateTable("dbo", "Product");

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Customer")]
    [InlineData("Contact")]
    [InlineData("Employee")]
    [InlineData("Staff")]
    [InlineData("Member")]
    [InlineData("Client")]
    [InlineData("Profile")]
    public void IsMatch_PersonTableContext_ReturnsTrue(string tableName)
    {
        var column = CreateColumn("Suffix", "varchar", "10");
        var table = CreateTable("dbo", tableName);

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("NameSuffix")]
    [InlineData("Name_Suffix")]
    [InlineData("FullNameSuffix")]
    public void IsMatch_NamePlusSuffix_ReturnsTrue(string columnName)
    {
        var column = CreateColumn(columnName, "varchar", "10");
        var table = CreateTable("dbo", "Data");

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("PersonSuffix")]
    [InlineData("Person_Suffix")]
    public void IsMatch_PersonPlusSuffix_ReturnsTrue(string columnName)
    {
        var column = CreateColumn(columnName, "varchar", "10");
        var table = CreateTable("dbo", "Data");

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_GenericSuffix_WithPersonSchemaContext_ReturnsTrue()
    {
        var column = CreateColumn("Suffix", "varchar", "10");
        var table = CreateTable("Customer", "Data");

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_RealWorldExample_CustomerSuffix_ReturnsTrue()
    {
        var column = CreateColumn("Suffix", "varchar", "5");
        var table = CreateTable("sales", "Customer");

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_RealWorldExample_ProductFileSuffix_ReturnsFalse()
    {
        var column = CreateColumn("FileSuffix", "varchar", "10");
        var table = CreateTable("inventory", "Product");

        Assert.False(_matcher.IsMatch(column, table));
    }

    private static ColumnModel CreateColumn(string name, string dataType, string maxLength = "255")
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