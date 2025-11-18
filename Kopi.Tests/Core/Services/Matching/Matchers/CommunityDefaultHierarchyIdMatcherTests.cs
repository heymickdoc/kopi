using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityDefaultHierarchyIdMatcherTests
{
    private readonly CommunityDefaultHierarchyIdMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldReturn20()
    {
        Assert.Equal(20, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldReturnDefaultHierarchyId()
    {
        Assert.Equal("default_hierarchyid", _matcher.GeneratorTypeKey);
    }

    [Fact]
    public void IsMatch_WhenDataTypeIsHierarchyId_ShouldReturnTrue()
    {
        var column = new ColumnModel { DataType = "hierarchyid" };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WhenDataTypeIsHierarchyIdUpperCase_ShouldReturnTrue()
    {
        var column = new ColumnModel { DataType = "HIERARCHYID" };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WhenDataTypeIsHierarchyIdMixedCase_ShouldReturnTrue()
    {
        var column = new ColumnModel { DataType = "HierarchyId" };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_WhenDataTypeIsNotHierarchyId_ShouldReturnFalse()
    {
        var column = new ColumnModel { DataType = "varchar" };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_WhenDataTypeIsNull_ShouldReturnFalse()
    {
        var column = new ColumnModel { DataType = null };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_WhenDataTypeIsEmpty_ShouldReturnFalse()
    {
        var column = new ColumnModel { DataType = string.Empty };
        var table = new TableModel();

        var result = _matcher.IsMatch(column, table);

        Assert.False(result);
    }
}