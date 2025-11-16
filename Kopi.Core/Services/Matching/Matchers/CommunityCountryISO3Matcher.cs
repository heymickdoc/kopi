using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityCountryISO3Matcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "country_iso3";

    private static readonly HashSet<string> ColumnNames = new()
    {
        "iso3country",
        "countryiso",
        "isocountry",
        "countrycodeiso",
        "countrycode",
        "iso3",
        "countryiso3",
        "countrycode3",
        "country3"
    };
    
    private static readonly HashSet<string> PartialColumnNames = new()
    {
        "iso",
        "country",
        "code",
        "ctry",
        "nation",
        "state",
        "region"
    };

    private static readonly HashSet<string> TableNames = new()
    {
        "address",
        "location",
        "customer",
        "user",
        "country",
        "region",
        "geo",
        "loc"
    };

    private static readonly HashSet<string> SchemaNames = new()
    {
        "person",
        "customer",
        "location",
        "address",
        "country",
        "region",
        "geo"
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        var score = 0;

        if (tableContext.SchemaName.Equals("person", StringComparison.OrdinalIgnoreCase) &&
            tableContext.TableName.Equals("countryregion", StringComparison.OrdinalIgnoreCase))
        {
            var debug = 1;
        }

        var colName = new string(column.ColumnName.ToLower().Where(char.IsLetterOrDigit).ToArray());
        var tableName = tableContext.TableName.ToLower();
        var schemaName = tableContext.SchemaName.ToLower();

        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        //Look for a column length of 3
        var maxLength = DataTypeHelper.GetMaxLength(column);
        if (maxLength == 3)
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
        
        //Check if the partial column names exist within the column name
        if (PartialColumnNames.Any(k => colName.Contains(k)))
        {
            score += 8;
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