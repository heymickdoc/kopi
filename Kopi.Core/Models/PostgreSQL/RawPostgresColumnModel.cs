namespace Kopi.Core.Models.PostgreSQL;

public class RawPostgresColumnModel
{
    public string SchemaName { get; set; }
    public string TableName { get; set; }
    public string ColumnName { get; set; }
    public int OrdinalPosition { get; set; }
    public string DataType { get; set; } // e.g., 'integer', 'character varying'
    public string UdtName { get; set; } // The internal postgres type name
    public bool IsNullable { get; set; }
    public int? CharacterMaxLength { get; set; }
    public int? NumericPrecision { get; set; }
    public int? NumericScale { get; set; }
        
    // Identity / Sequence Info (Postgres 10+)
    public bool IsIdentity { get; set; } 
    public string IdentityGeneration { get; set; } // 'ALWAYS' or 'BY DEFAULT'
        
    // Default Values
    public string DefaultValue { get; set; }
}