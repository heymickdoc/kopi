using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityDefaultIntegerMatcher : IColumnMatcher
{
    public int Priority => 5;
    public string GeneratorTypeKey => "default_integer";

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        return DataTypeHelper.IsIntegerType(column.DataType);
    }
}