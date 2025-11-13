using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityAddressFullMatcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "address_full";

    private static readonly HashSet<string> ColumnNames = new()
    {
        "address",
        "fulladdress",
        "addressfull",
        "streetaddress",
        "addressstreet",
        "mailingaddress",
        "addressmailing",
        "residentialaddress",
        "addressresidential",
        "shippingaddress",
        "addressshipping",
        "homeaddress",
        "addresshome",
        "billingaddress",
        "addressbilling",
        "invoiceaddress",
        "addressinvoice",
        "addr",
        "addressline",
        "addresslinefull",
        "fulladdressline"
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

        return score >= 20;
    }
}