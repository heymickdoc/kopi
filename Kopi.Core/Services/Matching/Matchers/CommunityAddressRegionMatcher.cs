using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;
using Kopi.Core.Models.Common;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for "Region" or "Territory" columns.
///  Generates broad geographic areas (e.g. "North", "EMEA", "Southwest") 
///  rather than political states/provinces.
/// </summary>
public class CommunityAddressRegionMatcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "address_region"; // Matches your Compass/EMEA generator

    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "log", "system", "error", "auth"
    };

    // --- 2. Strong Column Matches ---
    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "region",
        "regionname",
        "salesregion",
        "salesterritory",
        "territory",
        "territoryname",
        "georegion"
    };

    // --- 3. Exclusion Words ---
    // "Region" is often combined with other terms. We must yield to more specific matchers.
    private static readonly HashSet<string> ExclusionWords = new()
    {
        "country", // Yield to CountryMatcher (e.g. "CountryRegion")
        "state",   // Yield to StateMatcher
        "province", // Yield to StateMatcher
        "county",   // Yield to CountyMatcher
        "id", "code" // Avoid FKs or Codes (unless you want to generate codes)
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        // 1. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);
        
        // We don't strictly need table context for Region, as it's distinct.
        
        var colWords = StringUtils.SplitIntoWords(column.ColumnName)
            .Select(s => s.ToLower())
            .ToList();

        // 2. Immediate Disqualification
        if (InvalidSchemaNames.Overlaps(schemaWords)) return false;

        // 3. Negative Checks
        // CRITICAL: If it's "CountryRegion", we want the Country matcher to take it.
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 4. Strong Normalized Match
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Any(s => normalizedCol.Contains(s)))
        {
            return true;
        }

        // 5. Token Analysis
        
        // Case A: "Region"
        if (colWords.Contains("region"))
        {
            return true;
        }

        // Case B: "Territory"
        if (colWords.Contains("territory"))
        {
            return true;
        }

        return false;
    }
}