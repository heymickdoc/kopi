using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityPhoneNumberMatcherTests
{
    private readonly CommunityPhoneNumberMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldBe15()
    {
        Assert.Equal(15, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldBePhoneNumber()
    {
        Assert.Equal("phone_number", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("Phone", "varchar", "25")]
    [InlineData("PhoneNumber", "nvarchar", "25")]
    [InlineData("Telephone", "varchar", "25")]
    [InlineData("Mobile", "varchar", "25")]
    [InlineData("MobilePhone", "varchar", "25")]
    [InlineData("HomePhone", "varchar", "25")]
    [InlineData("WorkPhone", "varchar", "25")]
    [InlineData("Fax", "varchar", "25")]
    [InlineData("Tel", "varchar", "25")]
    public void IsMatch_StrongColumnNames_ReturnsTrue(string columnName, string dataType, string maxLength)
    {
        var column = CreateColumn(columnName, dataType, maxLength);
        var table = CreateTable("Customers");

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Microphone")]
    [InlineData("Headphone")]
    [InlineData("PhoneID")]
    [InlineData("PhoneType")]
    [InlineData("PhoneProvider")]
    public void IsMatch_ExclusionWords_ReturnsFalse(string columnName)
    {
        var column = CreateColumn(columnName, "varchar", "25");
        var table = CreateTable("Products");

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_InvalidSchemaNames_ReturnsFalse()
    {
        var column = CreateColumn("Phone", "varchar", "20");
        var table = CreateTable("Items", "production");

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("bigint")]
    [InlineData("decimal")]
    [InlineData("datetime")]
    public void IsMatch_NonStringDataType_ReturnsFalse(string dataType)
    {
        var column = CreateColumn("Phone", dataType);
        var table = CreateTable("Customers");

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_TooShortLength_ReturnsFalse()
    {
        var column = CreateColumn("Phone", "varchar", "5");
        var table = CreateTable("Customers");

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("Cell", "Customers", true)]
    [InlineData("Cell", "Products", false)]
    [InlineData("CellPhone", "Products", true)]
    public void IsMatch_CellColumn_RequiresContextOrPhoneToken(string columnName, string tableName, bool expected)
    {
        var column = CreateColumn(columnName, "varchar", "20");
        var table = CreateTable(tableName);

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("user_phone", "varchar", "20", true)]
    [InlineData("customer_mobile", "varchar", "15", true)]
    [InlineData("employee_fax", "varchar", "25", true)]
    [InlineData("contact_tel", "varchar", "20", true)]
    public void IsMatch_ContactContextWithPhoneColumns_ReturnsTrue(string columnName, string dataType, string maxLength, bool expected)
    {
        var column = CreateColumn(columnName, dataType, maxLength);
        var table = CreateTable("Users");

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Phone_Number", "varchar", "20", true)]
    [InlineData("phone-number", "varchar", "20", true)]
    [InlineData("PHONENUMBER", "varchar", "20", true)]
    public void IsMatch_NormalizedColumnNames_ReturnsTrue(string columnName, string dataType, string maxLength, bool expected)
    {
        var column = CreateColumn(columnName, dataType, maxLength);
        var table = CreateTable("Customers");

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsMatch_AudioSchema_ReturnsFalse()
    {
        var column = CreateColumn("Phone", "varchar", "20");
        var table = CreateTable("Devices", "audio");

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_UnlimitedVarchar_ReturnsTrue()
    {
        var column = CreateColumn("Phone", "varchar", "4000");
        var table = CreateTable("Customers");

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    private ColumnModel CreateColumn(string name, string dataType, string maxLength = "255")
    {
        return new ColumnModel
        {
            ColumnName = name,
            DataType = dataType,
            MaxLength = maxLength
        };
    }

    private TableModel CreateTable(string tableName, string schemaName = "dbo")
    {
        return new TableModel
        {
            TableName = tableName,
            SchemaName = schemaName
        };
    }
}