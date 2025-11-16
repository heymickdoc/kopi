using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers.Special;

/// <summary>
/// The helper function for matching product name columns in databases
/// </summary>
public class CommunitySpecialProductName
{
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "contact",
        "user",
        "hr",
        "humanresources",
        "employee",
        "staff",
        "personnel",
        "address",
        "location",
    };
    
    /// <summary>
    /// Names of tables that are likely to contain product names
    /// </summary>
    private static readonly HashSet<string> TableNames = new()
    {
        "production",
        "product",
        "item",
        "inventory",
        "catalog",
        "sku",
        "stockkeepingunit",
        "merchandise",
        "good",
        "wares",
        "logistics",
        "warehouse"
    };

    public static bool IsMatch(TableModel tableContext)
    {
        // 1. Tokenize and normalize the schema name
        // e.g., "Production" -> ["production"]
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);
        
        // 2. Tokenize and normalize the table name
        // e.g., "ProductInventories" -> ["product", "inventory"]
        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);
        
        return !InvalidSchemaNames.Overlaps(schemaWords) && TableNames.Overlaps(tableWords);
    }
}