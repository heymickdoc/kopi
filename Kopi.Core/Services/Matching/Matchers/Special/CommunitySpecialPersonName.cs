using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers.Special;

/// <summary>
///  The helper function for matching person "name" column in databases
/// </summary>
public class CommunitySpecialPersonName
{
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production",
        "inventory",
        "product",
        "item",
        "catalog",
        "logistics",
        "warehouse"
    };
    
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
        "member"
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