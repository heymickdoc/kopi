using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityDefaultDecimalMatcher : IColumnMatcher
{
    public int Priority => 1;
    public string GeneratorTypeKey => "default_decimal";
    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        return !string.IsNullOrEmpty(column.DataType) && DataTypeHelper.IsDecimalType(column.DataType);
    }
}