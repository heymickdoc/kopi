using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityPersonDateOfBirthMatcher : IColumnMatcher
{
    public int Priority => 25;
    public string GeneratorTypeKey => "person_dob";
    
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
        "dateofbirth",
        "dob",
        "birthdate",
        "birth_date",
        "dobirth",
        "d_birth"
    };
    
    // --- 4. Exclusion Words ---
    private static readonly HashSet<string> ExclusionWords = new()
    {
        "age", "created", "modified", "updated", "timestamp", "time",
        "start", "end", "dateadded", "date_created", "date_modified"
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        // 1. Data Type Check
        // We only want date/datetime columns. 
        if (!DataTypeHelper.IsDateType(column.DataType)) return false;

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
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 4. Strong Normalized Match (e.g. "DateOfBirth")
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Contains(normalizedCol))
        {
            return true;
        }

        // 5. Context Analysis
        // If the table is "Person" and the column contains "birth", it's likely a DOB
        var isPersonTable = PersonTableContexts.Overlaps(tableWords) || PersonTableContexts.Overlaps(schemaWords);
        
        if (isPersonTable && colWords.Contains("birth"))
        {
            return true;
        }

        return false;
    }
}