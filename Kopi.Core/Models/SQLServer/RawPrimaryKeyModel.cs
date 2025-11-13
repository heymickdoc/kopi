namespace Kopi.Core.Models.SQLServer;

public class RawPrimaryKeyModel
{
    public string SchemaName { get; set; }
    public string TableName { get; set; }
    public string PrimaryKeyName { get; set; }
    public string ColumnName { get; set; }
    public int KeyOrder { get; set; }
}