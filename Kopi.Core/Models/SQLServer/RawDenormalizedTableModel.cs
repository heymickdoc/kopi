namespace Kopi.Core.Models.SQLServer;

/// <summary>
/// Initial data we grab from the database. We need to tidy this as it's denormalized
/// </summary>
public class RawDenormalizedTableModel
{
	public string TableName { get; set; }
	public string SchemaName { get; set; }
	public string ColumnName { get; set; }
	public string DataType { get; set; }
	public string MaxLength { get; set; }
	public bool IsNullable { get; set; } = false;
	public int NumericPrecision { get; set; }
	public int NumericScale { get; set; }
	public bool IsIdentity { get; set; }
	public int IdentitySeed { get; set; }
	public int IdentityIncrement { get; set; }
	public bool IsPrimaryKey { get; set; } = false;
	public bool IsComputed { get; set; }
	public bool IsPersisted { get; set; }
	public string? ComputedDefinition { get; set; }
	public string? DefaultDefinition { get; set; }
	public string? DefaultConstraintName { get; set; }
	public bool IsUserDefinedType { get; set; }
}