using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityCreditCardDateMatcherTests
{
    private readonly CommunityCreditCardDateMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldBe10()
    {
        Assert.Equal(10, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldBeCreditCardDate()
    {
        Assert.Equal("credit_card_date", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("varchar", "dbo", "Payment", "CreditCardExpDate")]
    [InlineData("date", "dbo", "Payment", "CreditCardExpDate")]
    [InlineData("datetime", "dbo", "Transaction", "CCExpDate")]
    [InlineData("int", "dbo", "Customer", "ExpMonth")]
    [InlineData("int", "dbo", "Customer", "ExpYear")]
    public void IsMatch_StrongColumnNames_ShouldReturnTrue(string dataType, string schema, string table, string column)
    {
        var columnModel = new ColumnModel { ColumnName = column, DataType = dataType };
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        var result = _matcher.IsMatch(columnModel, tableModel);

        Assert.True(result);
    }

    [Theory]
    [InlineData("date", "dbo", "Payment", "ExpDate")]
    [InlineData("varchar", "dbo", "Transaction", "ExpirationDate")]
    [InlineData("datetime", "Finance", "Order", "Expiration")]
    public void IsMatch_GenericExpirationWithFinancialContext_ShouldReturnTrue(string dataType, string schema, string table, string column)
    {
        var columnModel = new ColumnModel { ColumnName = column, DataType = dataType };
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        var result = _matcher.IsMatch(columnModel, tableModel);

        Assert.True(result);
    }

    [Theory]
    [InlineData("date", "dbo", "Payment", "CardExpDate")]
    [InlineData("varchar", "dbo", "Customer", "CCExpiration")]
    public void IsMatch_ExpirationWithCardOrCC_ShouldReturnTrue(string dataType, string schema, string table, string column)
    {
        var columnModel = new ColumnModel { ColumnName = column, DataType = dataType };
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        var result = _matcher.IsMatch(columnModel, tableModel);

        Assert.True(result);
    }

    [Theory]
    [InlineData("date", "Production", "Inventory", "ExpDate")]
    [InlineData("date", "Product", "Items", "ExpirationDate")]
    [InlineData("varchar", "HumanResources", "Employee", "CertificationExpDate")]
    [InlineData("datetime", "Log", "System", "ExpDate")]
    public void IsMatch_InvalidSchemaNames_ShouldReturnFalse(string dataType, string schema, string table, string column)
    {
        var columnModel = new ColumnModel { ColumnName = column, DataType = dataType };
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        var result = _matcher.IsMatch(columnModel, tableModel);

        Assert.False(result);
    }

    [Theory]
    [InlineData("varchar", "dbo", "Payment", "CardNumber")]
    [InlineData("varchar", "dbo", "Customer", "CreditCardCVV")]
    [InlineData("varchar", "dbo", "Transaction", "CardType")]
    [InlineData("int", "dbo", "Customer", "CardID")]
    public void IsMatch_ExclusionWords_ShouldReturnFalse(string dataType, string schema, string table, string column)
    {
        var columnModel = new ColumnModel { ColumnName = column, DataType = dataType };
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        var result = _matcher.IsMatch(columnModel, tableModel);

        Assert.False(result);
    }

    [Theory]
    [InlineData("date", "dbo", "Employee", "ExpDate")]
    [InlineData("varchar", "dbo", "Product", "ExpirationDate")]
    public void IsMatch_GenericExpirationWithoutFinancialContext_ShouldReturnFalse(string dataType, string schema, string table, string column)
    {
        var columnModel = new ColumnModel { ColumnName = column, DataType = dataType };
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        var result = _matcher.IsMatch(columnModel, tableModel);

        Assert.False(result);
    }

    [Theory]
    [InlineData("decimal", "dbo", "Transaction", "CCExpDate")]
    [InlineData("float", "dbo", "Customer", "ExpMonth")]
    public void IsMatch_InvalidDataType_ShouldReturnFalse(string dataType, string schema, string table, string column)
    {
        var columnModel = new ColumnModel { ColumnName = column, DataType = dataType };
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        var result = _matcher.IsMatch(columnModel, tableModel);

        Assert.False(result);
    }

    [Theory]
    [InlineData("date", "dbo", "Payment", "CC_Exp_Date")]
    [InlineData("varchar", "dbo", "Transaction", "Card-Expiration")]
    public void IsMatch_NormalizedColumnNames_ShouldMatchWithSeparators(string dataType, string schema, string table, string column)
    {
        var columnModel = new ColumnModel { ColumnName = column, DataType = dataType };
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        var result = _matcher.IsMatch(columnModel, tableModel);

        Assert.True(result);
    }

    [Theory]
    [InlineData("date", "dbo", "Inventory", "BatchExpDate")]
    [InlineData("varchar", "dbo", "Product", "WarrantyExpDate")]
    public void IsMatch_ProductExpirationContext_ShouldReturnFalse(string dataType, string schema, string table, string column)
    {
        var columnModel = new ColumnModel { ColumnName = column, DataType = dataType };
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        var result = _matcher.IsMatch(columnModel, tableModel);

        Assert.False(result);
    }
}