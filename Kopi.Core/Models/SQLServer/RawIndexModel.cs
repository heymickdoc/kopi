namespace Kopi.Core.Models.SQLServer;

public class RawIndexModel
{
	public string SchemaName { get; set; }
	public string TableName { get; set; }
	public string IndexName { get; set; }
	public string ColumnName { get; set; }
	public bool IsUnique { get; set; } = false;
	public bool IsPrimaryKey { get; set; }
	public int KeyOrdinal { get; set; }
	public bool IsIncludedColumn { get; set; }
}
