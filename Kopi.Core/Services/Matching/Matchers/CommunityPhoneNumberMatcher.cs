using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;
using Kopi.Core.Models.Common;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for Phone Number columns (Mobile, Home, Work, Fax).
///  Strictly distinguishes "Phone" from "Microphone" or "Headphone".
/// </summary>
public class CommunityPhoneNumberMatcher : IColumnMatcher
{
    public int Priority => 15;
    public string GeneratorTypeKey => "phone_number";

    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "log", "system", "error", "auth",
        "audio", "media", "hardware" // Contexts for Microphones/Headphones
    };

    // --- 2. Strong Table Context ---
    private static readonly HashSet<string> ContactTableContexts = new()
    {
        "user", "customer", "contact", "person", "employee", "staff",
        "member", "account", "client", "partner", "lead", "candidate",
        "vendor", "supplier", "store", "location", "branch", "office",
        "address", "directory"
    };

    // --- 3. Strong Column Matches ---
    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "phonenumber",
        "phone",
        "telephone",
        "telephonenumber",
        "mobile",
        "mobilephone",
        "cellphone",
        "homephone",
        "workphone",
        "businessphone",
        "fax",
        "faxnumber",
        "tel"
    };

    // --- 4. Exclusion Words ---
    private static readonly HashSet<string> ExclusionWords = new()
    {
        // Audio Equipment (The "Phone" Trap)
        "micro", "head", "gramo", "saxo", "xylo", "mega", // Microphone, Headphone
        // Technical / Metadata
        "id", "key", "type", "desc", "usage", "provider", "carrier",
        // "Number" is generic. We match "PhoneNumber" via tokens, but avoid just "Number"
        "serial", "part", "track", "invoice"
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        // Note: While some DBs use BigInt for phones, 99% use String for formatting.
        // We stick to String to avoid matching IDs or Quantities.
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        // 1. Length Check
        // A phone number is rarely < 7 chars. 
        // If it's varchar(5), it might be an internal extension or code.
        var maxLength = DataTypeHelper.GetMaxLength(column);
        if (maxLength is > 0 and < 7) return false;

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
        // Critical for avoiding "Headphone", "Microphone", "PhoneID"
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 5. Calculate Context
        var hasContactContext = ContactTableContexts.Overlaps(tableWords) || 
                                 ContactTableContexts.Overlaps(schemaWords);

        // 6. Strong Normalized Match
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Contains(normalizedCol))
        {
            return true;
        }

        // 7. Token Analysis

        // Case A: "Phone"
        if (colWords.Contains("phone")) return true;

        // Case B: "Mobile"
        if (colWords.Contains("mobile")) return true;

        // Case C: "Fax"
        if (colWords.Contains("fax")) return true;

        // Case D: "Tel"
        if (colWords.Contains("tel")) return true;

        // Case E: "Cell" (Context Required)
        // Now that "cell" is removed from StrongColumnNames, this logic finally runs.
        if (colWords.Contains("cell"))
        {
            // "CellPhone" -> Passed via StrongColumnNames or contains "phone" here.
            // "Customers.Cell" -> Passed via hasContactContext.
            // "Products.Cell" -> Fails both checks -> Returns False.
            if (colWords.Contains("phone") || hasContactContext)
            {
                return true;
            }
        }

        return false;
    }
}