using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;
using Kopi.Core.Models.Common;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for City / Town / Municipality columns.
///  Handles "City" generic matches while avoiding words like "Capacity" or "Velocity".
/// </summary>
public class CommunityAddressCityMatcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "address_city";

    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "log", "system", "error", "auth",
        "capacity" // "Capacity" often appears in schema names too
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
        "city",
        "cityname",
        "town",
        "townname",
        "municipality",
        "suburb",
        "village",
        "metro",
        "addresscity",
        "billingcity",
        "shippingcity",
        "mailingcity"
    };

    // --- 4. Exclusion Words ---
    // Words that end in "city" or look like cities but aren't.
    private static readonly HashSet<string> ExclusionWords = new()
    {
        "capacity", "velocity", "electricity", "scarcity", "publicity",
        "simplicity", "elasticity", "multiplicity", "authenticity",
        "id", "code", "key" // Avoid "CityID" or "CityCode" (usually FKs)
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
        if (InvalidSchemaNames.Overlaps(schemaWords)) return false;

        // 3. Negative Checks (The "Capacity" Trap)
        // If the column contains "Capacity", "Velocity", or "ID", abort.
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 4. Strong Normalized Match
        // Matches "CityName", "BillingCity"
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Any(s => normalizedCol.Contains(s)))
        {
            return true;
        }

        // 5. Token Analysis
        // "City", "Town", "Municipality" are strong signals.
        // Because we used SplitIntoWords, "Capacity" becomes ["capacity"], 
        // so it won't falsely trigger colWords.Contains("city").
        if (colWords.Contains("city") || 
            colWords.Contains("town") || 
            colWords.Contains("municipality") ||
            colWords.Contains("suburb"))
        {
            return true;
        }

        return false;
    }
}