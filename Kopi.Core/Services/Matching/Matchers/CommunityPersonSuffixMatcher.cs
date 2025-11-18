using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for Person Name Suffixes (e.g., Jr., Sr., III, PhD).
///  Strictly separates name suffixes from File, Domain, or Street suffixes.
/// </summary>
public class CommunityPersonSuffixMatcher : IColumnMatcher
{
    public int Priority => 23; // Same level as Middle Name
    public string GeneratorTypeKey => "person_suffix";

    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "log", "system", "error", "auth",
        "config", "file", "document" // Added file/doc contexts
    };

    // --- 2. Strong Table Context ---
    private static readonly HashSet<string> PersonTableContexts = new()
    {
        "user", "customer", "contact", "person", "employee", "staff",
        "member", "account", "client", "partner", "guest", "candidate",
        "profile", "identity"
    };

    // --- 3. Strong Column Matches ---
    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "suffix",
        "personsuffix",
        "namesuffix",
        "courtesysuffix", // Often used for Titles/Suffixes
        "generationsuffix" // Jr/Sr specific
    };

    // --- 4. Exclusion Words ---
    // "Suffix" is very common in non-human contexts. We must exclude them.
    private static readonly HashSet<string> ExclusionWords = new()
    {
        // Technical / File System
        "file", "path", "url", "domain", "host", "dns", "network", "doc",
        // Address Components (Street Suffix = "St", "Ave")
        "street", "address", "addr", "road", "way", "location",
        // Finance
        "account", "card" // "AccountSuffix"
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        // 1. Length Check
        // Name suffixes are short (Jr, Sr, III, PhD). 
        // If it's varchar(100), it's likely a file path or description.
        // 10 is a safe upper bound (e.g. "Esq.").
        var maxLength = DataTypeHelper.GetMaxLength(column);
        if (maxLength > 15) return false;

        // 2. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);
        
        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);

        var colWords = StringUtils.SplitIntoWords(column.ColumnName)
            .Select(s => s.ToLower())
            .ToList();

        // 3. Immediate Disqualification
        if (InvalidSchemaNames.Overlaps(schemaWords)) return false;

        // 4. Negative Checks (Critical for Suffixes)
        // Prevents "FileSuffix", "DomainSuffix", "StreetSuffix"
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 5. Calculate Context
        var hasPersonContext = PersonTableContexts.Overlaps(tableWords) || 
                                PersonTableContexts.Overlaps(schemaWords);

        // 6. Strong Normalized Match
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Contains(normalizedCol))
        {
            // If the column is just "Suffix", we REQUIRE context.
            // "Person.Suffix" -> MATCH
            // "Product.Suffix" -> FAIL (Could be a SKU suffix)
            if (normalizedCol == "suffix" && !hasPersonContext)
            {
                return false;
            }

            return true;
        }

        // 7. Token Analysis
        
        // Case A: "Name" + "Suffix"
        if (colWords.Contains("name") && colWords.Contains("suffix"))
        {
            return true;
        }

        // Case B: "Person" + "Suffix"
        if (colWords.Contains("person") && colWords.Contains("suffix"))
        {
            return true;
        }

        // Case C: Generic "Suffix" token
        // Must have Person Context to be accepted.
        if (colWords.Contains("suffix") && hasPersonContext)
        {
            return true;
        }

        return false;
    }
}