using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;
using Kopi.Core.Models.Common;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for "County" columns (e.g. Orange County, Cook County).
///  Strictly distinguishes between "County" and "Country".
/// </summary>
public class CommunityAddressCountyMatcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "address_county";

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
        "person", "order", "invoice"
    };

    // --- 3. Strong Column Matches ---
    // Normalized strings (lowercase, no separators)
    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "county",
        "countyname",
        "addresscounty",
        "billingcounty",
        "shippingcounty",
        "mailingcounty",
        "regioncounty"
    };

    // --- 4. Exclusion Words ---
    private static readonly HashSet<string> ExclusionWords = new()
    {
        // "Country" is the biggest risk. We must let the Country matcher handle it.
        "country", 
        // "Count" (e.g. HeadCount) is usually an Integer, but if it's a string, exclude it.
        "count" 
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        // 1. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);

        var colWords = StringUtils.SplitIntoWords(column.ColumnName)
            .Select(s => s.ToLower())
            .ToList();

        // 2. Immediate Disqualification
        var schemaRaw = tableContext.SchemaName?.ToLower().Replace("_", "") ?? "";
        if (InvalidSchemaNames.Overlaps(schemaWords) || InvalidSchemaNames.Contains(schemaRaw)) 
        {
            return false;
        }

        // 3. Negative Checks
        // Prevents matching "CountryName" or "HeadCount"
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 4. Strong Normalized Match
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Any(s => normalizedCol.Contains(s)))
        {
            return true;
        }

        // 5. Token Analysis
        // "County" is a very specific signal.
        if (colWords.Contains("county"))
        {
            return true;
        }

        return false;
    }
}