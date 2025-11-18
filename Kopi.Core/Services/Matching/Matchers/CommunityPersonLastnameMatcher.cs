using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for Last Name / Surname / Family Name columns.
/// </summary>
public class CommunityPersonLastnameMatcher : IColumnMatcher
{
    public int Priority => 25; // High priority (Same as FirstName)
    public string GeneratorTypeKey => "last_name";

    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "log", "system", "error", "auth",
        "config", "setting"
    };

    // --- 2. Strong Table Context ---
    // Used for resolving ambiguity if needed, though "Surname" is usually distinct.
    private static readonly HashSet<string> PersonTableContexts = new()
    {
        "user", "customer", "contact", "person", "employee", "staff",
        "member", "account", "client", "partner", "guest", "candidate",
        "profile", "identity"
    };

    // --- 3. Strong Column Matches ---
    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "lastname",
        "surname",
        "familyname",
        "lname",
        "last" // Sometimes just "First" and "Last" are used
    };

    // --- 4. Exclusion Words ---
    private static readonly HashSet<string> ExclusionWords = new()
    {
        "first", "middle", "given", "fore", // Prevent "FirstLastName"
        "full", "initial", // Prevent "FullName" or "LastInitial"
        "login", "seen", "modified", "updated", "changed" // Prevent "LastLogin", "LastModified"
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        // 1. Length Check
        // A last name must be at least 2 chars (e.g. "Ng", "Li").
        // If MaxLength is 1, it is likely an Initial.
        if (DataTypeHelper.GetMaxLength(column) == 1) return false;

        // 2. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);

        var colWords = StringUtils.SplitIntoWords(column.ColumnName)
            .Select(s => s.ToLower())
            .ToList();

        // 3. Immediate Disqualification
        if (InvalidSchemaNames.Overlaps(schemaWords)) return false;

        // 4. Negative Checks
        // Critical for avoiding "LastLogin" or "FirstLast"
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 5. Strong Normalized Match
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Contains(normalizedCol))
        {
            return true;
        }

        // 6. Token Analysis

        // Case A: "Last" + "Name"
        // Matches "LastName", "Cust_Last_Name", "Contact_Last_Nm"
        if (colWords.Contains("last") && (colWords.Contains("name") || colWords.Contains("nm")))
        {
            return true;
        }

        // Case B: "Surname"
        if (colWords.Contains("surname"))
        {
            return true;
        }

        // Case C: "Family" + "Name"
        if (colWords.Contains("family") && colWords.Contains("name"))
        {
            return true;
        }

        // Case D: "LName" token
        if (colWords.Contains("lname"))
        {
            return true;
        }

        return false;
    }
}