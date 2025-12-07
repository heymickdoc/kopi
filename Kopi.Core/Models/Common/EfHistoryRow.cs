namespace Kopi.Core.Models.Common;

/// <summary>
/// Represents a row in the EF Migrations History table.
/// </summary>
public class EfHistoryRow
{
    /// <summary>
    /// The unique identifier for the migration.
    /// </summary>
    public string MigrationId { get; set; } = "";
    
    /// <summary>
    /// The product version of Entity Framework used for the migration.
    /// </summary>
    public string ProductVersion { get; set; } = "";
}