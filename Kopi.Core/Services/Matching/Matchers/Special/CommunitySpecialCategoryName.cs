using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers.Special;

public class CommunitySpecialCategoryName
{
    private static readonly HashSet<string> TableNames = new()
    {
        "category",
        "classification"
    };

    public static bool IsMatch(TableModel tableContext)
    {
        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);

        return TableNames.Overlaps(tableWords);
    }
}