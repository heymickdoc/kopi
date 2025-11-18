using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityDefaultBooleanMatcher : IColumnMatcher
{
    public int Priority => 1;
    public string GeneratorTypeKey => "default_boolean";
    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        return !string.IsNullOrEmpty(column.DataType) && column.DataType.Equals("bit", StringComparison.OrdinalIgnoreCase);
    }
}