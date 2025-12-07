using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;
using Kopi.Core.Models.Common;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for Full Name columns (e.g. "John Smith").
///  Matches generic "Name" columns ONLY if they are in a Person/Customer context.
/// </summary>
public class CommunityPersonFullnameMatcher : IColumnMatcher
{
    // Priority 20 is lower than FirstName/LastName (25).
    // This ensures we don't accidentally grab "FirstName" as a FullName.
    public int Priority => 20; 
    public string GeneratorTypeKey => "person_fullname";

    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "log", "system", "error", "auth",
        "config", "setting", "reference", "lookup"
    };

    // --- 2. Strong Table Context ---
    private static readonly HashSet<string> PersonTableContexts = new()
    {
        "user", "customer", "contact", "person", "employee", "staff",
        "member", "account", "client", "partner", "guest", "candidate",
        "profile", "lead", "salesperson"
    };

    // --- 3. Strong Column Matches ---
    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "fullname",
        "completename",
        "personname",
        "displayname" // Often the full name in UI
    };

    // --- 4. Exclusion Words ---
    private static readonly HashSet<string> ExclusionWords = new()
    {
        // Parts of a name (Handled by Priority 25 matchers)
        "first", "last", "middle", "sur", "given", "family", "fore",
        // Specific types that are not "Real Names"
        "user", "login", "nick", "alias", // Usernames/Nicknames
        "file", "product", "item", "group", "role" // Non-human names
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        // 1. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);
        
        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);

        var colWords = StringUtils.SplitIntoWords(column.ColumnName)
            .Select(s => s.ToLower())
            .ToList();

        // 2. Immediate Disqualification
        if (InvalidSchemaNames.Overlaps(schemaWords)) return false;

        // 3. Negative Checks (Safety Valve)
        // Prevents "FirstName", "UserName", "FileName"
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 4. Calculate Context
        // Check if the TABLE implies people (e.g. "Customers")
        bool hasTableContext = PersonTableContexts.Overlaps(tableWords) || 
                               PersonTableContexts.Overlaps(schemaWords);

        // 5. Strong Normalized Match
        // Matches "FullName", "DisplayName"
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Contains(normalizedCol))
        {
            return true;
        }

        // 6. Token Analysis for "Name"
        if (colWords.Contains("name"))
        {
            // Scenario A: "CustomerName", "EmployeeName"
            // The column name itself contains the context.
            if (PersonTableContexts.Overlaps(colWords))
            {
                return true;
            }

            // Scenario B: Generic "Name"
            // Table "Customer", Column "Name" -> MATCH
            // Table "Product", Column "Name" -> FAIL
            if (hasTableContext)
            {
                return true;
            }
        }

        return false;
    }
}