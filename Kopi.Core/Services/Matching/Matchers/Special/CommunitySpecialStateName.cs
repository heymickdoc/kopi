using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers.Special;

public class CommunitySpecialStateName
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
        "state",
        "province"
    };

    public static bool IsMatch(TableModel tableContext, int maxLength)
    {
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);

        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);

        // Check for max length > 2 to avoid 2-letter codes
        return !InvalidSchemaNames.Overlaps(schemaWords) &&
               TableNames.Overlaps(tableWords) &&
               maxLength > 2;
    }
}