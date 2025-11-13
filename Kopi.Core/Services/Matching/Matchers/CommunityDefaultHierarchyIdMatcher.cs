using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityDefaultHierarchyIdMatcher : IColumnMatcher
{
    public int Priority => 20;
    public string GeneratorTypeKey => "default_hierarchyid";
    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        return column.DataType.ToLower().Equals("hierarchyid");
    }
}