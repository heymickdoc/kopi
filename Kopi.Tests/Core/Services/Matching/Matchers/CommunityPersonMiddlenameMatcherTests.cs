using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers;
using Xunit;

namespace Kopi.Tests.Core.Services.Matching.Matchers;

public class CommunityPersonMiddlenameMatcherTests
{
    private readonly CommunityPersonMiddlenameMatcher _matcher = new();

    [Fact]
    public void Priority_ShouldBe23()
    {
        Assert.Equal(23, _matcher.Priority);
    }

    [Fact]
    public void GeneratorTypeKey_ShouldBeMiddleName()
    {
        Assert.Equal("person_middlename", _matcher.GeneratorTypeKey);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("bigint")]
    [InlineData("decimal")]
    [InlineData("datetime")]
    public void IsMatch_NonStringDataType_ShouldReturnFalse(string dataType)
    {
        var column = new ColumnModel { ColumnName = "MiddleName", DataType = dataType };
        var table = new TableModel { SchemaName = "dbo", TableName = "Users" };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("MiddleName")]
    [InlineData("middlename")]
    [InlineData("MIDDLENAME")]
    [InlineData("middle_name")]
    [InlineData("middle-name")]
    public void IsMatch_StrongColumnNames_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Users" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("MName")]
    [InlineData("mname")]
    [InlineData("MiddleInitial")]
    [InlineData("MInitial")]
    [InlineData("MidName")]
    public void IsMatch_AdditionalStrongMatches_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "nvarchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Customers" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("production")]
    [InlineData("inventory")]
    [InlineData("product")]
    [InlineData("log")]
    [InlineData("system")]
    [InlineData("error")]
    [InlineData("auth")]
    [InlineData("config")]
    [InlineData("setting")]
    public void IsMatch_InvalidSchemaNames_ShouldReturnFalse(string schemaName)
    {
        var column = new ColumnModel { ColumnName = "MiddleName", DataType = "varchar" };
        var table = new TableModel { SchemaName = schemaName, TableName = "Data" };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("FirstMiddleName")]
    [InlineData("LastMiddleName")]
    [InlineData("MiddleClass")]
    [InlineData("MiddleTier")]
    [InlineData("SurMiddleName")]
    [InlineData("FamilyMiddleName")]
    [InlineData("GivenMiddleName")]
    public void IsMatch_ExclusionWords_ShouldReturnFalse(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Users" };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("MiddleNameValue")]
    [InlineData("Middle_Name")]
    [InlineData("MiddleNm")]
    [InlineData("Middle_Nm")]
    public void IsMatch_MiddlePlusName_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Contacts" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("MiddleInitialValue")]
    [InlineData("Middle_Initial")]
    [InlineData("MiddleInit")]
    [InlineData("Middle_Init")]
    public void IsMatch_MiddlePlusInitial_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "nvarchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "People" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("MInitialValue")]
    [InlineData("M_Initial")]
    [InlineData("MInit")]
    [InlineData("M_Init")]
    public void IsMatch_MPlusInitial_ShouldReturnTrue(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Employees" };

        Assert.True(_matcher.IsMatch(column, table));
    }

    [Theory]
    [InlineData("FirstName")]
    [InlineData("LastName")]
    [InlineData("FullName")]
    [InlineData("Email")]
    [InlineData("Phone")]
    [InlineData("Address")]
    public void IsMatch_UnrelatedColumns_ShouldReturnFalse(string columnName)
    {
        var column = new ColumnModel { ColumnName = columnName, DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Users" };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_MiddleAlone_ShouldReturnFalse()
    {
        var column = new ColumnModel { ColumnName = "Middle", DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "Users" };

        Assert.False(_matcher.IsMatch(column, table));
    }

    [Fact]
    public void IsMatch_ValidPersonTableContext_ShouldMatch()
    {
        var column = new ColumnModel { ColumnName = "MiddleName", DataType = "varchar" };
        var table = new TableModel { SchemaName = "dbo", TableName = "UserProfiles" };

        Assert.True(_matcher.IsMatch(column, table));
    }
}