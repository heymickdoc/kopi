namespace Kopi.Core.Models.PostgreSQL;

public class RawPostgresRelationshipModel
{
    public string ConstraintName { get; set; }
        
    // Child (The table with the FK)
    public string SchemaName { get; set; }
    public string TableName { get; set; }
    public string ColumnName { get; set; }
        
    // Parent (The referenced table)
    public string ForeignSchemaName { get; set; }
    public string ForeignTableName { get; set; }
    public string ForeignColumnName { get; set; }
        
    // Useful for composite keys
    public int OrdinalPosition { get; set; }
}