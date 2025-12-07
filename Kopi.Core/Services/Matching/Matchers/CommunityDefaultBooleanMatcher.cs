using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityDefaultBooleanMatcher : IColumnMatcher
{
    public int Priority => 1;
    public string GeneratorTypeKey => "default_boolean";
    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        var dataType = column.DataType.ToLower();
        
        return DataTypeHelper.IsBooleanType(dataType);
    }
}