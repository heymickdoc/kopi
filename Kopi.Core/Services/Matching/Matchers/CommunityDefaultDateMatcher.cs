using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityDefaultDateMatcher : IColumnMatcher
{
    public int Priority => 1;
    public string GeneratorTypeKey => "default_date";

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (string.IsNullOrEmpty(column.DataType)) return false;
        
        var dataType = column.DataType.ToLower();
        
        var isDateType = DataTypeHelper.IsDateType(dataType);
        return isDateType;
    }
}