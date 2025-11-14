using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityDefaultTimeMatcher : IColumnMatcher
{
    public int Priority => 2;
    public string GeneratorTypeKey => "default_time";
    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        var dataType = column.DataType.ToLower();
        
        var isTimeType = dataType is "time" or "time2";
        return isTimeType;
    }
}