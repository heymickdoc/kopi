using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityDefaultMoneyMatcher : IColumnMatcher
{
    public int Priority => 5;
    public string GeneratorTypeKey => "default_money";

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        return DataTypeHelper.IsMoneyType(column.DataType);
    }
}