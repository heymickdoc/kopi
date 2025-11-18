using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;

namespace Kopi.Core.Services.Matching.Matchers.Special;

/// <summary>
///  The helper function for identifying Person-context tables.
///  Used when we find a generic "Name" column in a table like "Customer" or "Employee".
/// </summary>
public class CommunitySpecialPersonName
{
    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production",
        "inventory",
        "product",
        "item",
        "catalog",
        "logistics",
        "warehouse",
        "asset",
        "system",
        "log"
    };
    
    // --- 2. Strong Table Keywords ---
    private static readonly HashSet<string> TableNames = new()
    {
        "user",
        "customer",
        "contact",
        "person",
        "employee",
        "staff",
        "personnel",
        "client",
        "subscriber",
        "member",
        "partner",
        "candidate",
        "lead"
    };
    
    public static bool IsMatch(TableModel tableContext)
    {
        // 1. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);
        
        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);
        
        // 2. Immediate Disqualification (Standard + Raw Check)
        // Future-proofs against compound invalid schemas (e.g. "SystemLogs")
        var schemaRaw = tableContext.SchemaName?.ToLower().Replace("_", "") ?? "";
        if (InvalidSchemaNames.Overlaps(schemaWords) || InvalidSchemaNames.Contains(schemaRaw))
        {
            return false;
        }

        // 3. Match Check
        return TableNames.Overlaps(tableWords);
    }
}