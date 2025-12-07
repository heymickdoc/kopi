namespace Kopi.Core.Models.SQLServer;

public class RawSqlServerStoredProceduresAndFunctionsModel
{
    public string SchemaName { get; set; }
    public string ObjectName { get; set; }
    public string ObjectType { get; set; } // PROCEDURE, SCALAR_FUNCTION, TABLE_VALUED_FUNCTION
    public string Definition { get; set; }
}