using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers.Special;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers.Special;

public class CommunitySpecialStatusNameTests
{
    [Theory]
    [InlineData("dbo", "OrderStatus")]
    [InlineData("dbo", "WorkflowStage")]
    [InlineData("dbo", "ProjectPhase")]
    [InlineData("dbo", "WorkflowStep")]
    [InlineData("dbo", "ItemCondition")]
    [InlineData("dbo", "EmployeeRank")]
    [InlineData("dbo", "AccessLevel")]
    [InlineData("dbo", "PricingTier")]
    public void IsMatch_WithValidStatusTables_ReturnsTrue(string schema, string table)
    {
        // Arrange
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        // Act
        var result = CommunitySpecialStatusName.IsMatch(tableModel);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("log", "OrderStatus")]
    [InlineData("logging", "UserStatus")]
    [InlineData("audit", "StatusTable")]
    [InlineData("history", "StatusLog")]
    [InlineData("backup", "Status")]
    [InlineData("sys", "StatusRecord")]
    [InlineData("system", "SystemStatus")]
    [InlineData("config", "ConfigStatus")]
    [InlineData("setting", "StatusSetting")]
    [InlineData("error", "ErrorStatus")]
    public void IsMatch_WithInvalidSchemas_ReturnsFalse(string schema, string table)
    {
        // Arrange
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        // Act
        var result = CommunitySpecialStatusName.IsMatch(tableModel);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("dbo", "Order")]
    [InlineData("dbo", "Customer")]
    [InlineData("dbo", "Product")]
    [InlineData("dbo", "Invoice")]
    public void IsMatch_WithNonStatusTables_ReturnsFalse(string schema, string table)
    {
        // Arrange
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        // Act
        var result = CommunitySpecialStatusName.IsMatch(tableModel);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("dbo", "order_status")]
    [InlineData("dbo", "workflow_stage")]
    [InlineData("dbo", "user_rank")]
    public void IsMatch_WithUnderscoreSeparatedNames_ReturnsTrue(string schema, string table)
    {
        // Arrange
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        // Act
        var result = CommunitySpecialStatusName.IsMatch(tableModel);

        // Assert
        Assert.True(result);
    }


    [Theory]
    [InlineData("dbo", "Statuses")]
    [InlineData("dbo", "Stages")]
    [InlineData("dbo", "Levels")]
    public void IsMatch_WithPluralForms_ReturnsTrue(string schema, string table)
    {
        // Arrange
        var tableModel = new TableModel { SchemaName = schema, TableName = table };

        // Act
        var result = CommunitySpecialStatusName.IsMatch(tableModel);

        // Assert
        Assert.True(result);
    }
}