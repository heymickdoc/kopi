using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for First Name / Given Name columns.
/// </summary>
public class CommunityPersonFirstnameMatcher : IColumnMatcher
{
    public int Priority => 25; // High priority to grab "FirstName" before generic "Name"
    public string GeneratorTypeKey => "person_firstname";

    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "log", "system", "error", "auth",
        "config", "setting"
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
        "firstname",
        "fname",
        "givenname",
        "forename",
        "christianname", // Legacy/Regional
        "first" // Sometimes just "First" and "Last"
    };

    // --- 4. Exclusion Words ---
    private static readonly HashSet<string> ExclusionWords = new()
    {
        "last", "middle", "sur", "family", // Prevent "LastFirstName" or "MiddleName"
        "initial", // Prevent "FirstInitial"
        "full"     // Prevent "FullName"
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        // 1. Length Check
        // A first name must be at least 2 chars (e.g. "Bo", "Ty").
        // If MaxLength is 1, it is likely an Initial (e.g. "FInitial").
        if (DataTypeHelper.GetMaxLength(column) == 1) return false;

        // 2. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);
        
        // We usually don't strictly need table words for "FirstName" because the column is specific,
        // but we keep the context list available if we want to enforce it for weak matches.
        
        var colWords = StringUtils.SplitIntoWords(column.ColumnName)
            .Select(s => s.ToLower())
            .ToList();

        // 3. Immediate Disqualification
        if (InvalidSchemaNames.Overlaps(schemaWords)) return false;

        // 4. Negative Checks
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 5. Strong Normalized Match
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Contains(normalizedCol))
        {
            return true;
        }

        // 6. Token Analysis
        
        // Case A: "First" + "Name"
        if (colWords.Contains("first") && (colWords.Contains("name") || colWords.Contains("nm")))
        {
            return true;
        }

        // Case B: "Given" + "Name"
        if (colWords.Contains("given") && colWords.Contains("name"))
        {
            return true;
        }

        // Case C: "Fore" + "Name"
        if (colWords.Contains("fore") && colWords.Contains("name"))
        {
            return true;
        }
        
        // Case D: "FName" token
        if (colWords.Contains("fname"))
        {
            return true;
        }

        return false;
    }
}