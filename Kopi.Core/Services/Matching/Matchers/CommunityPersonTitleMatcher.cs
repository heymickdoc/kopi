using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for Person Titles / Salutations (e.g. Mr., Mrs., Dr., Ms.).
///  Strictly differentiates from "Job Title", "Book Title", or "Page Title".
/// </summary>
public class CommunityPersonTitleMatcher : IColumnMatcher
{
    public int Priority => 23; 
    public string GeneratorTypeKey => "person_title";

    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "log", "system", "error", "auth",
        "cms", "content", "media", "library" 
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
        "salutation",
        "honorific",
        "courtesytitle",
        "persontitle",
        "nametitle",
        "titleofcourtesy" 
    };

    // --- 4. Exclusion Words ---
    private static readonly HashSet<string> ExclusionWords = new()
    {
        // Employment / HR 
        "job", "work", "role", "position", "rank", "grade", "level", "business",
        // Content / Media
        "book", "page", "movie", "song", "article", "post", "blog", "project", "task",
        // Legal / Assets
        "property", "deed", "asset"
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

        // 3. Negative Checks
        // Prevents "JobTitle", "PageTitle", "ProjectTitle"
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 4. Calculate Context
        var hasPersonContext = PersonTableContexts.Overlaps(tableWords) || 
                                PersonTableContexts.Overlaps(schemaWords);

        // 5. Strong Normalized Match
        // Matches "Salutation", "Honorific"
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Contains(normalizedCol))
        {
            return true;
        }

        // 6. Token Analysis
        
        // Case A: "Courtesy" + "Title"
        if (colWords.Contains("courtesy") && colWords.Contains("title"))
        {
            return true;
        }

        // Case B: "Person" + "Title"
        if (colWords.Contains("person") && colWords.Contains("title"))
        {
            return true;
        }

        // Case C: Generic "Title"
        // This is the specific fix for your concern.
        // We allow "Title" of ANY length, provided:
        // 1. It is NOT excluded (not JobTitle, not PageTitle)
        // 2. It IS in a Person table (User.Title, Contact.Title)
        if (colWords.Contains("title") && hasPersonContext)
        {
            return true;
        }

        return false;
    }
}