using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for ISO 3166-1 alpha-2 Country Codes (e.g. "US", "GB", "FR").
///  Strictly looks for columns with MaxLength == 2.
/// </summary>
public class CommunityCountryISO2Matcher : IColumnMatcher
{
    public int Priority => 11; // High priority for specific code matches
    public string GeneratorTypeKey => "country_iso2";

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
    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "iso2",
        "countryiso2",
        "countrycode2",
        "isocode2",
        "countryid", // Commonly char(2)
        "countrycode", // Ambiguous, but if len=2, it's ISO2
        "countryregioncode" // AdventureWorks specific
    };

    // --- 4. Exclusion Words ---
    private static readonly HashSet<string> ExclusionWords = new()
    {
        // These are often 2 chars, so we must exclude them
        "state", "province", "region", // State codes (e.g. "CA", "NY")
        "language", "lang", // Language codes (e.g. "en", "fr")
        "currency", "curr", // Currency (rarely 2, usually 3, but safe to exclude)
        "area", // Area code
        "phone"
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        // 1. CRITICAL: MaxLength Check
        // ISO2 codes are strictly 2 characters.
        // If the DBA defined it as varchar(50), we treat it as a name, not a code.
        // We strictly require MaxLength == 2 to differentiate from State/Province names.
        var maxLength = DataTypeHelper.GetMaxLength(column);
        if (maxLength != 2) return false;

        // 2. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);

        var colWords = StringUtils.SplitIntoWords(column.ColumnName)
            .Select(s => s.ToLower())
            .ToList();

        // 3. Immediate Disqualification
        if (InvalidSchemaNames.Overlaps(schemaWords)) return false;

        // 4. Negative Checks (The "State Code" Trap)
        // If it contains "State", "Province", or "Language", it is not a Country Code.
        // Exception: "CountryRegionCode" contains "Region", but starts with "Country".
        // We handle the exclusion carefully.
        if (colWords.Contains("state") || colWords.Contains("province") || colWords.Contains("language"))
        {
            return false;
        }

        // 5. Strong Normalized Match
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Contains(normalizedCol))
        {
            return true;
        }

        // 6. Token Analysis
        
        // Case A: "Country" + "Code" / "ISO"
        // Since we already confirmed Length == 2, "CountryCode" implies ISO2.
        var hasCountry = colWords.Contains("country") || colWords.Contains("nation");
        var hasCode = colWords.Contains("code") || colWords.Contains("id") || colWords.Contains("iso");

        if (hasCountry && hasCode)
        {
            return true;
        }

        // Case B: "Country" alone (if length is exactly 2)
        // e.g. Table "Address", Column "Country" (char(2))
        if (hasCountry)
        {
            return true;
        }

        // Case C: "ISO2" explicit mention
        if (colWords.Contains("iso2"))
        {
            return true;
        }

        return false;
    }
}