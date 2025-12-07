using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;
using Kopi.Core.Models.Common;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for Credit Card Expiration Dates.
///  Handles full dates (Date/DateTime) or component parts (ExpMonth, ExpYear).
/// </summary>
public class CommunityCreditCardDateMatcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "credit_card_date";

    // --- 1. Safe "Stop Words" ---
    // Avoid product inventory, human resources (certifications), or system logs
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "humanresources", "employee", "log", "system"
    };

    // --- 2. Strong Table Context ---
    // Tables where financial data lives
    private static readonly HashSet<string> FinancialTableContexts = new()
    {
        "payment", "transaction", "order", "invoice", "customer",
        "billing", "sale", "finance", "creditcard", "card"
    };

    // --- 3. Strong Column Matches ---
    // Normalized strings (lowercase, no separators)
    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "creditcardexpdate",
        "creditcardexpiration",
        "ccexpdate",
        "ccexpiration",
        "cardexpdate",
        "cardexpiration",
        "ccexpiry",
        "cardexpiry",
        "creditcardexpiry",
        "expirationmonth", // Specific components
        "expirationyear",
        "expmonth",
        "expyear"
    };

    // --- 4. Exclusion Words ---
    private static readonly HashSet<string> ExclusionWords = new()
    {
        // Technical or PII fields that are NOT dates
        "number", "num", "pan", "code", "cvv", "cvc", "name", "type", "id",
        // Contexts that imply product expiration
        "batch", "lot", "shelf", "warranty"
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        // 1. Data Type Check
        // Expiration dates can be Strings ("10/25"), Dates (2025-10-01), or Integers (10, 2025)
        var isDate = DataTypeHelper.IsDateType(column.DataType);
        var isString = DataTypeHelper.IsStringType(column.DataType);
        var isInt = DataTypeHelper.IsIntegerType(column.DataType);

        if (!isDate && !isString && !isInt) return false;

        // 2. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);
        
        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);

        var colWords = StringUtils.SplitIntoWords(column.ColumnName)
            .Select(s => s.ToLower())
            .ToList();

        // 3. Immediate Disqualification
        var schemaRaw = tableContext.SchemaName?.ToLower().Replace("_", "") ?? "";
        if (InvalidSchemaNames.Overlaps(schemaWords) || InvalidSchemaNames.Contains(schemaRaw)) 
        {
            return false;
        }

        // 4. Negative Checks (Safety Valve)
        // Prevents matching "CardNumber" or "BatchExpDate"
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 5. Calculate Context
        bool hasFinancialContext = FinancialTableContexts.Overlaps(tableWords) || 
                                   FinancialTableContexts.Overlaps(schemaWords);

        // CASE A: Strong Normalized Match
        // Matches "CCExpDate", "ExpYear"
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Any(s => normalizedCol.Contains(s)))
        {
            // Even for a strong match like "ExpYear", we want to ensure we aren't
            // in a totally unrelated table (like "Inventory.Product").
            // But since we already passed InvalidSchemaNames, we can generally trust these specific keys.
            return true;
        }

        // CASE B: Generic "ExpDate" or "Expiration"
        // "ExpDate" is ambiguous (Credit Card vs Milk vs License).
        // We strictly require Financial Context.
        var hasExpiration = colWords.Contains("expiration") || 
                             colWords.Contains("expiry") || 
                             (colWords.Contains("exp") && colWords.Contains("date"));

        if (hasExpiration)
        {
            // If the column also says "Card" or "CC", we trust it.
            if (colWords.Contains("card") || colWords.Contains("cc"))
            {
                return true;
            }

            // Otherwise (just "ExpDate"), we MUST have table context.
            // "Payment.ExpDate" -> TRUE
            // "Product.ExpDate" -> FALSE (Filtered by InvalidSchemaNames or fail here)
            if (hasFinancialContext)
            {
                return true;
            }
        }

        return false;
    }
}