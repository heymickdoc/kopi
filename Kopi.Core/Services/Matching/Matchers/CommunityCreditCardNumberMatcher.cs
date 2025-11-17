using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;

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
		"hr", "humanresources" // HR cards are usually ID badges, not Credit Cards
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
		"ccnum",
		"cardnumber",
		"ccn", // Common abbreviation
        "pan"  // Primary Account Number (Banking term)
    };

	// --- 4. Exclusion Words ---
	private static readonly HashSet<string> ExclusionWords = new()
	{
        // Parts of the card that are NOT the number
        "type", "code", "cvv", "cvc", "exp", "date", "name", "holder",
        // Internal IDs (usually integers/FKs, distinct from the PAN)
        "id", "key",
        // "Last 4" columns (too short for a full generator)
        "last4", "suffix", "last"
	};

	public bool IsMatch(ColumnModel column, TableModel tableContext)
	{
		// 1. CAPACITY CHECK (The "Physics" of a Credit Card)
		// A Credit Card is 13-19 digits. We must ensure the column can hold it.
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
		if (InvalidSchemaNames.Overlaps(schemaWords)) return false;

		// 4. Negative Checks
		// Prevents "CardType", "CardExp", "CardID"
		if (ExclusionWords.Overlaps(colWords)) return false;

		// 5. Calculate Context
		bool hasFinancialContext = FinancialTableContexts.Overlaps(tableWords) ||
								   FinancialTableContexts.Overlaps(schemaWords);

		// CASE A: Strong Normalized Match
		// Matches "CCNumber", "CreditCardNum"
		var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
		if (StrongColumnNames.Any(s => normalizedCol.Contains(s)))
		{
			return true;
		}

		// CASE B: Token Analysis ("Card" + "Number")
		// "Card_Number", "CardNum"
		if (colWords.Contains("card") && (colWords.Contains("number") || colWords.Contains("num")))
		{
			return true;
		}

		// CASE C: "PAN" (Primary Account Number)
		// Technical term, requires context to avoid "Pan" (cooking) or "Panel"
		if (colWords.Contains("pan") && hasFinancialContext)
		{
			return true;
		}

		// CASE D: Ambiguous "CreditCard" or "CC"
		// If the column is just "CreditCard", it might mean the number, or a complex object.
		// We accept it if we are in a financial context.
		if (colWords.Contains("creditcard") || (colWords.Contains("cc") && hasFinancialContext))
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Determines if the column physically has the space/type to hold a 16-digit number.
	/// </summary>
	private bool IsValidCapacity(ColumnModel column)
	{
		// String Check
		if (DataTypeHelper.IsStringType(column.DataType))
		{
			var maxLen = DataTypeHelper.GetMaxLength(column);
			// Min 13 (shortest Visa), Max arbitrary but should allow dashes/spaces.
			// If maxLen is 4 ("Last4"), this returns false.
			return maxLen >= 13;
		}

		// Numeric/Decimal Check
		var dtype = column.DataType.ToLower();
		if (dtype == "numeric" || dtype == "decimal")
		{
			// Must hold at least 13 digits, and NO decimals (Scale must be 0)
			return column.NumericScale == 0 && column.NumericPrecision >= 13;
		}

		// Integer Check
		if (DataTypeHelper.IsIntegerType(column.DataType))
		{
			// Standard 'int' max is 2,147,483,647 (10 digits). 
			// A Credit Card is 16 digits. 'int' CANNOT hold a credit card.
			// Only 'bigint' (Int64) is valid.
			return dtype.Contains("big");
		}

		return false;
	}
}