using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for Middle Name or Middle Initial columns.
/// </summary>
public class CommunityPersonMiddlenameMatcher : IColumnMatcher
{
    public int Priority => 23; // Lower than First/Last (25), Higher than Full (20)
    public string GeneratorTypeKey => "person_middlename";

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
        "middlename",
        "mname",
        "middleinitial",
        "minitial",
        "midname"
    };

    // --- 4. Exclusion Words ---
    private static readonly HashSet<string> ExclusionWords = new()
    {
        "first", "last", "sur", "family", "given", // Prevent overlapping with other name parts
        "class", "tier" // "MiddleClass", "MiddleTier"
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

        // 3. Negative Checks
        // Prevent "FirstMiddleName" or "MiddleClass"
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 4. Strong Normalized Match
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Contains(normalizedCol))
        {
            return true;
        }

        // 5. Token Analysis
        
        // Case A: "Middle" + "Name"
        if (colWords.Contains("middle") && (colWords.Contains("name") || colWords.Contains("nm")))
        {
            return true;
        }

        // Case B: "Middle" + "Initial"
        if (colWords.Contains("middle") && (colWords.Contains("initial") || colWords.Contains("init")))
        {
            return true;
        }

        // Case C: "M" + "Initial" (e.g. "MInitial")
        // We have to be careful not to match "M" as "Male". 
        // "MInitial" or "M_Initial" is safe.
        if (colWords.Contains("m") && (colWords.Contains("initial") || colWords.Contains("init")))
        {
            return true;
        }

        return false;
    }
}