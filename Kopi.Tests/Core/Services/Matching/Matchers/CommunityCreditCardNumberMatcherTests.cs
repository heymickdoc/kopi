using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Kopi.Core.Utilities;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityCreditCardNumberMatcherTests
{
    private readonly CommunityCreditCardNumberMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldBe10()
    {
        Assert.Equal(10, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldBeCreditCardNumber()
    {
        Assert.Equal("credit_card_number", _matcher.GeneratorTypeKey);
    }

    #region Capacity Tests

    [Theory]
    [InlineData("varchar", 16, true)]
    [InlineData("varchar", 19, true)]
    [InlineData("varchar", 13, true)]
    [InlineData("varchar", 12, false)]
    [InlineData("varchar", 4, false)]
    [InlineData("nvarchar", 16, true)]
    [InlineData("char", 16, true)]
    public void IsMatch_StringTypes_ValidatesCapacity(string dataType, int maxLength, bool shouldMatch)
    {
        var column = CreateColumn("CreditCardNumber", dataType, maxLength: maxLength);
        var table = CreateTable("Payment");

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(shouldMatch, result);
    }

    [Theory]
    [InlineData(16, 0, true)]
    [InlineData(19, 0, true)]
    [InlineData(13, 0, true)]
    [InlineData(12, 0, false)]
    [InlineData(16, 2, false)] // Has decimals
    public void IsMatch_NumericTypes_ValidatesCapacityAndScale(int precision, int scale, bool shouldMatch)
    {
        var column = CreateColumn("CreditCardNumber", "numeric", precision: precision, scale: scale);
        var table = CreateTable("Payment");

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(shouldMatch, result);
    }

    [Theory]
    [InlineData("bigint", true)]
    [InlineData("int", false)]
    [InlineData("smallint", false)]
    [InlineData("tinyint", false)]
    public void IsMatch_IntegerTypes_OnlyBigIntIsValid(string dataType, bool shouldMatch)
    {
        var column = CreateColumn("CreditCardNumber", dataType);
        var table = CreateTable("Payment");

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(shouldMatch, result);
    }

    #endregion

    #region Strong Column Name Tests

    [Theory]
    [InlineData("CreditCardNumber")]
    [InlineData("CreditCardNum")]
    [InlineData("CCNumber")]
    [InlineData("CCNum")]
    [InlineData("CardNumber")]
    [InlineData("CCN")]
    [InlineData("PAN")]
    [InlineData("credit_card_number")]
    [InlineData("Credit-Card-Number")]
    public void IsMatch_StrongColumnNames_ReturnsTrue(string columnName)
    {
        var column = CreateColumn(columnName, "varchar", maxLength: 19);
        var table = CreateTable("Payment");

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    #endregion

    #region Token Analysis Tests

    [Theory]
    [InlineData("Card_Number")]
    [InlineData("CardNum")]
    [InlineData("Card_Num")]
    public void IsMatch_CardWithNumber_ReturnsTrue(string columnName)
    {
        var column = CreateColumn(columnName, "varchar", maxLength: 19);
        var table = CreateTable("Payment");

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    #endregion

    #region Financial Context Tests

    [Theory]
    [InlineData("Payment")]
    [InlineData("Transaction")]
    [InlineData("Order")]
    [InlineData("Invoice")]
    [InlineData("Customer")]
    [InlineData("Billing")]
    [InlineData("Sale")]
    [InlineData("Finance")]
    [InlineData("CreditCard")]
    [InlineData("Wallet")]
    public void IsMatch_PANWithFinancialContext_ReturnsTrue(string tableName)
    {
        var column = CreateColumn("PAN", "varchar", maxLength: 19);
        var table = CreateTable(tableName);

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_PANWithoutFinancialContext_ReturnsFalse()
    {
        var column = CreateColumn("PAN", "varchar", maxLength: 19);
        var table = CreateTable("Product");

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_CreditCardWithFinancialContext_ReturnsTrue()
    {
        var column = CreateColumn("CreditCard", "varchar", maxLength: 19);
        var table = CreateTable("Payment");

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("CC")]
    [InlineData("CreditCard")]
    public void IsMatch_AmbiguousNameWithFinancialContext_ReturnsTrue(string columnName)
    {
        var column = CreateColumn(columnName, "varchar", maxLength: 19);
        var table = CreateTable("Payment");

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    #endregion

    #region Exclusion Tests

    [Theory]
    [InlineData("CardType")]
    [InlineData("CardCode")]
    [InlineData("CardCVV")]
    [InlineData("CardCVC")]
    [InlineData("CardExp")]
    [InlineData("CardExpDate")]
    [InlineData("CardName")]
    [InlineData("CardHolder")]
    [InlineData("CardID")]
    [InlineData("CardKey")]
    [InlineData("CardLast4")]
    [InlineData("CardSuffix")]
    public void IsMatch_ExclusionWords_ReturnsFalse(string columnName)
    {
        var column = CreateColumn(columnName, "varchar", maxLength: 19);
        var table = CreateTable("Payment");

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    #endregion

    #region Invalid Schema Tests

    [Theory]
    [InlineData("Production")]
    [InlineData("Inventory")]
    [InlineData("Product")]
    [InlineData("Log")]
    [InlineData("System")]
    [InlineData("Error")]
    [InlineData("Auth")]
    [InlineData("HR")]
    [InlineData("HumanResources")]
    public void IsMatch_InvalidSchemaNames_ReturnsFalse(string schemaName)
    {
        var column = CreateColumn("CreditCardNumber", "varchar", maxLength: 19);
        var table = CreateTable("Payment", schemaName);

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    #endregion

    #region Helper Methods

    private ColumnModel CreateColumn(
        string name,
        string dataType,
        int maxLength = 0,
        int precision = 0,
        int scale = 0)
    {
        return new ColumnModel
        {
            ColumnName = name,
            DataType = dataType,
            MaxLength = maxLength.ToString(),
            NumericPrecision = (int)precision,
            NumericScale = scale
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

    #endregion
}