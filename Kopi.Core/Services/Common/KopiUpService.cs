using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.SQLServer.Source;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common;

/// <summary>
///  Common service to prepare the environment for "Kopi Up" operations
/// </summary>
public static class KopiUpService
{
    // Returns the SourceDbModel so the caller can register it in their specific DI container
    public static async Task<(KopiConfig Config, SourceDbModel SourceDbModel)?> PrepareEnvironmentAsync(
        string? configPath, 
        string? passwordFromCli)
    {
        KopiConfig config;
        try 
        {
            config = string.IsNullOrEmpty(configPath) 
                ? await KopiConfigService.LoadFromFile() 
                : await KopiConfigService.LoadFromFile(configPath);
        }
        catch (FileNotFoundException ex)
        {
            Msg.Write(MessageType.Error, ex.Message);
            return null;
        }

        GetFinalPassword(passwordFromCli, config);
        
        // 3. Determine DB Type
        var dbType = DatabaseHelper.GetDatabaseType(config.SourceConnectionString);
        if (dbType == DatabaseType.Unknown)
        {
            Msg.Write(MessageType.Error, "Could not determine source database type.");
            return null;
        }

        // 4. Load Schema (This uses your existing static helpers which should also be in Core)
        var sourceDbModel = await SourceModelLoader(config, dbType);
        
        if (sourceDbModel == null) return null;

        return (config, sourceDbModel);
    }
    
    /// <summary>
    ///  Determines the final password to use for the database.
    /// </summary>
    /// <param name="passwordFromCli">The password provided via command line, if any.</param>
    /// <param name="config">The Kopi configuration object.</param>
    private static void GetFinalPassword(string? passwordFromCli, KopiConfig config)
    {
        var finalPassword = passwordFromCli;

        if (string.IsNullOrEmpty(finalPassword))
        {
            config.Settings ??= new Settings();
            finalPassword = config.Settings.SaPassword;
        }
        
        // By this point, 'finalPassword' is guaranteed to have a value.
        // We'll update the config object in-memory so the DI container
        // and all other services get the single, correct password.
        config.Settings.SaPassword = finalPassword;

        var isPasswordComplexEnough = DatabaseHelper.IsPasswordComplexEnough(config.Settings.SaPassword);
        if (!isPasswordComplexEnough)
        {
            Msg.Write(MessageType.Error, "The provided database password does not meet complexity requirements. " +
                                         "It must be at least 8 characters long and include uppercase letters, " +
                                         "lowercase letters, numbers, and special characters. Exiting.");
            return;
        }
    }
    
    // --- Helper: Load SourceDbModel (New, Static) ---
    // This takes the logic OUT of DatabaseOrchestratorService.Begin
    private static async Task<SourceDbModel?>
        SourceModelLoader(KopiConfig config, DatabaseType dbType) // Requires KopiConfig
    {
        SourceDbModel? sourceDbData = null;
        //var stringToHash = config.SourceConnectionString + string.Join(",", config.Tables);
        var configPathString = string.IsNullOrEmpty(config.ConfigFileFullPath)
            ? "default_path"
            : config.ConfigFileFullPath;
        var hashedString = CryptoHelper.ComputeHash(configPathString, true);

        if (string.IsNullOrEmpty(hashedString))
        {
            Msg.Write(MessageType.Error, "Cannot generate cache key hash.");
            return null; // Return null on error
        }

        var isCached = CacheService.IsCached(hashedString);

        if (isCached)
        {
            Msg.Write(MessageType.Info, "Loading database schema from cache...");
            try
            {
                sourceDbData = await CacheService.LoadFromCache(hashedString);
                // Re-validate connection string even if cached data exists
                var isValidConnectionString = await ValidateConnectionString(config, dbType); // Use helper below
                if (!isValidConnectionString)
                {
                    // Don't exit, just invalidate cache and proceed to read source
                    Msg.Write(MessageType.Warning,
                        "Source DB connection string seems invalid. Ignoring cache and reading from source.");
                    sourceDbData = null; // Invalidate cache data
                    isCached = false; // Force re-cache later
                }
            }
            // Only treat FileNotFound as a cache miss, others might be real errors
            catch (FileNotFoundException)
            {
                Msg.Write(MessageType.Warning, "Cache file not found despite check. Reading source db...");
                isCached = false;
            }
            catch (Exception ex)
            {
                Msg.Write(MessageType.Error, $"Error loading from cache: {ex.Message}. Reading source db...");
                sourceDbData = null;
                isCached = false;
            }
        }

        // If not loaded from cache (or cache load failed/invalidated), read from source
        if (sourceDbData == null)
        {
            Msg.Write(MessageType.Info, "Reading source database schema...");
            sourceDbData = await ReadAndAnalyzeSource(config, dbType); // Use helper below
            Console.WriteLine("");
        }

        if (sourceDbData == null) return null; // Return null on error

        // Write to cache if it wasn't cached before OR if cache was invalidated
        if (!isCached)
        {
            Msg.Write(MessageType.Info, "Writing schema to cache...");
            await CacheService.WriteToCache(hashedString, sourceDbData);
            Msg.Write(MessageType.Success, "Schema cached successfully.");
            Console.WriteLine("");
        }

        return sourceDbData;
    }


    // --- Helper: Read Source (New, Static - depends on SourceDbOrchestratorService.Begin being static) ---
    private static async Task<SourceDbModel?> ReadAndAnalyzeSource(KopiConfig config, DatabaseType dbType)
    {
        try
        {
            var isValidConnectionString = await ValidateConnectionString(config, dbType);
            if (!isValidConnectionString)
            {
                // ValidateConnectionString already printed error
                return null; // Return null on error
            }

            // Assuming SourceDbOrchestratorService.Begin is static and handles its own errors/messages
            return dbType switch
            {
                DatabaseType.SqlServer => await SourceDbOrchestratorService.Begin(config),
                DatabaseType.PostgreSQL => throw new NotImplementedException(
                    "PostgreSQL support is not implemented yet."),
                _ => throw new NotSupportedException("Database type not supported"),
            };
        }
        catch (NotSupportedException ex) // Catch specific exceptions from the switch
        {
            Msg.Write(MessageType.Error, ex.Message);
            return null;
        }
        catch (NotImplementedException ex)
        {
            Msg.Write(MessageType.Error, ex.Message);
            return null;
        }
        catch (Exception ex) // Catch potential errors during source DB read
        {
            Msg.Write(MessageType.Error, $"Unexpected error reading source database: {ex.Message}");
            return null; // Return null on error
        }
    }

    // --- Helper: Validate Connection (New, Static - depends on SourceDbConnectionStringService being static) ---
    private static async Task<bool> ValidateConnectionString(KopiConfig config, DatabaseType dbType)
    {
        // Assuming SourceDbConnectionStringService methods are static and handle messages
        try
        {
            return dbType switch
            {
                DatabaseType.SqlServer => await SourceDbConnectionStringService.ValidateSqlServerConnectionString(
                    config),
                DatabaseType.PostgreSQL => throw new NotImplementedException("PostgreSQL validation not implemented."),
                _ => throw new NotSupportedException("Database type not supported for validation"),
            };
        }
        catch (NotSupportedException ex) // Catch specific exceptions
        {
            Msg.Write(MessageType.Error, ex.Message);
            return false;
        }
        catch (NotImplementedException ex)
        {
            Msg.Write(MessageType.Error, ex.Message);
            return false;
        }
        catch (Exception ex) // Catch general validation failures
        {
            Msg.Write(MessageType.Error, $"Connection string validation failed: {ex.Message}");
            return false;
        }
    }
}