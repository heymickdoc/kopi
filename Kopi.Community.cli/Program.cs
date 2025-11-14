using Bogus;
using Kopi.Community.cli.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services;
using Kopi.Core.Services.Common;
using Kopi.Core.Services.Common.DataGeneration.Generators;
using Kopi.Core.Services.Docker;
using Kopi.Core.Services.Matching.Matchers;
using Kopi.Core.Services.SQLServer.Source;
using Kopi.Core.Services.SQLServer.Target;
using Kopi.Core.Utilities;

namespace Kopi.Community.cli;

internal class Program
{
    /// <summary>
    /// Holds the validated command-line arguments
    /// </summary>
    private static Dictionary<string, string> _arguments = [];

    static async Task Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "-h" || args[0] == "--help") ShowUsage();

        try
        {
            _arguments = ArgumentValidationService.ValidateArguments(args);
            if (_arguments.Count == 0) ShowUsage();
        }
        catch (ArgumentException ex)
        {
            Msg.Write(MessageType.Error, ex.Message);
            Console.WriteLine("");
            ShowUsage();
        }

        // At this point, we've validated the command line args and can proceed.
        // If the command is "up", we proceed with the main logic.
        if (_arguments["command"] == "up")
        {
            await RunKopiUp();
        }

        // If the command is "down", we proceed with the teardown logic.
        if (_arguments["command"] == "down")
        {
            await RunKopiDown();
        }

        // If the command is "version", we display version information.
        if (_arguments["command"] == "version")
        {
            RunKopiVersion();
        }
        
        // If the command is "status", we check the status of the setup.
        if (_arguments["command"] == "status")
        {
            await RunKopiStatus();
        }
    }

    private static async Task RunKopiUp()
    {
        var stopWatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Let's load the arguments (if any)
        _arguments.TryGetValue("config", out var configPath);
        _arguments.TryGetValue("password", out var passwordFromCli);
        
        // --- 0. LOAD CONFIGURATION FIRST ---
        var config = await ConfigLoader(configPath);
        if (config is null)
        {
            Msg.Write(MessageType.Error, "Failed to load configuration. Exiting.");
            Environment.Exit(1);
            return; // Necessary for nullable analysis
        }

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
            Environment.Exit(1);
            return; // Necessary for nullable analysis
        }

        // --- 1. DETERMINE THE SOURCE DATABASE TYPE ---
        //One of the reasons is that we may Dependency Inject different services based on source DB type.
        //The Community Edition will only allow source and target to be the same type anyway (SQL Server for now).
        var dbType = DatabaseHelper.GetDatabaseType(config.SourceConnectionString);
        if (dbType == DatabaseType.Unknown)
        {
            //TODO: Show a menu interactively to select the source database type? Only if we can't determine it automatically.
            Msg.Write(MessageType.Error, "Could not determine source database type from connection string. Exiting.");
            Environment.Exit(1);
            return; // Necessary for nullable analysis
        }

        // --- 2. LOAD SOURCE DB MODEL (Cache or Fresh) FIRST ---
        var sourceDbModel = await SourceModelLoader(config, dbType); // Pass loaded config
        if (sourceDbModel == null)
        {
            Msg.Write(MessageType.Error, "Failed to load source database model. Exiting.");
            Environment.Exit(1);
            return; // Necessary for nullable analysis
        }

        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // --- REGISTER THE LOADED SINGLETON INSTANCES ---
                // The DI container will now always provide these exact objects
                services.AddSingleton(config);
                services.AddSingleton(sourceDbModel);

                // --- Register Core Services (mostly singletons) ---
                services.AddSingleton<TargetDbOrchestratorService>(); // Target DB setup
                services.AddSingleton<DataOrchestratorService>(); // Data generation logic
                services.AddSingleton<DataGeneratorService>(); // The "Switchboard"
                services.AddSingleton<Faker>(); // Bogus instance

                // --- Register Data Generators (Community) ---
                services.AddSingleton<IDataGenerator, CommunityAddressCityGenerator>();
                services.AddSingleton<IDataGenerator, CommunityAddressCountyGenerator>();
                services.AddSingleton<IDataGenerator, CommunityAddressFullGenerator>();
                services.AddSingleton<IDataGenerator, CommunityAddressLine1Generator>();
                services.AddSingleton<IDataGenerator, CommunityAddressLine2Generator>();
                services.AddSingleton<IDataGenerator, CommunityAddressPostalcodeGenerator>();
                services.AddSingleton<IDataGenerator, CommunityAddressStateGenerator>();
                services.AddSingleton<IDataGenerator, CommunityAddressZipcodeGenerator>();
                services.AddSingleton<IDataGenerator, CommunityCountryGenerator>();
                services.AddSingleton<IDataGenerator, CommunityCountryISO2Generator>();
                services.AddSingleton<IDataGenerator, CommunityCountryISO3Generator>();
                services.AddSingleton<IDataGenerator, CommunityCreditCardDateGenerator>();
                services.AddSingleton<IDataGenerator, CommunityCreditCardNumberGenerator>();
                services.AddSingleton<IDataGenerator, CommunityDefaultBinaryGenerator>();
                services.AddSingleton<IDataGenerator, CommunityDefaultBooleanGenerator>();
                services.AddSingleton<IDataGenerator, CommunityDefaultDateGenerator>();
                services.AddSingleton<IDataGenerator, CommunityDefaultDecimalGenerator>();
                services.AddSingleton<IDataGenerator, CommunityDefaultGeographyGenerator>();
                services.AddSingleton<IDataGenerator, CommunityDefaultGeometryGenerator>();
                services.AddSingleton<IDataGenerator, CommunityDefaultGuidGenerator>();
                services.AddSingleton<IDataGenerator, CommunityDefaultHierarchyIdGenerator>();
                services.AddSingleton<IDataGenerator, CommunityDefaultIntegerGenerator>();
                services.AddSingleton<IDataGenerator, CommunityDefaultJsonGenerator>();
                services.AddSingleton<IDataGenerator, CommunityDefaultMoneyGenerator>();
                services.AddSingleton<IDataGenerator, CommunityDefaultStringGenerator>();
                services.AddSingleton<IDataGenerator, CommunityDefaultTimeGenerator>();
                services.AddSingleton<IDataGenerator, CommunityDefaultXMLGenerator>();
                services.AddSingleton<IDataGenerator, CommunityEmailAddressGenerator>();
                services.AddSingleton<IDataGenerator, CommunityPersonFirstnameGenerator>();
                services.AddSingleton<IDataGenerator, CommunityPersonFullnameGenerator>();
                services.AddSingleton<IDataGenerator, CommunityPersonLastnameGenerator>();
                services.AddSingleton<IDataGenerator, CommunityPersonMiddlenameGenerator>();
                services.AddSingleton<IDataGenerator, CommunityPersonSuffixGenerator>();
                services.AddSingleton<IDataGenerator, CommunityPersonTitleGenerator>();
                services.AddSingleton<IDataGenerator, CommunityPhoneNumberGenerator>();
                services.AddSingleton<IDataGenerator, CommunityProductNameGenerator>();

                // --- Register Column Matchers (Community) ---
                services.AddSingleton<IColumnMatcher, CommunityAddressCityMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityAddressCountyMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityAddressFullMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityAddressLine1Matcher>();
                services.AddSingleton<IColumnMatcher, CommunityAddressLine2Matcher>();
                services.AddSingleton<IColumnMatcher, CommunityAddressPostalcodeMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityAddressStateMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityAddressZipcodeMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityCountryISO2Matcher>();
                services.AddSingleton<IColumnMatcher, CommunityCountryISO3Matcher>();
                services.AddSingleton<IColumnMatcher, CommunityCountryMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityCreditCardDateMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityCreditCardNumberMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityDefaultBinaryMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityDefaultBooleanMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityDefaultDateMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityDefaultDecimalMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityDefaultGeographyMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityDefaultGeometryMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityDefaultGuidMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityDefaultHierarchyIdMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityDefaultIntegerMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityDefaultJsonMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityDefaultMoneyMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityDefaultStringMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityDefaultTimeMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityDefaultXMLMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityEmailAddressMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityPersonFirstnameMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityPersonFullnameMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityPersonLastnameMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityPersonMiddlenameMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityPersonSuffixMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityPersonTitleMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityPhoneNumberMatcher>();
                services.AddSingleton<IColumnMatcher, CommunityProductNameMatcher>();
            });

        var app = builder.Build();

        var mainOrchestrator = app.Services.GetRequiredService<TargetDbOrchestratorService>();
        var dbConnectionString = await mainOrchestrator.PrepareTargetDb();

        if (string.IsNullOrEmpty(dbConnectionString))
        {
            Msg.Write(MessageType.Error, "Failed to prepare the target database. Exiting.");
            Environment.Exit(1);
        }

        Msg.Write(MessageType.Info, "===============================================");
        Msg.Write(MessageType.Info, "Target database preparation complete.");
        Msg.Write(MessageType.Info, "Note: You can recreate the target database at any time by running Kopi again.");
        Msg.Write(MessageType.Info, "===============================================");
        Console.WriteLine("");
        Msg.Write(MessageType.Success, "You can connect to your new database using the following connection string:");
        Msg.Write(MessageType.Success, "===============================================");
        Msg.Write(MessageType.Warning, $"{dbConnectionString}");
        Msg.Write(MessageType.Success, "===============================================");
        Console.WriteLine("");

        stopWatch.Stop();
        Msg.Write(MessageType.Info, $"Total execution time: {stopWatch.Elapsed.TotalSeconds} seconds.");
        Environment.Exit(0);
    }

    private static async Task RunKopiDown()
    {
        var stopWatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Let's load the arguments (if any)
        _arguments.TryGetValue("config", out var configPath);
        var tearDownAll = _arguments.ContainsKey("all");

        if (configPath is not null) await KopiDown.ExecuteTearDown(configPath);

        if (tearDownAll)
        {
            //This is a teardown of all Kopi-managed Docker containers.
            var allContainers = await DockerService.ListAllKopiContainers();
            if (allContainers.Count == 0)
            {
                Msg.Write(MessageType.Info,
                    "No Kopi-managed Docker containers found to tear down. Exiting.");
            }
            else
            {
                var isErrors = await KopiDown.ExecuteTearDownAll(allContainers);

                if (!isErrors)
                {
					Console.WriteLine("");
                    Msg.Write(MessageType.Success, "All Kopi-managed Docker containers torn down successfully.");
                }
                else
                {
					Console.WriteLine("");
					Msg.Write(MessageType.Error, "Some errors occurred while tearing down Kopi-managed Docker containers.");
				}
			}
        }
        else
        {
			var config = await ConfigLoader(configPath);
			if (config is null)
			{
				Msg.Write(MessageType.Error, "Failed to load configuration. Exiting.");
				Environment.Exit(1);
				return; // Necessary for nullable analysis
			}

			var containerName = DockerHelper.GetContainerName(config.ConfigFileFullPath);

			if (!string.IsNullOrEmpty(containerName))
			{
				await KopiDown.ExecuteTearDown(containerName);
			}
			else
			{
				Msg.Write(MessageType.Error, "Could not determine Docker container name from configuration. Exiting.");
				Environment.Exit(1);
				return; // Necessary for nullable analysis
			}
		}

        stopWatch.Stop();
        Msg.Write(MessageType.Info, $"Total execution time: {stopWatch.Elapsed.TotalSeconds} seconds.");
        Environment.Exit(0);
    }

    /// <summary>
    ///  Displays the Kopi version information and exits.
    /// </summary>
    private static void RunKopiVersion()
    {
        var version = typeof(Program).Assembly.GetName().Version;
        Msg.Write(MessageType.Info, $"Kopi Community Edition - Version {version}");
        Environment.Exit(0);
    }

    /// <summary>
    ///  Checks and displays the status of the Kopi setup (source and target databases).
    /// </summary>
    private static async Task RunKopiStatus()
    {
		var allContainers = await DockerService.GetAllContainersStatus();

        Msg.Write(MessageType.Info, "Kopi Docker Containers Status:");
        foreach (var container in allContainers) {
            if (container.State == "running") {
                Msg.Write(MessageType.Success, $"Container: {container.Name}, Image: {container.Image}, Ports: {container.Ports}, State: {container.State}");
            } else {
                Msg.Write(MessageType.Warning, $"Container: {container.Name}, Image: {container.Image}, Ports: {container.Ports}, State: {container.State}");
            }
		}

		Environment.Exit(0);
	}

    /// <summary>
    ///  Loads the Kopi configuration file based on command-line arguments.
    /// </summary>
    /// <param name="configPath">The user-supplied config path</param>
    /// <returns></returns>
    private static async Task<KopiConfig?> ConfigLoader(string? configPath)
    {
        var config = new KopiConfig();

        try
        {
            if (!string.IsNullOrEmpty(configPath))
            {
                config = await KopiConfigService.LoadFromFile(configPath);
            }
            else
            {
                //No config file specified, so we'll look for a default one in the current directory.
                config = await KopiConfigService.LoadFromFile();
            }
        }
        catch (FileNotFoundException ex)
        {
            Msg.Write(MessageType.Error, "Missing or invalid configuration file: " + ex.Message);
            Environment.Exit(1);
        }

        return config;
    }

    // --- Helper: Load SourceDbModel (New, Static) ---
    // This takes the logic OUT of DatabaseOrchestratorService.Begin
    private static async Task<SourceDbModel?>
        SourceModelLoader(KopiConfig config, DatabaseType dbType) // Requires KopiConfig
    {
        SourceDbModel? sourceDbData = null;
        var stringToHash = config.SourceConnectionString + string.Join(",", config.Tables);
        var hashedString = CryptoHelper.ComputeHash(stringToHash, true);

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

    private static void ShowUsage()
    {
        Console.WriteLine("Kopi Community Edition");
        Console.WriteLine("");
        Console.WriteLine("Usage: kopi <command> [options]");
        Console.WriteLine("");
        Console.WriteLine("Commands:");
        Console.WriteLine("  up                     Run Kopi and create a new target database.");
        Console.WriteLine("    Options:");
        Console.WriteLine("      -c, --config <path>    Optional. Path to the configuration file.");
        Console.WriteLine("      -p, --password <pass>  Optional. Specify fixed password for the database.");
        Console.WriteLine("");
        Console.WriteLine("  down                   Teardown the target database created by Kopi.");
        Console.WriteLine("    Options:");
        Console.WriteLine("      -c, --config <path>    Optional. Target a specific replica by its config file.");
        Console.WriteLine("      -all                   Optional. Tear down all Kopi-managed replicas.");
        Console.WriteLine("");
        Console.WriteLine("  status, -s, --status   Check the status of the all Kopi Docker instances.");
        Console.WriteLine("  version, -v, --version Show version information and exit.");
        Console.WriteLine("  help, -h, --help       Show this help message and exit.");
        Console.WriteLine("");
        Console.WriteLine("Examples:");
        Console.WriteLine("  kopi up -c \"C:\\path\\to\\kopi.json\" -p MySuperSecureDBPassword");
        Console.WriteLine("  kopi up");
        Console.WriteLine("  kopi down -all");
        Console.WriteLine("  kopi help");
        Console.WriteLine("  kopi -v");
        Environment.Exit(0);
    }
}