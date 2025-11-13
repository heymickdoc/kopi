using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityDefaultJsonMatcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "default_json";
    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        return column.DataType.ToLower().Equals("json");
    }
}