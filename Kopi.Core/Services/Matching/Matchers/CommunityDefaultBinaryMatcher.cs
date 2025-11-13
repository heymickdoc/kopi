using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityDefaultBinaryMatcher : IColumnMatcher
{
    public int Priority => 1;
    public string GeneratorTypeKey => "default_binary";
    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        return DataTypeHelper.IsBinaryType(column.DataType);
    }
}