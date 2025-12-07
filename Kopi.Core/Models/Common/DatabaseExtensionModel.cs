namespace Kopi.Core.Models.Common;

/// <summary>
/// Handles database extension information, like citext, postgis, etc.
/// </summary>
public class DatabaseExtensionModel
{
    public string Name { get; set; }
    public string Version { get; set; }
    public string SchemaName { get; set; }
}