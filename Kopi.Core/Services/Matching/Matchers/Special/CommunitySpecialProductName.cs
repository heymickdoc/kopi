using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;
using Kopi.Core.Models.Common;

namespace Kopi.Core.Services.Matching.Matchers.Special;

/// <summary>
/// The helper function for identifying Product-context tables.
/// Used when we find a generic "Name" column in a table like "Product" or "Inventory".
/// </summary>
public class CommunitySpecialProductName
{
    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        // People / HR Contexts
        "contact", "user", "hr", "humanresources", "employee", 
        "staff", "personnel", "person",
        "address", "location", "geo",
        "system", "log", "auth", "security", "config"
    };
    
    // --- 2. Strong Table Keywords ---
    private static readonly HashSet<string> TableNames = new()
    {
        "production", "product", "item", "inventory", "catalog",
        "sku", "stockkeepingunit", "merchandise", "good", "wares",
        "logistics", "warehouse", 
        "material", "asset", "stock"
    };

    public static bool IsMatch(TableModel tableContext)
    {
        // 1. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);
        
        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);
        
        // 2. Immediate Disqualification (Standard + Raw Check)
        // CRITICAL FIX: "HumanResources" -> "humanresources".
        // Without this, the tokenizer splits it into ["human", "resource"] 
        // and fails to match the "humanresources" entry in the set.
        var schemaRaw = tableContext.SchemaName?.ToLower().Replace("_", "") ?? "";
        if (InvalidSchemaNames.Overlaps(schemaWords) || InvalidSchemaNames.Contains(schemaRaw))
        {
            return false;
        }

        // 3. Match Check
        return TableNames.Overlaps(tableWords);
    }
}