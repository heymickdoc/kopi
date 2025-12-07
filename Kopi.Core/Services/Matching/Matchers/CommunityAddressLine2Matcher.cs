using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityAddressLine2Matcher : IColumnMatcher
{
    public int Priority => 11;
    public string GeneratorTypeKey => "address_line2";
    
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
        "address2",
        "addr2",
        "street2",
        "streetaddress2",
        "addressline2",
        "addrline2"
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

        // CASE A: Strong Normalized Match (Exact "Address2" style)
        // This handles: "Address2", "Addr2", "AddressLine2"
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Any(s => normalizedCol.Contains(s)))
        {
            return true;
        }

        // --- TOKEN ANALYSIS ---
        
        // Helpers to find the "2" signal
        // Depending on splitting, it might be "Line2" (one word) or "Line", "2" (two words)
        var hasNumberTwo = colWords.Contains("2") || colWords.Contains("two") || colWords.Contains("line2");
        
        // CASE B: "Address 2" or "Street 2"
        // We ONLY accept "Address" or "Street" if they are accompanied by a "2".
        // "Address" -> Fails (No '2') -> Goes to Full Matcher
        // "Address2" -> Passes
        var hasAddressWord = colWords.Contains("address") || colWords.Contains("addr") || colWords.Contains("street");
        
        if (hasAddressWord && hasNumberTwo)
        {
            return true;
        }

        // CASE C: "Line 2"
        // "Line2" is ambiguous (could be Order Line 2), so it strictly requires Table Context.
        var hasLineWord = colWords.Contains("line") || colWords.Contains("line2");
        
        if (hasLineWord && hasNumberTwo && hasTableContext) return true;

        return false;
    }
}