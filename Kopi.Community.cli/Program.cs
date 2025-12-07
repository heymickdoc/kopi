using Bogus;
using Kopi.Community.cli.Services;
using Kopi.Core.Extensions;
using Kopi.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services;
using Kopi.Core.Services.Common;
using Kopi.Core.Services.Common.DataGeneration.Generators;
using Kopi.Core.Services.Docker;
using Kopi.Core.Services.Matching.Matchers;
using Kopi.Core.Services.PostgreSQL.Target;
using Kopi.Core.Services.SQLServer.PostProcessing;
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
        
        var environment = await KopiUpService.PrepareEnvironmentAsync(configPath, passwordFromCli);
        if (environment == null) Environment.Exit(1);
        
        var (config, sourceDbModel) = environment.Value;

        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(config);
                services.AddSingleton(sourceDbModel);
                services.AddKopiCore();
                
                if (config.DatabaseType == DatabaseType.PostgreSQL)
                {
                    services.AddSingleton<ITargetDbOrchestratorService, PostgresTargetDbOrchestratorService>();
                    services.AddSingleton<IDataInsertionService, PostgresDataInsertionService>();
                }
                else
                {
                    // Default to SQL Server
                    services.AddSingleton<ITargetDbOrchestratorService, SqlServerTargetDbOrchestratorService>();
                    services.AddSingleton<IDataInsertionService, SqlServerDataInsertionService>();
                }
            });

        var app = builder.Build();

        var mainOrchestrator = app.Services.GetRequiredService<ITargetDbOrchestratorService>();
        var dbConnectionString = await mainOrchestrator.PrepareTargetDb();

        if (string.IsNullOrEmpty(dbConnectionString))
        {
            Msg.Write(MessageType.Error, "Connection string for target database is null or empty. Exiting.");
            Environment.Exit(1);
        }
        
        // --- STEP 2: GENERATE DATA (In Memory) ---
        Msg.Write(MessageType.Info, "Generating synthetic data...");
        var dataOrchestrator = app.Services.GetRequiredService<DataOrchestratorService>();
        var generatedData = await dataOrchestrator.OrchestrateDataGeneration();
        
        // --- STEP 3: INSERT DATA (Bulk Write) ---
        Msg.Write(MessageType.Info, "Writing data to target database...");
    
        // We use the INTERFACE here too
        var dataWriter = app.Services.GetRequiredService<IDataInsertionService>();
        await dataWriter.InsertData(config, sourceDbModel, generatedData, dbConnectionString);
    
        Msg.Write(MessageType.Success, "Data insertion complete.");
        Console.WriteLine("");
        
        // --- STEP 4: APPLY PROGRAMMABILITY (SPs, Views) ---
        // Now that data is safe, we add the complex logic objects
        await mainOrchestrator.CreateDatabaseProgrammability(dbConnectionString);

        // --- STEP 5: SYNC EF MIGRATIONS (Post-Process) ---
        try 
        {
            var efService = new SqlServerEfMigrationHistoryService(
                config.SourceConnectionString, 
                dbConnectionString, 
                sourceDbModel);

            await efService.CopyMigrationHistoryAsync();
        }
        catch (Exception ex)
        {
            Msg.Write(MessageType.Warning, $"Failed to sync EF Migration History: {ex.Message}");
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
        // Let's load the arguments (if any)
        _arguments.TryGetValue("config", out var configPath);
        var tearDownAll = _arguments.ContainsKey("all");

        if (configPath is not null)
        {
            var containerName = DockerHelper.GetContainerName(configPath);
            await DockerService.ExecuteTearDown(containerName);
            return;
        }

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
                var isErrors = await DockerService.ExecuteTearDownAll(allContainers);

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
			var config = await KopiConfigService.LoadFromFile();
            if (string.IsNullOrEmpty(config.ConfigFileFullPath))
            {
                Msg.Write(MessageType.Error, "Failed to load configuration. Exiting.");
                Environment.Exit(1);
                return; // Necessary for nullable analysis
            }

			var containerName = DockerHelper.GetContainerName(config.ConfigFileFullPath);

			if (!string.IsNullOrEmpty(containerName))
			{
				await DockerService.ExecuteTearDown(containerName);
			}
			else
			{
				Msg.Write(MessageType.Error, "Could not determine Docker container name from configuration. Exiting.");
				Environment.Exit(1);
				return; // Necessary for nullable analysis
			}
		}

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