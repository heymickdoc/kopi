using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers.Special;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers.Special;

public class CommunitySpecialCityNameTests
{
    [Theory]
    [InlineData("dbo", "City", 50, true)]
    [InlineData("geo", "Towns", 100, true)]
    [InlineData("location", "Municipality", 75, true)]
    [InlineData("address", "Suburb", 30, true)]
    [InlineData("geo", "Villages", 40, true)]
    [InlineData("location", "Metro", 25, true)]
    public void IsMatch_ValidCityTables_ReturnsTrue(string schema, string table, int maxLength, bool expected)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        var result = CommunitySpecialCityName.IsMatch(tableModel, maxLength);
        
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("dbo", "City", 2, false)]
    [InlineData("geo", "Town", 1, false)]
    public void IsMatch_MaxLengthTooSmall_ReturnsFalse(string schema, string table, int maxLength, bool expected)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        var result = CommunitySpecialCityName.IsMatch(tableModel, maxLength);
        
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("production", "City", 50, false)]
    [InlineData("inventory", "Town", 100, false)]
    [InlineData("product", "Municipality", 75, false)]
    [InlineData("catalog", "Suburb", 30, false)]
    [InlineData("logging", "Village", 40, false)]
    [InlineData("audit", "Metro", 25, false)]
    [InlineData("security", "City", 50, false)]
    [InlineData("finance", "Town", 100, false)]
    public void IsMatch_InvalidSchemaNames_ReturnsFalse(string schema, string table, int maxLength, bool expected)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        var result = CommunitySpecialCityName.IsMatch(tableModel, maxLength);
        
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("dbo", "Customer", 50, false)]
    [InlineData("geo", "Country", 100, false)]
    [InlineData("location", "Address", 75, false)]
    public void IsMatch_InvalidTableNames_ReturnsFalse(string schema, string table, int maxLength, bool expected)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        var result = CommunitySpecialCityName.IsMatch(tableModel, maxLength);
        
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("geo_location", "city_master", 50, true)]
    [InlineData("GeographicData", "CityList", 100, true)]
    public void IsMatch_CompoundNames_HandlesCorrectly(string schema, string table, int maxLength, bool expected)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        var result = CommunitySpecialCityName.IsMatch(tableModel, maxLength);
        
        Assert.Equal(expected, result);
    }
}