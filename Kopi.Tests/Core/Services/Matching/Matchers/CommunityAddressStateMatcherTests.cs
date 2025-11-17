using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityAddressStateMatcherTests
{
    private readonly CommunityAddressStateMatcher _matcher = new();

    [Theory]
    [InlineData("varchar", true)]
    [InlineData("nvarchar", true)]
    [InlineData("char", true)]
    [InlineData("int", false)]
    [InlineData("bigint", false)]
    public void IsMatch_NonStringDataType_ReturnsFalse(string dataType, bool shouldMatch)
    {
        var column = new ColumnModel { ColumnName = "State", DataType = dataType };
        var table = new TableModel { SchemaName = "dbo", TableName = "Address" };

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(shouldMatch, result);
    }

    [Theory]
    [InlineData("StateProvince")]
    [InlineData("AddressState")]
    [InlineData("BillingState")]
    [InlineData("ShippingState")]
    [InlineData("MailingState")]
    [InlineData("AddrState")]
    [InlineData("StateCode")]
    [InlineData("ProvinceCode")]
    public void IsMatch_StrongColumnNames_ReturnsTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Customer" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Province")]
    [InlineData("CustomerProvince")]
    [InlineData("Billing_Province")]
    public void IsMatch_ProvinceColumn_ReturnsTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Orders" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("production")]
    [InlineData("inventory")]
    [InlineData("product")]
    [InlineData("workflow")]
    [InlineData("process")]
    public void IsMatch_InvalidSchemaNames_ReturnsFalse(string schemaName)
    {
        var column = new ColumnModel { ColumnName = "State", DataType = "varchar" };
        var table = new TableModel { SchemaName = schemaName, TableName = "Data" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("OrderState")]
    [InlineData("WorkflowState")]
    [InlineData("TaskState")]
    [InlineData("JobState")]
    [InlineData("MachineState")]
    [InlineData("SystemState")]
    public void IsMatch_StatusIndicators_ReturnsFalse(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Orders" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("Address_State")]
    [InlineData("BillingState")]
    [InlineData("ShippingState")]
    [InlineData("AddrState")]
    public void IsMatch_StateWithAddressIndicator_ReturnsTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Orders" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Address", "State", true)]
    [InlineData("Customer", "State", true)]
    [InlineData("Location", "State", true)]
    [InlineData("Vendor", "State", true)]
    [InlineData("Employee", "State", true)]
    [InlineData("Orders", "State", false)]
    [InlineData("Products", "State", false)]
    public void IsMatch_GenericStateColumn_RequiresTableContext(string tableName, string columnName, bool expected)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = tableName };

        var result = _matcher.IsMatch(column, table);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Priority_Returns10()
    {
        Assert.Equal(10, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ReturnsAddressState()
    {
        Assert.Equal("address_state", _matcher.GeneratorTypeKey);
    }
}