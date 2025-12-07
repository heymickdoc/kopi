using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers.Special;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers.Special;

public class CommunitySpecialRegionNameTests
{
    [Theory]
    [InlineData("Sales", "Territory", 50, true)]
    [InlineData("Sales", "Region", 100, true)]
    [InlineData("Geo", "District", 30, true)]
    [InlineData("Location", "Zone", 25, true)]
    public void IsMatch_WithValidRegionTable_ReturnsTrue(string schema, string table, int maxLength, bool expected)
    {
        // Arrange
        var tableContext = new TableModel { SchemaName = schema, TableName = table };

        // Act
        var result = CommunitySpecialRegionName.IsMatch(tableContext, maxLength);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Sales", "Territory", 3)]
    [InlineData("Sales", "Region", 2)]
    [InlineData("Geo", "District", 1)]
    public void IsMatch_WithMaxLengthTooSmall_ReturnsFalse(string schema, string table, int maxLength)
    {
        // Arrange
        var tableContext = new TableModel { SchemaName = schema, TableName = table };

        // Act
        var result = CommunitySpecialRegionName.IsMatch(tableContext, maxLength);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("Production", "Territory", 50)]
    [InlineData("Inventory", "Region", 100)]
    [InlineData("Product", "District", 30)]
    [InlineData("Log", "Zone", 25)]
    [InlineData("Audit", "Territory", 40)]
    [InlineData("Finance", "Region", 60)]
    public void IsMatch_WithInvalidSchema_ReturnsFalse(string schema, string table, int maxLength)
    {
        // Arrange
        var tableContext = new TableModel { SchemaName = schema, TableName = table };

        // Act
        var result = CommunitySpecialRegionName.IsMatch(tableContext, maxLength);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("Sales", "Customer", 50)]
    [InlineData("Sales", "Order", 100)]
    [InlineData("Geo", "Country", 30)]
    public void IsMatch_WithNonRegionTable_ReturnsFalse(string schema, string table, int maxLength)
    {
        // Arrange
        var tableContext = new TableModel { SchemaName = schema, TableName = table };

        // Act
        var result = CommunitySpecialRegionName.IsMatch(tableContext, maxLength);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("SalesTerritory", "Territory", 50, true)]
    [InlineData("Sales_Territory", "TerritoryRegion", 40, true)]
    public void IsMatch_WithCompoundWords_HandlesCorrectly(string schema, string table, int maxLength, bool expected)
    {
        // Arrange
        var tableContext = new TableModel { SchemaName = schema, TableName = table };

        // Act
        var result = CommunitySpecialRegionName.IsMatch(tableContext, maxLength);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsMatch_WithNullSchema_DoesNotThrow()
    {
        // Arrange
        var tableContext = new TableModel { SchemaName = null, TableName = "Territory" };

        // Act
        var result = CommunitySpecialRegionName.IsMatch(tableContext, 50);

        // Assert
        Assert.True(result);
    }
}