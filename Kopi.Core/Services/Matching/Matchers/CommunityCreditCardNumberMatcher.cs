using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;
using Kopi.Core.Models.Common;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for Credit Card Numbers (PAN).
///  Strictly validates data type capacity (must hold 13-19 digits) and financial context.
/// </summary>
public class CommunityCreditCardNumberMatcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "credit_card_number";

    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "log", "system", "error", "auth",
        "hr", "humanresources", "resource", "personnel"
    };

    // --- 2. Strong Table Context ---
    private static readonly HashSet<string> FinancialTableContexts = new()
    {
        "payment", "transaction", "order", "invoice", "customer",
        "billing", "sale", "finance", "creditcard", "card", "wallet"
    };

    // --- 3. Strong Column Matches ---
    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "creditcardnumber",
        "creditcardnum",
        "ccnumber",
        "cardnumber",
        "ccn"
    };

    // --- 4. Exclusion Words ---
    private static readonly HashSet<string> ExclusionWords = new()
    {
        "type", "code", "cvv", "cvc", "exp", "date", "name", "holder",
        "id", "key",
        "last4", "suffix", "last"
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        // 1. CAPACITY CHECK
        if (!IsValidCapacity(column)) return false;

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

        // 4. Negative Checks
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 5. Calculate Context
        bool hasFinancialContext = FinancialTableContexts.Overlaps(tableWords) || 
                                   FinancialTableContexts.Overlaps(schemaWords);

        // CASE A: Strong Normalized Match
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Any(s => normalizedCol.Contains(s)))
        {
            return true;
        }

        // CASE B: Token Analysis ("Card" + "Number")
        if (colWords.Contains("card") && (colWords.Contains("number") || colWords.Contains("num")))
        {
            return true;
        }

        // CASE C: "PAN"
        if (colWords.Contains("pan") && hasFinancialContext)
        {
            return true;
        }

        // CASE D: Ambiguous "CreditCard" or "CC" (FIXED)
        // "CreditCard" splits into ["credit", "card"], so we must check for both.
        // "creditcard" (lowercase) stays as ["creditcard"].
        var isCreditCard = (colWords.Contains("credit") && colWords.Contains("card")) || 
                            colWords.Contains("creditcard");
        
        // "CC" is a single token
        var isCC = colWords.Contains("cc");

        if ((isCreditCard || isCC) && hasFinancialContext)
        {
            return true;
        }

        return false;
    }

    private bool IsValidCapacity(ColumnModel column)
    {
        if (DataTypeHelper.IsStringType(column.DataType))
        {
            var maxLen = DataTypeHelper.GetMaxLength(column);
            return maxLen >= 13; 
        }

        var dtype = column.DataType.ToLower();
        if (dtype == "numeric" || dtype == "decimal")
        {
            return column.NumericScale == 0 && column.NumericPrecision >= 13;
        }

        if (DataTypeHelper.IsIntegerType(column.DataType))
        {
            return dtype.Contains("big");
        }

        return false;
    }
}