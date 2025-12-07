namespace Kopi.Core.Models.Common;

public class ColumnModel
{
    // ----- Core Identity -----
    public string SchemaName { get; set; }
    public string TableName { get; set; }
    public string ColumnName { get; set; }

    // ----- Data Type Info -----
    public string DataType { get; set; } // e.g., "nvarchar", "int"
    public string MaxLength { get; set; }
    public int NumericPrecision { get; set; }
    public int NumericScale { get; set; }
    public bool IsUserDefinedType { get; set; }
    public bool IsNullable { get; set; } = false;

    // ----- Key Info (Essential for relational generation) -----
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }

    // ----- Identity Info -----
    public bool IsIdentity { get; set; }
    public int? IdentitySeed { get; set; }
    public int? IdentityIncrement { get; set; }

    // ----- Computed Column Info -----
    public bool IsComputed { get; set; }
    public bool IsPersisted { get; set; }
    public string? ComputedDefinition { get; set; }

    // ----- Default Value Info -----
    public string? DefaultDefinition { get; set; }
    public string? DefaultConstraintName { get; set; }
}