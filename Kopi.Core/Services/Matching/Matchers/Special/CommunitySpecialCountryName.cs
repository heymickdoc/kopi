using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;
using Kopi.Core.Models.Common;

namespace Kopi.Core.Services.Matching.Matchers.Special;

/// <summary>
///  Helper to identify if a table represents a Country entity.
///  Used when we find a generic "Name" column in a table like "Region.Country".
/// </summary>
public class CommunitySpecialCountryName
{
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "item", "catalog",
        "log", "logging", "audit", "error", "system", "auth", "security",
        "finance", "accounting"
    };

    private static readonly HashSet<string> TableNames = new()
    {
        "country",
        "nation"
        // REMOVED: "location" (Too ambiguous. Location.Name is usually a Building Name).
    };

    public static bool IsMatch(TableModel tableContext, int maxLength)
    {
        // 1. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);

        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);
            
        // 2. Immediate Disqualification (Standard + Raw Check)
        var schemaRaw = tableContext.SchemaName?.ToLower().Replace("_", "") ?? "";
        if (InvalidSchemaNames.Overlaps(schemaWords) || InvalidSchemaNames.Contains(schemaRaw))
        {
            return false;
        }

        // 3. Match Logic
        // - Must be in a Country/Nation table
        // - Must be > 3 chars (excludes 2/3 char ISO codes, allows "Chad", "Togo")
        return TableNames.Overlaps(tableWords) && maxLength > 3;
    }
}