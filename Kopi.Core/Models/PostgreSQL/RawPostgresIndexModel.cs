namespace Kopi.Core.Models.PostgreSQL;

public class RawPostgresIndexModel
{
    public string SchemaName { get; set; }
    public string TableName { get; set; }
    public string IndexName { get; set; }
        
    // These properties were missing:
    public string ColumnName { get; set; }
    public int? KeyOrdinal { get; set; } // Nullable because Dapper might map nulls if logic allows, though usually 0+
    public bool IsIncludedColumn { get; set; }

    public bool IsUnique { get; set; }
    public bool IsPrimaryKey { get; set; }
        
    // Optional: Keep definition if you need it for debugging, 
    // though the service doesn't strictly use it for the mapping logic below.
    public string IndexDefinition { get; set; } 
}