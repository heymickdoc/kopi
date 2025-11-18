using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers.Special;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers.Special;

public class CommunitySpecialCountryNameTests
{
    [Theory]
    [InlineData("dbo", "Country", 50, true)]
    [InlineData("geo", "Nation", 100, true)]
    [InlineData("region", "CountryMaster", 255, true)]
    [InlineData("ref", "country_data", 60, true)]
    public void IsMatch_ValidCountryTable_ReturnsTrue(string schema, string table, int maxLength, bool expected)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        var result = CommunitySpecialCountryName.IsMatch(tableModel, maxLength);
        
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("dbo", "Country", 3, false)]
    [InlineData("geo", "Nation", 2, false)]
    [InlineData("ref", "country_code", 1, false)]
    public void IsMatch_MaxLengthTooSmall_ReturnsFalse(string schema, string table, int maxLength, bool expected)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        var result = CommunitySpecialCountryName.IsMatch(tableModel, maxLength);
        
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("production", "Country", 50, false)]
    [InlineData("inventory", "Nation", 100, false)]
    [InlineData("product", "CountryData", 255, false)]
    [InlineData("logging", "Country", 60, false)]
    [InlineData("audit", "Nation", 50, false)]
    [InlineData("finance", "Country", 100, false)]
    public void IsMatch_InvalidSchema_ReturnsFalse(string schema, string table, int maxLength, bool expected)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        var result = CommunitySpecialCountryName.IsMatch(tableModel, maxLength);
        
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("dbo", "Customer", 50, false)]
    [InlineData("geo", "City", 100, false)]
    [InlineData("ref", "Location", 255, false)]
    [InlineData("dbo", "Region", 60, false)]
    public void IsMatch_NonCountryTable_ReturnsFalse(string schema, string table, int maxLength, bool expected)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        var result = CommunitySpecialCountryName.IsMatch(tableModel, maxLength);
        
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("product_catalog", "Country", 50, false)]
    [InlineData("item_master", "Nation", 100, false)]
    public void IsMatch_InvalidSchemaRawCheck_ReturnsFalse(string schema, string table, int maxLength, bool expected)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        var result = CommunitySpecialCountryName.IsMatch(tableModel, maxLength);
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsMatch_NullSchemaName_ReturnsTrueForValidTable()
    {
        var tableModel = new TableModel { SchemaName = null, TableName = "Country" };
        
        var result = CommunitySpecialCountryName.IsMatch(tableModel, 50);
        
        Assert.True(result);
    }

    [Theory]
    [InlineData("geo", "Countries", 50, true)]
    [InlineData("ref", "Nations", 100, true)]
    public void IsMatch_PluralTableNames_ReturnsTrue(string schema, string table, int maxLength, bool expected)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        var result = CommunitySpecialCountryName.IsMatch(tableModel, maxLength);
        
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("dbo", "Country", 4, true)]
    [InlineData("geo", "Nation", 10, true)]
    public void IsMatch_MaxLengthGreaterThanThree_ReturnsTrue(string schema, string table, int maxLength, bool expected)
    {
        var tableModel = new TableModel { SchemaName = schema, TableName = table };
        
        var result = CommunitySpecialCountryName.IsMatch(tableModel, maxLength);
        
        Assert.Equal(expected, result);
    }
}