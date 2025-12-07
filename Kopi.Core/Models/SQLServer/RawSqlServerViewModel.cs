namespace Kopi.Core.Models.SQLServer;

public class RawSqlServerViewModel
{
    public string SchemaName { get; set; }
    public string ViewName { get; set; }
    public string Definition { get; set; }
    public string CreateScript { get; set; }
}