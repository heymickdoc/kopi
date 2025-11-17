using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityAddressFullMatcher : IColumnMatcher
{
    public int Priority => 10; // Lowest of the address group
    public string GeneratorTypeKey => "address_full";

    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "log", "system", "error", "auth"
    };

    private static readonly HashSet<string> AddressTableContexts = new()
    {
        "address", "location", "customer", "client", "vendor",
        "supplier", "employee", "store", "site", "shipping", "billing",
        "person", "order", "invoice"
    };

    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "address", "fulladdress", "streetaddress", "mailingaddress",
        "billingaddress", "shippingaddress", "homeaddress", "residentialaddress",
        "workaddress", "invoiceaddress", "deliveryaddress", "street", "addr"
    };

    // CRITICAL: These prevent Priority 10 from grabbing Priority 11/12 columns
    private static readonly HashSet<string> ExclusionWords = new()
    {
        "email", "ip", "mac", "web", "url", "link", "host", // Tech
        "city", "state", "zip", "postal", "country", "county", // Geo parts
        "line", "1", "2", "3", "4", "5", "one", "two" // Specific Lines
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName).Select(StringUtils.ToSingular);
        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName).Select(StringUtils.ToSingular);
        var colWords = StringUtils.SplitIntoWords(column.ColumnName).Select(s => s.ToLower()).ToList();

        if (InvalidSchemaNames.Overlaps(schemaWords)) return false;

        // SAFETY CHECK: Abort if it looks like Line 1, Line 2, Email, or City
        if (ExclusionWords.Overlaps(colWords)) return false;

        var hasTableContext = AddressTableContexts.Overlaps(tableWords) || 
                               AddressTableContexts.Overlaps(schemaWords);

        // Case A: Strong Match (e.g. "BillingAddress")
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Any(s => normalizedCol.Contains(s)))
        {
            return true;
        }

        // Case B: Token Match (e.g. "Shipping_Addr")
        var hasAddressWord = colWords.Contains("address") || 
                              colWords.Contains("addr") || 
                              colWords.Contains("street");

        if (hasAddressWord)
        {
            // "Street" usually requires context to avoid generic false positives
            if (colWords.Count == 1 && colWords[0] == "street" && !hasTableContext)
            {
                return false;
            }
            return true;
        }

        return false;
    }
}