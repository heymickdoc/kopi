using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;

namespace Kopi.Core.Services.Matching.Matchers.Special;

/// <summary>
///  Helper to identify if a table represents a Region/Territory entity.
///  Used when we find a generic "Name" column in a table like "Sales.Territory".
/// </summary>
public class CommunitySpecialRegionName
{
    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "item", "catalog",
        "log", "logging", "audit", "error", "system", "auth", "security",
        "finance", "accounting"
    };

    // --- 2. Strong Table Keywords ---
    private static readonly HashSet<string> TableNames = new()
    {
        "region",
        "territory",
        "district",
        "zone"
    };

    public static bool IsMatch(TableModel tableContext, int maxLength)
    {
        // 1. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);

        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);
            
        // 2. Immediate Disqualification (Standard + Raw Check)
        // Future-proofs against compound invalid schemas
        var schemaRaw = tableContext.SchemaName?.ToLower().Replace("_", "") ?? "";
        if (InvalidSchemaNames.Overlaps(schemaWords) || InvalidSchemaNames.Contains(schemaRaw))
        {
            return false;
        }

        // 3. Match Logic
        // - Must be in a Region/Territory table
        // - Must be > 3 chars (Avoids "ID" columns or very short codes like "US")
        return TableNames.Overlaps(tableWords) && maxLength > 3;
    }
}