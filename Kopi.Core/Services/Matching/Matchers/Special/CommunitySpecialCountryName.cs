using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers.Special;

public class CommunitySpecialCountryName
{
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production",
        "inventory",
        "product",
        "item",
        "catalog",
        "log",
        "logging",
        "audit",
        "error",
        "system",
        "auth",
        "security",
        "finance",
        "accounting"
    };

    private static readonly HashSet<string> TableNames = new()
    {
        "location",
        "country",
        "nation"
    };

    public static bool IsMatch(TableModel tableContext, int maxLength)
    {
        // 1. Tokenize and normalize the schema name
        // e.g., "Production" -> ["production"]
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);

        // 2. Tokenize and normalize the table name
        // e.g., "ProductInventories" -> ["product", "inventory"]
        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);

        return !InvalidSchemaNames.Overlaps(schemaWords) && TableNames.Overlaps(tableWords) && maxLength > 8;
    }
}