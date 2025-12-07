using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityDefaultTimeMatcher : IColumnMatcher
{
    public int Priority => 2;
    public string GeneratorTypeKey => "default_time";
    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (string.IsNullOrEmpty(column.DataType)) return false;
        var dataType = column.DataType.ToLower();

        return DataTypeHelper.IsTimeType(dataType);
        
    }
}