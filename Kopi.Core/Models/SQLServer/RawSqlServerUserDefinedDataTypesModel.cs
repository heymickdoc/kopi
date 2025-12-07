namespace Kopi.Core.Models.SQLServer;

public class RawSqlServerUserDefinedDataTypesModel
{
    public string SchemaName { get; set; }
    public string TypeName { get; set; }
    public string BaseTypeName { get; set; }
    public int MaxLength { get; set; }
    public int Precision { get; set; }
    public int Scale { get; set; }
    public bool IsNullable { get; set; }
    public string CreateScript { get; set; }
}