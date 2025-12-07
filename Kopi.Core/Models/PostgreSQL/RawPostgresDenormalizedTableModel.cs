namespace Kopi.Core.Models.PostgreSQL;

/// <summary>
/// Represents a single row from the Postgres information_schema.columns view, 
/// flattened with Key and Identity information.
/// </summary>
public class RawPostgresDenormalizedTableModel
{
    public string SchemaName { get; set; }
    public string TableName { get; set; }
    public string ColumnName { get; set; }
    public int OrdinalPosition { get; set; }
    public string DataType { get; set; }
    public string UdtName { get; set; }
    public string IsNullable { get; set; } // Postgres returns "YES"/"NO"
    public int? CharacterMaxLength { get; set; }
    public int? NumericPrecision { get; set; }
    public int? NumericScale { get; set; }
    
    // Identity (Postgres 10+)
    public string IsIdentity { get; set; } // "YES"/"NO"
    public string IdentityGeneration { get; set; } // "ALWAYS"/"BY DEFAULT"
    public string IdentityStart { get; set; } // Postgres returns these as strings in some versions
    public string IdentityIncrement { get; set; }

    // Defaults & Computed
    public string DefaultDefinition { get; set; }
    public string GenerationExpression { get; set; } // For generated columns (PG 12+)

    // Calculated in SQL
    public bool IsPrimaryKey { get; set; }
}