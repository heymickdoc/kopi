using Kopi.Core.Models.Common;

namespace Kopi.Core.Utilities;

public static class DockerHelper
{
    /// <summary>
    /// Generates a unique Docker container name based on the full path of the Kopi config file.
    /// </summary>
    /// <param name="config">The config file</param>
    /// <returns></returns>
    public static string GetContainerName(string configFileFullPath)
    {
        var stringToHash = configFileFullPath;
        //The container name will be kopi_<hash_of_kopi_config_file_path>
        var hashedString = CryptoHelper.ComputeHash(stringToHash, true);

        //Msg.Write(MessageType.Info, $"Generated Docker container name: kopi_{hashedString.ToLower()}");
    
        return $"kopi_{hashedString.ToLower()}";
    }
}