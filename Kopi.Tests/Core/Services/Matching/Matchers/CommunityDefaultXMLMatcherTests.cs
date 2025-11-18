using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityDefaultXMLMatcherTests
{
    private readonly CommunityDefaultXMLMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldReturn10()
    {
        Assert.Equal(10, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnDefaultXml()
    {
        Assert.Equal("default_xml", _matcher.GeneratorTypeKey);
    }

    [Fact]
    public void IsMatch_WithXmlDataType_ShouldReturnTrue()
    {
        var column = new ColumnModel { DataType = "xml" };
        var tableContext = new TableModel();

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithUpperCaseXmlDataType_ShouldReturnTrue()
    {
        var column = new ColumnModel { DataType = "XML" };
        var tableContext = new TableModel();

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithMixedCaseXmlDataType_ShouldReturnTrue()
    {
        var column = new ColumnModel { DataType = "Xml" };
        var tableContext = new TableModel();

        var result = _matcher.IsMatch(column, tableContext);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WithNonXmlDataType_ShouldReturnFalse()
    {
        var column = new ColumnModel { DataType = "varchar" };
        var tableContext = new TableModel();

        var result = _matcher.IsMatch(column, tableContext);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_WithNullDataType_ShouldReturnFalse()
    {
        var column = new ColumnModel { DataType = null };
        var tableContext = new TableModel();

        var result = _matcher.IsMatch(column, tableContext);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_WithEmptyDataType_ShouldReturnFalse()
    {
        var column = new ColumnModel { DataType = string.Empty };
        var tableContext = new TableModel();

        var result = _matcher.IsMatch(column, tableContext);

        Assert.False(result);
    }
}