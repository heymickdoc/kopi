using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers.Special;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers.Special;

public class CommunitySpecialStateNameTests
{
    [Fact]
    public void IsMatch_ValidStateTable_ReturnsTrue()
    {
        // Arrange
        var table = new TableModel { SchemaName = "dbo", TableName = "State" };

        // Act
        var result = CommunitySpecialStateName.IsMatch(table, 50);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ValidProvinceTable_ReturnsTrue()
    {
        // Arrange
        var table = new TableModel { SchemaName = "dbo", TableName = "Province" };

        // Act
        var result = CommunitySpecialStateName.IsMatch(table, 50);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ValidTerritoryTable_ReturnsTrue()
    {
        // Arrange
        var table = new TableModel { SchemaName = "dbo", TableName = "Territory" };

        // Act
        var result = CommunitySpecialStateName.IsMatch(table, 50);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_MaxLengthTooSmall_ReturnsFalse()
    {
        // Arrange
        var table = new TableModel { SchemaName = "dbo", TableName = "State" };

        // Act
        var result = CommunitySpecialStateName.IsMatch(table, 2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_InvalidSchemaName_ReturnsFalse()
    {
        // Arrange
        var table = new TableModel { SchemaName = "production", TableName = "State" };

        // Act
        var result = CommunitySpecialStateName.IsMatch(table, 50);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_InvalidSchemaNameRaw_ReturnsFalse()
    {
        // Arrange
        var table = new TableModel { SchemaName = "pro_duction", TableName = "State" };

        // Act
        var result = CommunitySpecialStateName.IsMatch(table, 50);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_NonStateTable_ReturnsFalse()
    {
        // Arrange
        var table = new TableModel { SchemaName = "dbo", TableName = "Customer" };

        // Act
        var result = CommunitySpecialStateName.IsMatch(table, 50);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_PluralTableName_ReturnsTrue()
    {
        // Arrange
        var table = new TableModel { SchemaName = "dbo", TableName = "States" };

        // Act
        var result = CommunitySpecialStateName.IsMatch(table, 50);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_CompoundTableName_ReturnsTrue()
    {
        // Arrange
        var table = new TableModel { SchemaName = "Address", TableName = "StateProvince" };

        // Act
        var result = CommunitySpecialStateName.IsMatch(table, 50);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("inventory")]
    [InlineData("log")]
    [InlineData("audit")]
    [InlineData("finance")]
    public void IsMatch_VariousInvalidSchemas_ReturnsFalse(string schemaName)
    {
        // Arrange
        var table = new TableModel { SchemaName = schemaName, TableName = "State" };

        // Act
        var result = CommunitySpecialStateName.IsMatch(table, 50);

        // Assert
        Assert.False(result);
    }
}