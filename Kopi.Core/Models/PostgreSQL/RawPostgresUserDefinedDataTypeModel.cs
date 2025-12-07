namespace Kopi.Core.Models.PostgreSQL;

public class RawPostgresUserDefinedDataTypeModel
{
    public string SchemaName { get; set; }
    public string TypeName { get; set; }
    public string BaseTypeName { get; set; } // For Domains
    public char TypeType { get; set; } // 'd' = domain, 'e' = enum
    public string CreateScript { get; set; } // We can reconstruct this or just store raw info
}