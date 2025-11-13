using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityDefaultXMLMatcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "default_xml";

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        return column.DataType.ToLower().Equals("xml");
    }
}