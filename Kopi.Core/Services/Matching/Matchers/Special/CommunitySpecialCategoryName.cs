using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq; // Required for .Select()

namespace Kopi.Core.Services.Matching.Matchers.Special;

/// <summary>
///  Helper to identify if a table represents a Category/Lookup entity.
///  Used when we find a generic "Name" column in a table like "ProductCategory".
/// </summary>
public class CommunitySpecialCategoryName
{
    // --- 1. Safe "Stop Words" ---
    // Categories are universal, but we avoid system/logging tables 
    // where "Name" might be technical.
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "log", "logging", "audit", "history", "backup", "sys", "system"
    };

    // --- 2. Strong Table Keywords ---
    private static readonly HashSet<string> TableNames = new()
    {
        "category",
        "classification",
        "type",   // e.g. "ProductType", "CustomerType"
        "group",  // e.g. "AssetGroup"
        "kind",   // e.g. "AnimalKind"
        "class",  // e.g. "AssetClass"
        "segment" // e.g. "MarketSegment"
    };

    public static bool IsMatch(TableModel tableContext)
    {
        // 1. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);
        
        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);

        // 2. Immediate Disqualification
        // We use the standard Overlaps check here. 
        // Since our invalid list only has simple words ("log", "sys"), 
        // we don't need the complex 'schemaRaw' fix here.
        if (InvalidSchemaNames.Overlaps(schemaWords)) return false;

        // 3. Match Check
        // "ProductCategory" -> ["product", "category"] -> Matches "category"
        // "CustomerType" -> ["customer", "type"] -> Matches "type"
        return TableNames.Overlaps(tableWords);
    }
}