using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;
using Kopi.Core.Models.Common;

namespace Kopi.Core.Services.Matching.Matchers.Special;

/// <summary>
///  Helper to identify if a table represents a State/Province entity.
///  Used when we find a generic "Name" column in a table like "Address.State".
/// </summary>
public class CommunitySpecialStateName
{
    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "item", "catalog",
        "log", "logging", "audit", "error", "system", "auth", "security",
        "finance", "accounting",
        "workflow", "process", "job", "task" 
    };

    // --- 2. Strong Table Keywords ---
    private static readonly HashSet<string> TableNames = new()
    {
        "state",
        "province",
        "territory"
    };

    public static bool IsMatch(TableModel tableContext, int maxLength)
    {
        // 1. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);

        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);
            
        // 2. Immediate Disqualification (Standard + Raw Check)
        var schemaRaw = tableContext.SchemaName?.ToLower().Replace("_", "") ?? "";
        if (InvalidSchemaNames.Overlaps(schemaWords) || InvalidSchemaNames.Contains(schemaRaw))
        {
            return false;
        }

        // 3. Match Logic
        // - Must be in a State/Province table
        // - Must be > 2 chars (Avoids "StateCode" char(2) columns being treated as Names)
        return TableNames.Overlaps(tableWords) && maxLength > 2;
    }
}