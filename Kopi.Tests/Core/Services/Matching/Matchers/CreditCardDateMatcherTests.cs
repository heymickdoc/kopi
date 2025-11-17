using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CreditCardDateMatcherTests
{
    private readonly CommunityCreditCardDateMatcher _matcher = new();
    
    
    [Fact]
    public void IsMatch_ExactColumnNameCreditCardExpDate_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "creditcardexpdate", DataType = "nvarchar" };
        var table = new TableModel { TableName = "orders", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ColumnContainsCreditCardExpirationAndTableMatches_ReturnsTrue()
    {
        var column = new ColumnModel { ColumnName = "usercreditcardexpiration", DataType = "varchar" };
        var table = new TableModel { TableName = "paymentinfo", SchemaName = "dbo" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_TableAndSchemaButNoColumnMatch_ReturnsFalse()
    {
        var column = new ColumnModel { ColumnName = "cardtype", DataType = "nvarchar" };
        var table = new TableModel { TableName = "invoice", SchemaName = "finance" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }
}