namespace Kopi.Core.Models.PostgreSQL;

public class RawPostgresRoutineModel
{
    public string SchemaName { get; set; }
    public string RoutineName { get; set; }
        
    // 'FUNCTION' or 'PROCEDURE'
    public string RoutineType { get; set; } 
        
    public string DataType { get; set; } // Return type (e.g. 'integer', 'void')
    public string Definition { get; set; } // The body code
}