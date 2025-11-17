using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;


/// <summary>
///  Matcher for community "address line 1" columns.
/// </summary>
public class CommunityAddressLine1Matcher : IColumnMatcher
{
    public int Priority => 11;
    public string GeneratorTypeKey => "address_line1";

    // If a schema matches these, we abort immediately (e.g. avoiding "Line1" in a manufacturing system)
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production",
        "inventory",
        "product",
        "log",
        "system",
        "error",
        "auth"
    };

    // Tables that strongly suggest address data
    private static readonly HashSet<string> AddressTableContexts = new()
    {
        "address",
        "location",
        "customer",
        "client",
        "vendor",
        "supplier",
        "employee",
        "store",
        "site",
        "shipping",
        "billing"
    };

    // These are almost always addresses, regardless of table name
    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "address1",
        "addr1",
        "street1",
        "streetaddress1",
        "addressline1",
        "addrline1"
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        // 1. Tokenize inputs AND SINGULARIZE
        // This ensures "Customers" becomes "customer", matching your HashSet
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular); 
            
        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);
            
        // Keep column words as ToLower() list for easier "Contains" checking
        var colWords = StringUtils.SplitIntoWords(column.ColumnName)
            .Select(s => s.ToLower())
            .ToList();

        // 2. Immediate Disqualification
        if (InvalidSchemaNames.Overlaps(schemaWords)) return false;

        // 3. Check Table Context
        var hasTableContext = AddressTableContexts.Overlaps(tableWords) || 
                               AddressTableContexts.Overlaps(schemaWords);

        // CASE A: Strong Normalized Match (Exact "Address1" style)
        // This handles: "Address1", "Addr1", "AddressLine1"
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Any(s => normalizedCol.Contains(s)))
        {
            return true;
        }

        // --- TOKEN ANALYSIS ---
        
        // Helpers to find the "1" signal
        // Depending on splitting, it might be "Line1" (one word) or "Line", "1" (two words)
        var hasNumberOne = colWords.Contains("1") || colWords.Contains("one") || colWords.Contains("line1");
        
        // CASE B: "Address 1" or "Street 1"
        // We ONLY accept "Address" or "Street" if they are accompanied by a "1".
        // "Address" -> Fails (No '1') -> Goes to Full Matcher
        // "Address1" -> Passes
        var hasAddressWord = colWords.Contains("address") || colWords.Contains("addr") || colWords.Contains("street");
        
        if (hasAddressWord && hasNumberOne)
        {
            return true;
        }

        // CASE C: "Line 1"
        // "Line1" is ambiguous (could be Order Line 1), so it strictly requires Table Context.
        var hasLineWord = colWords.Contains("line") || colWords.Contains("line1");
        
        if (hasLineWord && hasNumberOne && hasTableContext) return true;

        return false;
    }
}