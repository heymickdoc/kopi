using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityAddressLine1Matcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "address_line1";
    
    private static readonly HashSet<string> ColumnNames = new()
    {
        "addr1",
        "addrline1",
        "address1",
        "addressline1",
        "billing1",
        "billingaddress1",
        "home1",
        "homeaddress1",
        "invoice1",
        "invoiceaddress1",
        "line1",
        "mailing1",
        "mailingaddress1",
        "residential1",
        "residentialaddress1",
        "shipping1",
        "shippingaddress1",
        "street1",
        "streetaddress1"
    };
    
    private static readonly HashSet<string> TableNames = new()
    {
        "address",
        "location",
        "customer",
        "user"
    };

    private static readonly HashSet<string> SchemaNames = new()
    {
        "person",
        "customer",
        "location",
        "address"
    };
    
    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        var score = 0;

        var colName = new string(column.ColumnName.ToLower().Where(char.IsLetterOrDigit).ToArray());
        var tableName = tableContext.TableName.ToLower();
        var schemaName = tableContext.SchemaName.ToLower();

        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        //Exact match
        if (ColumnNames.Contains(colName))
        {
            score += 32;
        }
        
        //Partial match
        if (ColumnNames.Any(k => colName.Contains(k)))
        {
            score += 16;
        }
        
        if (TableNames.Any(k => tableName.Contains(k)))
        {
            score += 8;
        }

        if (SchemaNames.Any(k => schemaName.Contains(k)))
        {
            score += 4;
        }

        return score >= 32;
    }
}