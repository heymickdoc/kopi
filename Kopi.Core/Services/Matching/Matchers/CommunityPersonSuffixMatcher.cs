using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityPersonSuffixMatcher : IColumnMatcher
{
    public int Priority => 23;
    public string GeneratorTypeKey => "person_suffix";
    
    private static readonly HashSet<string> ColumnNames = new()
    {
        "suffix",
        "personsuffix",
        "courtesysuffix",
        "namesuffix"
    };

    private static readonly HashSet<string> TableNames = new()
    {
        "user",
        "employee",
        "person",
        "contact",
        "customer"
    };

    private static readonly HashSet<string> SchemaNames = new()
    {
        "person",
        "employee",
        "contact",
        "customer"
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

        return score >= 32;
    }
}