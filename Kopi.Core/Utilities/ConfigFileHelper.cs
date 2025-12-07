namespace Kopi.Core.Utilities;

public class ConfigFileHelper
{
    /// <summary>
    /// Validates if the provided path is a valid configuration file path
    /// </summary>
    /// <param name="path">The provided path</param>
    /// <returns>True if valid</returns>
    public static bool IsValidConfigFilePath(string path)
    {
        return File.Exists(path) && Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase);
    }
}