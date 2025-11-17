using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for ISO 3166-1 alpha-3 Country Codes (e.g. "USA", "GBR", "FRA").
///  Strictly looks for columns with MaxLength == 3.
/// </summary>
public class CommunityCountryISO3Matcher : IColumnMatcher
{
    public int Priority => 11;
    public string GeneratorTypeKey => "country_iso3";

    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "log", "system", "error", "auth"
    };

    // --- 2. Strong Column Matches ---
    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "iso3",
        "countryiso3",
        "countrycode3",
        "isocode3",
        "countryid", // Can be char(3)
        "countrycode", // If len=3, it's ISO3
        "countryregioncode"
    };

    // --- 3. Exclusion Words ---
    private static readonly HashSet<string> ExclusionWords = new()
    {
        // Currency codes are the biggest risk for char(3)
        "currency", "curr", 
        // Language codes (ISO 639-2/T) are 3 chars
        "language", "lang",
        // State/Province abbreviations are rarely 3, but possible
        "state", "province",
        "area", "phone" // Area codes
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        // 1. CRITICAL: MaxLength Check
        // ISO3 codes are strictly 3 characters.
        var maxLength = DataTypeHelper.GetMaxLength(column);
        if (maxLength != 3) return false;

        // 2. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);

        var colWords = StringUtils.SplitIntoWords(column.ColumnName)
            .Select(s => s.ToLower())
            .ToList();

        // 3. Immediate Disqualification
        if (InvalidSchemaNames.Overlaps(schemaWords)) return false;

        // 4. Negative Checks (The "Currency" Trap)
        // If it contains "Currency", "Language", etc., abort.
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 5. Strong Normalized Match
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Contains(normalizedCol))
        {
            return true;
        }

        // 6. Token Analysis
        
        // Case A: "Country" + "Code" / "ISO"
        // Since we confirmed Length == 3, "CountryCode" implies ISO3.
        var hasCountry = colWords.Contains("country") || colWords.Contains("nation");
        var hasCode = colWords.Contains("code") || colWords.Contains("id") || colWords.Contains("iso");

        if (hasCountry && hasCode)
        {
            return true;
        }

        // Case B: "Country" alone (if length is exactly 3)
        if (hasCountry)
        {
            return true;
        }

        // Case C: "ISO3" explicit mention
        if (colWords.Contains("iso3"))
        {
            return true;
        }

        return false;
    }
}