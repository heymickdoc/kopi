using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityDefaultStringMatcher : IColumnMatcher
{
    public int Priority => 1;
    public string GeneratorTypeKey => "default_string";

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        return DataTypeHelper.IsStringType(column.DataType);
    }
}