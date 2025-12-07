using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;
using Kopi.Core.Models.Common;

namespace Kopi.Core.Services.Matching.Matchers.Special;

/// <summary>
///  Helper to identify if a table represents a Status/Lifecycle entity.
///  Used when we find a generic "Name" column in a table like "OrderStatus".
/// </summary>
public class CommunitySpecialStatusName
{
    // --- 1. Safe "Stop Words" ---
    // Avoid system internals where "Status" might be a technical code/flag
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "log", "logging", "audit", "history", "backup", "sys", "system",
        "config", "setting", "error"
    };

    // --- 2. Strong Table Keywords ---
    private static readonly HashSet<string> TableNames = new()
    {
        "status",
        "stage",
        "phase",     // e.g. "ProjectPhase"
        "step",      // e.g. "WorkflowStep"
        "condition", // e.g. "ItemCondition"
        "rank",      // e.g. "EmployeeRank"
        "level",     // e.g. "AccessLevel"
        "tier"       // e.g. "PricingTier"
    };

    public static bool IsMatch(TableModel tableContext)
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

        // 3. Match Check
        // Matches "OrderStatus", "WorkflowStage", "UserRank"
        return TableNames.Overlaps(tableWords);
    }
}