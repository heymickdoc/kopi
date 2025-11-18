using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers.Special;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers.Special;

public class CommunitySpecialPersonNameTests
{
    [Theory]
    [InlineData("dbo", "Customer")]
    [InlineData("dbo", "Employee")]
    [InlineData("dbo", "User")]
    [InlineData("dbo", "Contact")]
    [InlineData("dbo", "Person")]
    [InlineData("dbo", "Staff")]
    [InlineData("dbo", "Personnel")]
    [InlineData("dbo", "Client")]
    [InlineData("dbo", "Subscriber")]
    [InlineData("dbo", "Member")]
    [InlineData("dbo", "Partner")]
    [InlineData("dbo", "Candidate")]
    [InlineData("dbo", "Lead")]
    public void IsMatch_WithValidPersonTables_ReturnsTrue(string schema, string table)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        Assert.True(CommunitySpecialPersonName.IsMatch(tableModel));
    }

    [Theory]
    [InlineData("production", "Customer")]
    [InlineData("inventory", "Employee")]
    [InlineData("product", "User")]
    [InlineData("item", "Contact")]
    [InlineData("catalog", "Person")]
    [InlineData("logistics", "Staff")]
    [InlineData("warehouse", "Client")]
    [InlineData("asset", "Member")]
    [InlineData("system", "Partner")]
    [InlineData("log", "Candidate")]
    public void IsMatch_WithInvalidSchemas_ReturnsFalse(string schema, string table)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        Assert.False(CommunitySpecialPersonName.IsMatch(tableModel));
    }

    [Theory]
    [InlineData("dbo", "Product")]
    [InlineData("dbo", "Inventory")]
    [InlineData("dbo", "Order")]
    [InlineData("dbo", "Invoice")]
    public void IsMatch_WithNonPersonTables_ReturnsFalse(string schema, string table)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        Assert.False(CommunitySpecialPersonName.IsMatch(tableModel));
    }

    [Theory]
    [InlineData("dbo", "Customers")]
    [InlineData("dbo", "Employees")]
    [InlineData("dbo", "Users")]
    public void IsMatch_WithPluralTableNames_ReturnsTrue(string schema, string table)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        Assert.True(CommunitySpecialPersonName.IsMatch(tableModel));
    }

    [Theory]
    [InlineData("dbo", "CustomerInfo")]
    [InlineData("dbo", "EmployeeDetails")]
    [InlineData("dbo", "UserProfile")]
    public void IsMatch_WithCompoundTableNames_ReturnsTrue(string schema, string table)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        Assert.True(CommunitySpecialPersonName.IsMatch(tableModel));
    }

    [Theory]
    [InlineData("SystemLogs", "Customer")]
    [InlineData("ProductionData", "Employee")]
    public void IsMatch_WithCompoundInvalidSchemas_ReturnsFalse(string schema, string table)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        Assert.False(CommunitySpecialPersonName.IsMatch(tableModel));
    }

    [Fact]
    public void IsMatch_WithNullSchemaName_DoesNotThrow()
    {
        var tableModel = new TableModel { SchemaName = null, TableName = "Customer" };
        
        var result = CommunitySpecialPersonName.IsMatch(tableModel);
        
        Assert.True(result);
    }

    [Theory]
    [InlineData("dbo", "customer_data")]
    [InlineData("dbo", "employee_records")]
    public void IsMatch_WithUnderscoreTableNames_ReturnsTrue(string schema, string table)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        Assert.True(CommunitySpecialPersonName.IsMatch(tableModel));
    }
}