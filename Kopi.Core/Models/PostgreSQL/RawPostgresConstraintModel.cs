namespace Kopi.Core.Models.PostgreSQL;

public class RawPostgresConstraintModel
{
    public string SchemaName { get; set; }
    public string TableName { get; set; }
    public string ConstraintName { get; set; }
        
    // 'p' = Primary Key, 'f' = Foreign Key, 'u' = Unique, 'c' = Check, 't' = Trigger
    public char ConstraintType { get; set; } 
        
    // The reconstructed SQL (e.g., "CHECK (price > 0)")
    public string Definition { get; set; }
}