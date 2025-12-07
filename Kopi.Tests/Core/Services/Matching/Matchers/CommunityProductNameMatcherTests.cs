using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityProductNameMatcherTests
{
    private readonly CommunityProductNameMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldBe26()
    {
        Assert.Equal(26, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldBeProductName()
    {
        Assert.Equal("product_name", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("bigint")]
    [InlineData("decimal")]
    public void IsMatch_WithNonStringDataType_ShouldReturnFalse(string dataType)
    {
        var column = new ColumnModel { ColumnName = "ProductName", DataType = dataType };
        var table = new TableModel { SchemaName = "dbo", TableName = "Products" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("ProductName")]
    [InlineData("ItemName")]
    [InlineData("ModelName")]
    [InlineData("GoodsName")]
    [InlineData("MerchandiseName")]
    public void IsMatch_WithStrongColumnNames_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "nvarchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Products" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("product_name")]
    [InlineData("PRODUCTNAME")]
    [InlineData("Product-Name")]
    public void IsMatch_WithNormalizedStrongColumnNames_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Products" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("ProductID")]
    [InlineData("ProductCode")]
    [InlineData("ProductDescription")]
    [InlineData("ProductPrice")]
    [InlineData("ProductQuantity")]
    [InlineData("ProductType")]
    [InlineData("ProductCategory")]
    [InlineData("ProductImageURL")]
    public void IsMatch_WithExclusionWords_ShouldReturnFalse(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "nvarchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Products" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("Product_Title")]
    [InlineData("ProductName")]
    [InlineData("Item_Name")]
    [InlineData("ItemTitle")]
    public void IsMatch_WithProductOrItemPlusName_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "nvarchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "AnyTable" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Products")]
    [InlineData("Items")]
    [InlineData("Inventory")]
    [InlineData("Catalog")]
    [InlineData("Merchandise")]
    [InlineData("Stock")]
    public void IsMatch_WithGenericNameAndProductContext_ShouldReturnTrue(string tableName)
    {
        var column = new ColumnModel { ColumnName = "Name", DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = tableName };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithGenericNameWithoutProductContext_ShouldReturnFalse()
    {
        var column = new ColumnModel { ColumnName = "Name", DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Customers" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_WithSingleWordProduct_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "Product", DataType = "nvarchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Sales" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("HumanResources")]
    [InlineData("Person")]
    [InlineData("Employee")]
    [InlineData("User")]
    [InlineData("Log")]
    [InlineData("System")]
    public void IsMatch_WithInvalidSchemaNames_ShouldReturnFalse(string schemaName)
    {
        var column = new ColumnModel { ColumnName = "ProductName", DataType = "varchar" };
        var table = new TableModel { SchemaName = schemaName, TableName = "Products" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_WithTitleAndProductContext_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "Title", DataType = "nvarchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Products" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithProductContextInSchema_ShouldReturnTrue()
    {
        var column = new ColumnModel { ColumnName = "Name", DataType = "varchar" };
        var table = new TableModel { SchemaName = "Inventory", TableName = "Items" };

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Theory]
    [InlineData("SKU")]
    [InlineData("SerialNumber")]
    [InlineData("Key")]
    public void IsMatch_WithIdentifierWords_ShouldReturnFalse(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "nvarchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Products" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Theory]
    [InlineData("ProductNote")]
    [InlineData("ProductComment")]
    [InlineData("ProductDetails")]
    [InlineData("ProductInfo")]
    public void IsMatch_WithMetadataWords_ShouldReturnFalse(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Products" };

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }
}