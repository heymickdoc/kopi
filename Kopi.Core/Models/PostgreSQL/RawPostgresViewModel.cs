namespace Kopi.Core.Models.PostgreSQL;

public class RawPostgresViewModel
{
    public string SchemaName { get; set; }
    public string ViewName { get; set; }
    public string Definition { get; set; } // The SELECT statement
}