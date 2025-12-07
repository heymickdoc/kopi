using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers.Special;

public class CommunitySpecialCityName
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
        "city",
        "town",
        "municipality",
        "suburb",
        "village",
        "metro"
    };

    public static bool IsMatch(TableModel tableContext, int maxLength)
    {
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);

        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);

        // Avoids 2-letter codes, which are never cities
        return !InvalidSchemaNames.Overlaps(schemaWords) &&
               TableNames.Overlaps(tableWords) &&
               maxLength > 2;
    }
}