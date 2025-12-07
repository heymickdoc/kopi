using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;
using Kopi.Core.Models.Common;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for full "Country Name" columns (e.g. "United States", "Germany").
///  Strictly ignores short columns (ISO codes) and "County" columns.
/// </summary>
public class CommunityCountryMatcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "country_name";

    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "log", "system", "error", "auth"
    };

    // --- 2. Strong Table Context ---
    private static readonly HashSet<string> AddressTableContexts = new()
    {
        "address", "location", "customer", "client", "vendor",
        "supplier", "employee", "store", "site", "shipping", "billing",
        "person", "country", "geo", "region", "sales"
    };

    // --- 3. Strong Column Matches ---
    // Normalized (lowercase, no separators)
    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "country",
        "countryname",
        "nation",
        "nationality",
        "countryregion", // AdventureWorks specific for the Name column
        "addresscountry",
        "billingcountry",
        "shippingcountry",
        "mailingcountry"
    };

    // --- 4. Exclusion Words ---
    private static readonly HashSet<string> ExclusionWords = new()
    {
        // Exclude Codes/IDs (Let ISO matchers or FK logic handle these)
        "code", "id", "iso", "key",
        // Exclude visual look-alikes
        "county" // "Orange County" != "Country"
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        // 1. CRITICAL: Length Check
        // If it's 2 or 3 chars, it is likely an ISO code. 
        // Let CommunityCountryISO2Matcher or ISO3Matcher handle it.
        // We only want full names here.
        var maxLength = DataTypeHelper.GetMaxLength(column);
        if (maxLength > 0 && maxLength <= 3) return false;

        // 2. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);

        var colWords = StringUtils.SplitIntoWords(column.ColumnName)
            .Select(s => s.ToLower())
            .ToList();

        // 3. Immediate Disqualification
        if (InvalidSchemaNames.Overlaps(schemaWords)) return false;

        // 4. Negative Checks
        // If it contains "Code", "ID", or "County", abort.
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 5. Strong Normalized Match
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Contains(normalizedCol))
        {
            return true;
        }

        // 6. Token Analysis
        
        // Case A: "Country"
        if (colWords.Contains("country"))
        {
            // We already filtered out "Code", "ID", and "County" above.
            // So "Country" here implies the name.
            return true;
        }

        // Case B: "Nation"
        if (colWords.Contains("nation"))
        {
            return true;
        }

        return false;
    }
}