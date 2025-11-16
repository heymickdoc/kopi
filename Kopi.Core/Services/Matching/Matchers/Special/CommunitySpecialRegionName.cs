using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;

namespace Kopi.Core.Services.Matching.Matchers.Special;

public class CommunitySpecialRegionName
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
        "region",
        "territory"
    };

    public static bool IsMatch(TableModel tableContext, int maxLength)
    {
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);

        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);

        // Region names are almost never 2 or 3 letters
        return !InvalidSchemaNames.Overlaps(schemaWords) &&
               TableNames.Overlaps(tableWords) &&
               maxLength > 3;
    }
}