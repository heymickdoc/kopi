using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers.Special;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers.Special;

public class CommunitySpecialProductNameTests
{
    [Theory]
    [InlineData("dbo", "Product", true)]
    [InlineData("dbo", "Products", true)]
    [InlineData("dbo", "ProductCatalog", true)]
    [InlineData("sales", "Inventory", true)]
    [InlineData("warehouse", "Stock", true)]
    [InlineData("logistics", "Items", true)]
    public void IsMatch_ValidProductTables_ReturnsTrue(string schema, string table, bool expected)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        var result = CommunitySpecialProductName.IsMatch(tableModel);
        
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("contact", "Name", false)]
    [InlineData("user", "Product", false)]
    [InlineData("HumanResources", "Product", false)]
    [InlineData("hr", "Inventory", false)]
    [InlineData("employee", "Item", false)]
    [InlineData("auth", "Product", false)]
    public void IsMatch_InvalidSchemas_ReturnsFalse(string schema, string table, bool expected)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        var result = CommunitySpecialProductName.IsMatch(tableModel);
        
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("dbo", "Customer", false)]
    [InlineData("sales", "Order", false)]
    [InlineData("dbo", "Transaction", false)]
    public void IsMatch_NonProductTables_ReturnsFalse(string schema, string table, bool expected)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        var result = CommunitySpecialProductName.IsMatch(tableModel);
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsMatch_HumanResourcesSchema_ReturnsFalse()
    {
        var tableModel = new TableModel { SchemaName = "HumanResources", TableName = "Product" };
        
        var result = CommunitySpecialProductName.IsMatch(tableModel);
        
        Assert.False(result);
    }
}