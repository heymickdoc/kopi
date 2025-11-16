using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;

namespace Kopi.Core.Services.Matching.Matchers.Special;

public class CommunitySpecialStatusName
{
    private static readonly HashSet<string> TableNames = new()
    {
        "status",
        "stage"
    };

    public static bool IsMatch(TableModel tableContext)
    {
        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);

        return TableNames.Overlaps(tableWords);
    }
}