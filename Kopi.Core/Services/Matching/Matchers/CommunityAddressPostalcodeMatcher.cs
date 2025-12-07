using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;
using Kopi.Core.Models.Common;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for International "Postal Code" columns.
///  Explicitly avoids "Zip" to let the ZipCode matcher handle those.
/// </summary>
public class CommunityAddressPostalcodeMatcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "address_postalcode";

    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "log", "system", "error", "auth"
    };

    // --- 2. Strong Column Matches ---
    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "postalcode",
        "postcode",
        "addresspostalcode",
        "billingpostalcode",
        "shippingpostalcode"
    };
    
    // --- 3. Exclusions ---
    // If it says "Zip", let the ZipMatcher handle it.
    private static readonly HashSet<string> ExclusionWords = new()
    {
        "zip"
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        // 1. Tokenize
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);
        
        var colWords = StringUtils.SplitIntoWords(column.ColumnName)
            .Select(s => s.ToLower())
            .ToList();

        // 2. Immediate Disqualification
        if (InvalidSchemaNames.Overlaps(schemaWords)) return false;
        
        // 3. Negative Check
        // If it contains "zip", ignore it so CommunityAddressZipcodeMatcher can take it.
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 4. Strong Normalized Match
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Any(s => normalizedCol.Contains(s)))
        {
            return true;
        }

        // 5. Token Analysis
        
        // Case A: "Postal" (e.g. "Postal_Code", "Billing_Postal")
        if (colWords.Contains("postal"))
        {
            return true;
        }

        // Case B: "Post" + "Code" (e.g. "Post_Code")
        // We require both to avoid matching "PostTitle" or "BlogPost"
        if (colWords.Contains("post") && colWords.Contains("code"))
        {
            return true;
        }

        return false;
    }
}