using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityDefaultGuidMatcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "default_guid";

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        return !string.IsNullOrEmpty(column.DataType) && column.DataType.ToLower().Equals("uniqueidentifier");
    }
}