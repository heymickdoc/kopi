using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityDefaultGeometryMatcher : IColumnMatcher
{
    public int Priority => 20;
    public string GeneratorTypeKey => "default_geometry";
    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        return string.Equals(column.DataType, "geometry", StringComparison.OrdinalIgnoreCase);
    }
}