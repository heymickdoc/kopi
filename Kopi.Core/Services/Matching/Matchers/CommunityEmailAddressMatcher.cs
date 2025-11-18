using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for Email Address columns.
///  Strictly avoids content columns like "EmailBody" or "EmailSubject".
/// </summary>
public class CommunityEmailAddressMatcher : IColumnMatcher
{
    public int Priority => 20; // High priority, email is a critical PII field
    public string GeneratorTypeKey => "email_address";

    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "production", "inventory", "product", "log", "system", "error", "auth",
        "config", "setting"
    };

    // --- 2. Strong Table Context ---
    // Not strictly required for "Email", but helps resolve ambiguity if needed
    private static readonly HashSet<string> PersonTableContexts = new()
    {
        "user", "customer", "contact", "person", "employee", "staff",
        "member", "account", "login", "auth", "client", "partner"
    };

    // --- 3. Strong Column Matches ---
    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "email",
        "emailaddress",
        "emailaddr",
        "useremail",
        "contactemail",
        "primaryemail",
        "secondaryemail",
        "personalemail",
        "workemail"
    };

    // --- 4. Exclusion Words ---
    // Critical for avoiding "EmailBody", "EmailSubject", "EmailSentDate"
    private static readonly HashSet<string> ExclusionWords = new()
    {
        "body", "subject", "content", "message", "text", "html", // Content
        "date", "time", "sent", "received", // Timestamps
        "status", "flag", "valid", "verified", // Boolean/Status
        "id", "key" // FKs
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;
        
        // 2. Length Check (Optimization)
        // The shortest valid email (a@b.c) is 5 chars.
        // If the column is smaller than 5, it physically cannot be an email.
        if (DataTypeHelper.GetMaxLength(column) < 5) return false;

        // 1. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);
        
        var colWords = StringUtils.SplitIntoWords(column.ColumnName)
            .Select(s => s.ToLower())
            .ToList();

        // 2. Immediate Disqualification
        if (InvalidSchemaNames.Overlaps(schemaWords)) return false;

        // 3. Negative Checks (Safety Valve)
        // Prevents matching "EmailBody" or "EmailStatus"
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 4. Strong Normalized Match
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Contains(normalizedCol))
        {
            return true;
        }

        // 5. Token Analysis
        
        // "Email" is a very strong signal.
        // Because we already ran ExclusionWords, if we see "email" here, 
        // it is highly likely to be the address itself.
        if (colWords.Contains("email"))
        {
            // Optional: Safety check for context if the column name is weird 
            // (e.g. "SendEmail" -> might be a boolean flag? usually excluded by type check if bit)
            // But "SendEmail" as a string implies the address.
            
            return true;
        }
        
        // "E-mail" (handled by tokenization splitting on hyphen usually, but normalized handles it)
        
        return false;
    }
}