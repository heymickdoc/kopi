using Kopi.Core.Models;
using Kopi.Core.Models.Common;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common;

public static class KopiConfigService
{
    /// <summary>
    ///  Indicates whether to append TrustServerCertificate=True to SQL Server connection strings
    /// </summary>
    private static bool _appendTrustServerCertificate;
    
    public static async Task<KopiConfig> LoadFromFile(string configFilePath = "")
    {
        if (string.IsNullOrEmpty(configFilePath))
        {
            configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "kopi.json");
        }
        
        try
        {
            //Msg.Write(MessageType.Info, $"Loading configuration file from: {configFilePath}");
            var configFile = await File.ReadAllTextAsync(configFilePath);
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<KopiConfig>(configFile);
            
            
            if (config is not null)
            {
                config.ConfigFileFullPath = configFilePath;
                //Msg.Write(MessageType.Success, "Successfully loaded configuration file");
                //Console.WriteLine("");
            
                return config;
            }
        }
        catch (FileNotFoundException)
        {
            Msg.Write(MessageType.Error, $"No configuration file specified and the default configuration file was not found at path: {configFilePath}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Msg.Write(MessageType.Error, $"An error occurred while loading the configuration file: {ex.Message}");
            Environment.Exit(1);
        }
        
        return null;
    }
    
    
    public static bool ShouldAppendTrustServerCertificate()
    {
        return _appendTrustServerCertificate;
    }
    
    /// <summary>
    ///  Sets whether to append TrustServerCertificate=True to SQL Server connection strings
    /// </summary>
    /// <param name="value">True or False</param>
    /// <returns></returns>
    public static void SetAppendTrustServerCertificate(bool value)
    {
        _appendTrustServerCertificate = value;
    }
}