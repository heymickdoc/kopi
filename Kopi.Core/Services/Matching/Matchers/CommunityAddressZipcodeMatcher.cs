using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;
using Kopi.Core.Models.Common;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for US-style "Zip Code" columns.
/// </summary>
public class CommunityAddressZipcodeMatcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "address_zipcode";

    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "log", "system", "error", "auth"
    };

    // --- 2. Strong Table Context (Optional but good for safety) ---
    private static readonly HashSet<string> AddressTableContexts = new()
    {
        "address", "location", "customer", "client", "vendor",
        "supplier", "employee", "store", "site", "shipping", "billing",
        "person", "order", "invoice"
    };

    // --- 3. Strong Column Matches ---
    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "zipcode",
        "zip",
        "addresszip",
        "billingzip",
        "shippingzip",
        "mailingzip"
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        // Note: If you support integer Zip codes, remove the IsStringType check.
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        // 1. Tokenize
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);

        // Keep column words raw (lowercase)
        var colWords = StringUtils.SplitIntoWords(column.ColumnName)
            .Select(s => s.ToLower())
            .ToList();

        // 2. Immediate Disqualification
        if (InvalidSchemaNames.Overlaps(schemaWords)) return false;

        // 3. Strong Normalized Match
        // Matches "ZipCode", "BillingZip", "Zip"
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Any(s => normalizedCol.Contains(s)))
        {
            return true;
        }

        // 4. Token Analysis
        // "Zip" is a very specific, strong signal. We generally trust it.
        if (colWords.Contains("zip"))
        {
            return true;
        }

        return false;
    }
}