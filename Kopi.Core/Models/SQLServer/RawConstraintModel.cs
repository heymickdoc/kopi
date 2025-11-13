namespace Kopi.Core.Models.SQLServer;

/// <summary>
/// Holds information about a database constraint, including its schema, table, name, type, and the SQL script used to create it.
/// This is identical to ConstraintModel for now, but serves as a separate model for potential future transformations or additional properties.
/// </summary>
public class RawConstraintModel
{
    public string SchemaName { get; set; }
    public string TableName { get; set; }
    public string ConstraintName { get; set; }
    public string ConstraintType { get; set; } // CHECK, DEFAULT, UNIQUE, PRIMARY KEY, FOREIGN KEY
    public string CreateScript { get; set; }
}