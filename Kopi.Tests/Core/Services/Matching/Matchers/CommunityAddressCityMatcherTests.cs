using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityAddressCityMatcherTests
{
    private readonly CommunityAddressCityMatcher _matcher;

    public CommunityAddressCityMatcherTests()
    {
        _matcher = new CommunityAddressCityMatcher();
    }

    [Fact]
    public void Priority_ShouldReturn10()
    {
        Assert.Equal(10, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnAddressCity()
    {
        Assert.Equal("address_city", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("City", "varchar", "dbo", "Customers")]
    [InlineData("CityName", "nvarchar", "dbo", "Address")]
    [InlineData("Town", "varchar", "dbo", "Location")]
    [InlineData("TownName", "nvarchar", "dbo", "CustomerAddress")]
    [InlineData("Municipality", "varchar", "dbo", "Locations")]
    [InlineData("Suburb", "nvarchar", "dbo", "ShippingAddress")]
    [InlineData("Village", "varchar", "dbo", "Store")]
    [InlineData("Metro", "nvarchar", "dbo", "Site")]
    public void IsMatch_StrongColumnNames_ShouldReturnTrue(string columnName, string dataType, string schema, string table)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType };
        var tableContext = new TableModel { SchemaName = schema, TableName = table };

        Assert.True(_matcher.IsMatch(column, tableContext));
    }

    [Theory]
    [InlineData("AddressCity", "varchar", "dbo", "Customers")]
    [InlineData("BillingCity", "nvarchar", "dbo", "Orders")]
    [InlineData("ShippingCity", "varchar", "dbo", "Shipments")]
    [InlineData("MailingCity", "nvarchar", "dbo", "Contacts")]
    public void IsMatch_PrefixedCityNames_ShouldReturnTrue(string columnName, string dataType, string schema, string table)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType };
        var tableContext = new TableModel { SchemaName = schema, TableName = table };

        Assert.True(_matcher.IsMatch(column, tableContext));
    }

    [Theory]
    [InlineData("Capacity", "varchar", "dbo", "Warehouse")]
    [InlineData("Velocity", "nvarchar", "dbo", "Products")]
    [InlineData("Electricity", "varchar", "dbo", "Utilities")]
    [InlineData("Scarcity", "nvarchar", "dbo", "Inventory")]
    [InlineData("Publicity", "varchar", "dbo", "Marketing")]
    [InlineData("Simplicity", "nvarchar", "dbo", "Config")]
    [InlineData("Elasticity", "varchar", "dbo", "Products")]
    [InlineData("Multiplicity", "nvarchar", "dbo", "Data")]
    [InlineData("Authenticity", "varchar", "dbo", "Security")]
    public void IsMatch_ExclusionWords_ShouldReturnFalse(string columnName, string dataType, string schema, string table)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType };
        var tableContext = new TableModel { SchemaName = schema, TableName = table };

        Assert.False(_matcher.IsMatch(column, tableContext));
    }

    [Theory]
    [InlineData("CityID", "int", "dbo", "Customers")]
    [InlineData("CityCode", "varchar", "dbo", "Locations")]
    [InlineData("CityKey", "int", "dbo", "Address")]
    public void IsMatch_CityWithIdCodeKey_ShouldReturnFalse(string columnName, string dataType, string schema, string table)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType };
        var tableContext = new TableModel { SchemaName = schema, TableName = table };

        Assert.False(_matcher.IsMatch(column, tableContext));
    }

    [Theory]
    [InlineData("City", "varchar", "production", "Items")]
    [InlineData("City", "varchar", "inventory", "Stock")]
    [InlineData("Town", "nvarchar", "product", "Catalog")]
    [InlineData("Municipality", "varchar", "log", "Events")]
    [InlineData("City", "nvarchar", "capacity", "Planning")]
    public void IsMatch_InvalidSchemaNames_ShouldReturnFalse(string columnName, string dataType, string schema, string table)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType };
        var tableContext = new TableModel { SchemaName = schema, TableName = table };

        Assert.False(_matcher.IsMatch(column, tableContext));
    }

    [Theory]
    [InlineData("City", "int", "dbo", "Customers")]
    [InlineData("Town", "bigint", "dbo", "Address")]
    [InlineData("Municipality", "decimal", "dbo", "Locations")]
    public void IsMatch_NonStringDataType_ShouldReturnFalse(string columnName, string dataType, string schema, string table)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType };
        var tableContext = new TableModel { SchemaName = schema, TableName = table };

        Assert.False(_matcher.IsMatch(column, tableContext));
    }

    [Theory]
    [InlineData("customer_city", "varchar", "dbo", "Customers")]
    [InlineData("billing-city", "nvarchar", "dbo", "Orders")]
    [InlineData("shipping_town", "varchar", "dbo", "Shipments")]
    public void IsMatch_UnderscoreAndHyphenSeparators_ShouldReturnTrue(string columnName, string dataType, string schema, string table)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType };
        var tableContext = new TableModel { SchemaName = schema, TableName = table };

        Assert.True(_matcher.IsMatch(column, tableContext));
    }

    [Theory]
    [InlineData("Name", "varchar", "dbo", "Products")]
    [InlineData("Description", "nvarchar", "dbo", "Items")]
    [InlineData("Status", "varchar", "dbo", "Orders")]
    public void IsMatch_UnrelatedColumns_ShouldReturnFalse(string columnName, string dataType, string schema, string table)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = dataType };
        var tableContext = new TableModel { SchemaName = schema, TableName = table };

        Assert.False(_matcher.IsMatch(column, tableContext));
    }
}