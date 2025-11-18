using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers.Special;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers.Special;

public class CommunitySpecialCategoryNameTests
{
    [Theory]
    [InlineData("dbo", "ProductCategory", true)]
    [InlineData("dbo", "CustomerType", true)]
    [InlineData("sales", "AssetGroup", true)]
    [InlineData("core", "AnimalKind", true)]
    [InlineData("main", "AssetClass", true)]
    [InlineData("data", "MarketSegment", true)]
    [InlineData("dbo", "Classification", true)]
    public void IsMatch_ValidCategoryTables_ReturnsTrue(string schema, string table, bool expected)
    {
        // Arrange
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        // Act
        var result = CommunitySpecialCategoryName.IsMatch(tableModel);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("log", "ProductCategory", false)]
    [InlineData("logging", "CustomerType", false)]
    [InlineData("audit", "AssetGroup", false)]
    [InlineData("history", "Classification", false)]
    [InlineData("backup", "MarketSegment", false)]
    [InlineData("sys", "ProductType", false)]
    [InlineData("system", "AnimalKind", false)]
    public void IsMatch_InvalidSchemaNames_ReturnsFalse(string schema, string table, bool expected)
    {
        // Arrange
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        // Act
        var result = CommunitySpecialCategoryName.IsMatch(tableModel);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("dbo", "Product", false)]
    [InlineData("dbo", "Customer", false)]
    [InlineData("sales", "Order", false)]
    [InlineData("core", "User", false)]
    public void IsMatch_TablesWithoutCategoryKeywords_ReturnsFalse(string schema, string table, bool expected)
    {
        // Arrange
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        // Act
        var result = CommunitySpecialCategoryName.IsMatch(tableModel);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("dbo", "ProductCategories", true)]
    [InlineData("dbo", "CustomerTypes", true)]
    [InlineData("sales", "AssetGroups", true)]
    public void IsMatch_PluralTableNames_ReturnsTrue(string schema, string table, bool expected)
    {
        // Arrange
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        // Act
        var result = CommunitySpecialCategoryName.IsMatch(tableModel);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("dbo", "CategoryProduct", true)]
    [InlineData("dbo", "TypeCustomer", true)]
    [InlineData("sales", "GroupAsset", true)]
    public void IsMatch_CategoryKeywordAtStart_ReturnsTrue(string schema, string table, bool expected)
    {
        // Arrange
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        // Act
        var result = CommunitySpecialCategoryName.IsMatch(tableModel);

        // Assert
        Assert.Equal(expected, result);
    }
}