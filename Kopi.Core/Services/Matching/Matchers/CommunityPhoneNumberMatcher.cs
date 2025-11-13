using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityPhoneNumberMatcher : IColumnMatcher
{
    public int Priority => 15;
    public string GeneratorTypeKey => "phone_number";
    
    private static readonly HashSet<string> ColumnNames = new()
    {
        "phonenumber",
        "phone",
        "tel",
        "telephone",
        "mobile",
        "cellphone",
        "cell"
    };
   
    private static readonly HashSet<string> TableNames = new()
    {
        "contact",
        "customer",
        "employee",
        "person",
        "user"
    };

    private static readonly HashSet<string> SchemaNames = new()
    {
        "person",
        "customer",
        "contact",
        "employee",
        "user"
    };
    
    
    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        var score = 0;
        
        var colName = new string(column.ColumnName.ToLower().Where(char.IsLetterOrDigit).ToArray());
        var tableName = tableContext.TableName.ToLower();
        var schemaName = tableContext.SchemaName.ToLower();
        
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;
        
        //Exact match
        if (ColumnNames.Contains(colName))
        {
            score += 32;
        }
        
        //Partial match
        if (ColumnNames.Any(k => colName.Contains(k)))
        {
            score += 16;
        }
        
        if (TableNames.Any(k => tableName.Contains(k)))
        {
            score += 8;
        }

        if (SchemaNames.Any(k => schemaName.Contains(k)))
        {
            score += 4;
        }
        
        return score >= 20;
    }
}