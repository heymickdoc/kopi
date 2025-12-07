using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;
using Kopi.Core.Models.Common;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityAddressStateMatcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "address_state";

    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "log", "system", "error", "auth",
        "workflow", "process"
    };

    // --- 2. Strong Table Context ---
    // REMOVED: "order", "invoice"
    // Reason: "Orders.State" is usually a status, not a geography.
    // "BillingState" will still be caught by the explicit column check below.
    private static readonly HashSet<string> AddressTableContexts = new()
    {
        "address", "location", "customer", "client", "vendor",
        "supplier", "employee", "store", "site", "shipping", "billing",
        "person"
    };

    // --- 3. Strong Column Matches ---
    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "stateprovince",
        "addressstate",
        "billingstate",
        "shippingstate",
        "mailingstate",
        "addrstate",
        "statecode",
        "provincecode"
    };

    // --- 4. "State" Disqualifiers ---
    // Added "workflow" to handle "WorkflowState"
    private static readonly HashSet<string> StatusIndicators = new()
    {
        "order", "flow", "workflow", "work", "task", "job", 
        "machine", "sys", "system", "row", "data", "obj", "object", 
        "current", "previous", "next", "lifecycle"
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        // 1. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);
        
        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);

        var colWords = StringUtils.SplitIntoWords(column.ColumnName)
            .Select(s => s.ToLower())
            .ToList();

        // 2. Immediate Disqualification
        if (InvalidSchemaNames.Overlaps(schemaWords)) return false;

        // 3. Check Table Context
        var hasTableContext = AddressTableContexts.Overlaps(tableWords) || 
                               AddressTableContexts.Overlaps(schemaWords);

        // CASE A: Strong Normalized Match
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Any(s => normalizedCol.Contains(s)))
        {
            return true;
        }

        // CASE B: "Province" Analysis (Safe)
        if (colWords.Contains("province"))
        {
            return true;
        }

        // CASE C: "State" Analysis (Ambiguous)
        if (colWords.Contains("state"))
        {
            // 1. Check for explicit address indicators in the column name
            // This ensures "BillingState" works even though we removed "Order" from Table Contexts
            if (colWords.Contains("address") || colWords.Contains("addr") || 
                colWords.Contains("billing") || colWords.Contains("shipping") ||
                colWords.Contains("mailing"))
            {
                return true;
            }

            // 2. Check for "Status" indicators (Negative Check)
            // "WorkflowState" -> ["workflow", "state"] -> "workflow" is in StatusIndicators -> Returns False
            if (StatusIndicators.Overlaps(colWords))
            {
                return false;
            }

            // 3. Generic "State" Column
            // "Orders.State" -> Table "Orders" is NOT in AddressTableContexts -> Returns False
            // "Address.State" -> Table "Address" IS in AddressTableContexts -> Returns True
            if (hasTableContext)
            {
                return true;
            }
        }

        return false;
    }
}