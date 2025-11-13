using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityCountryISO2Matcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "country_iso2";

    private static readonly HashSet<string> ColumnNames = new()
    {
        "iso2country",
        "countryiso",
        "isocountry",
        "countrycodeiso",
        "countrycode",
        "iso2",
        "countryiso2",
        "countrycode2",
        "country2"
    };

    private static readonly HashSet<string> TableNames = new()
    {
        "address",
        "location",
        "customer",
        "user"
    };

    private static readonly HashSet<string> SchemaNames = new()
    {
        "person",
        "customer",
        "location",
        "address"
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        var score = 0;

        var colName = new string(column.ColumnName.ToLower().Where(char.IsLetterOrDigit).ToArray());
        var tableName = tableContext.TableName.ToLower();
        var schemaName = tableContext.SchemaName.ToLower();

        if (!DataTypeHelper.IsStringType(column.DataType)) return false;
        
        //Look for a column length of 2
        var maxLength = DataTypeHelper.GetMaxLength(column);
        if (maxLength == 2)
        {
            score += 16;
        }

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