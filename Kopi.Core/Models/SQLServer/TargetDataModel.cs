using System.Data;

namespace Kopi.Core.Models.SQLServer;

/// <summary>
/// Holds the actual data for the target database
/// </summary>
public class TargetDataModel
{
    public string SchemaName { get; set; }
    public string TableName { get; set; }

    /// <summary>
    ///  
    /// </summary>
    public List<RowData> Rows { get; set; } = [];
}

public class RowData
{
    public List<ColumnData> Columns { get; set; } = [];
}

/// <summary>
///  Holds the data for a single column in a row
/// </summary>
public class ColumnData(string columnName, object? rawValue, string sqlDataType)
{
    public string ColumnName { get; set; } = columnName;
    public object? RawValue { get; set; } = rawValue;
    public string SqlDataType { get; set; } = sqlDataType;
}