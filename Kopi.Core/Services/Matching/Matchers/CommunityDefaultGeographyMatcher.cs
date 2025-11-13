using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityDefaultGeographyMatcher : IColumnMatcher
{
    /// <summary>
    /// The priority is a bit higher for this default matcher as it's an unusual data type so we want to
    /// be able to catch it before other more generic matchers.
    /// </summary>
    public int Priority => 20;
    public string GeneratorTypeKey => "default_geography";
    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        return string.Equals(column.DataType, "geography", StringComparison.OrdinalIgnoreCase);
    }
}