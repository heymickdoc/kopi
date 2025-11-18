using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Common;
using Kopi.Core.Services.Docker;
using Kopi.Core.Services.SQLServer.Target;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.SQLServer.Target;

public class TargetDbOrchestratorService(
    KopiConfig config,
    SourceDbModel sourceDbData,
    DataOrchestratorService dataOrchestratorService)
{
    private readonly KopiConfig _config = config;
    private readonly SourceDbModel _sourceDbData = sourceDbData;

    // We'll keep track of failed functions to retry them later
    private readonly List<ProgrammabilityModel> _failedFunctions = new();
    
    /// <summary>
    /// Begins the process of preparing the target database schema, indexes, relationships, etc.
    /// </summary>
    /// <returns></returns>
    public async Task<string> PrepareTargetDb()
    {
        var (sqlPort, sqlPassword) = await HandleDockerContainerLifecycle(config, sourceDbData);

        // 2. Generate Scripts
        Msg.Write(MessageType.Info, "Generating structural scripts...");
        
        var dbCreationScript = ScriptCreationService.GenerateDatabaseCreationScript(config, sourceDbData);
        var dbSchemasCreationScript = ScriptCreationService.GenerateDbSchemasCreationScript(config, sourceDbData);
        var udtCreationScript = ScriptCreationService.GenerateUserDefinedDataTypeCreationScript(config, sourceDbData);
        var tableCreationScript = ScriptCreationService.GenerateTableCreationScript(config, sourceDbData);
        var primaryKeyCreationScript = ScriptCreationService.GeneratePrimaryKeyCreationScript(config, sourceDbData);
        var relationshipCreationScript = ScriptCreationService.GenerateRelationshipCreationScript(config, sourceDbData);
        var indexCreationScript = ScriptCreationService.GenerateIndexCreationScript(config, sourceDbData);
        
        var masterDbConnectionString =
            ScriptExecutionService.GenerateTargetDbConnectionString(config, sqlPort, sqlPassword, isDbCreation: true);
        var targetDbConnectionString =
            ScriptExecutionService.GenerateTargetDbConnectionString(config, sqlPort, sqlPassword, isDbCreation: false);
        
        // --- EXECUTE IN STRICT DEPENDENCY ORDER ---
        Msg.Write(MessageType.Info, "Creating target database structure...");
        
        // 1. Base Database
        await CreateTargetDatabaseCore(dbCreationScript, masterDbConnectionString);
        
        // 2. Schemas (Must be first, as other objects belong to them)
        await CreateTargetDatabaseSchemas(dbSchemasCreationScript, targetDbConnectionString);
        
        // 3. User Defined Types (UDTs)
        // CRITICAL: Tables depend on these (if a column uses a custom type).
        await CreateTargetDatabaseUDTs(udtCreationScript, targetDbConnectionString);
        
        // 3. FUNCTIONS PASS 1 (The "Pure Function" Pass)
        // We attempt to create ALL functions. 
        // Pure logic functions (needed for computed cols) will succeed.
        // Data-access functions (that select from tables) will fail. We catch them.
        await CreateTargetDatabaseFunctions(sourceDbData.Functions, targetDbConnectionString, isRetryPass: false);

        // 4. Tables
        // Now that "Pure" functions exist, tables with Computed Columns should work.
        await CreateTargetDatabaseTables(tableCreationScript, targetDbConnectionString);
        
        // 5. FUNCTIONS PASS 2 (The "Data-Access" Pass)
        // We retry only the functions that failed in Pass 1.
        // Now that tables exist, these should succeed.
        if (_failedFunctions.Any())
        {
            await CreateTargetDatabaseFunctions(_failedFunctions, targetDbConnectionString, isRetryPass: true);
        }
        
        // 6. Keys & Indexes
        await CreateTargetDatabasePKs(primaryKeyCreationScript, targetDbConnectionString);
        await CreateTargetDatabaseIndexes(indexCreationScript, targetDbConnectionString);
        
        // 7. Constraints & Relationships
        await CreateTargetDatabaseConstraints(sourceDbData, targetDbConnectionString);
        await CreateTargetDatabaseRelationships(relationshipCreationScript, targetDbConnectionString);

        Msg.Write(MessageType.Success, "Base structure created successfully.");
        Console.WriteLine("");

        return targetDbConnectionString;
    }
    
    public async Task CreateDatabaseProgrammability(string targetDbConnectionString)
    {
        Msg.Write(MessageType.Info, "Applying database programmability (SPs, Views)...");

        await CreateTargetDatabaseStoredProcedures(sourceDbData, targetDbConnectionString);
        await CreateTargetDatabaseViews(sourceDbData, targetDbConnectionString);
        
        // TODO: Add Triggers here!

        Msg.Write(MessageType.Success, "Programmability objects created successfully.");
        Console.WriteLine("");
    }

    /// <summary>
    ///  Handles the Docker container lifecycle: stops and deletes any existing container, creates a new one if needed, and ensures it's running.
    /// </summary>
    /// <param name="config">The <see cref="KopiConfig"/> file</param>
    /// <param name="sourceDbData">The source data</param>
    /// <returns>The SQL Port and Password</returns>
    private static async Task<(int sqlPort, string sqlPassword)> HandleDockerContainerLifecycle(KopiConfig config,
        SourceDbModel sourceDbData)
    {
        Msg.Write(MessageType.Info, "Preparing Docker container for target database...");

        var containerName = DockerHelper.GetContainerName(config.ConfigFileFullPath);

        var stoppedAndDeleted = await DockerService.StopAndDeleteRunningContainer(containerName);

        var sqlPort = await DockerService.GetAvailableSqlServerPort();
        var sqlPassword = config.Settings.SaPassword;

        if (stoppedAndDeleted)
        {
            await DockerService.CreateContainer(containerName, sourceDbData.SqlServerVersion, sqlPort, sqlPassword);
        }

        var isContainerRunning = false;
        var tries = 0;
        //Wait for the container to be running
        while (!isContainerRunning)
        {
            isContainerRunning = await DockerService.VerifyContainerIsRunning(containerName);
            if (isContainerRunning) continue;

            await Task.Delay(2000); //Wait 2 seconds before checking again
            tries++;
            if (tries < 5) continue; //Wait a maximum of 30 seconds

            Msg.Write(MessageType.Error, "Failed to start the Docker container in a timely manner. Exiting.");
            Environment.Exit(1);
        }

        Msg.Write(MessageType.Success, $"Docker container {containerName} is running.");

        Console.WriteLine("");
        return (sqlPort, sqlPassword);
    }

    /// <summary>
    /// Creates the target database.
    /// </summary>
    /// <param name="dbCreationScript">The script</param>
    /// <param name="masterDbConnectionString">The connection string for the Master database</param>
    private static async Task CreateTargetDatabaseCore(string dbCreationScript, string masterDbConnectionString)
    {
        Msg.Write(MessageType.Info, "Preparing target database. This may take a few moments...");
        var isSuccess = await ScriptExecutionService.ExecuteSqlScript(dbCreationScript, masterDbConnectionString);
        if (!isSuccess)
        {
            Msg.Write(MessageType.Error, "Failed to create the target database.");
            Environment.Exit(1);
        }

        Msg.Write(MessageType.Success, "Target database created successfully.");
        Console.WriteLine("");
    }

    /// <summary>
    /// Creates the target database schema, e.g. dbo, sales, hr, etc. Not the actual tables within the schemas.
    /// </summary>
    /// <param name="schemaCreationScript">The script</param>
    /// <param name="dbConnectionString">The target db connection string</param>
    private static async Task CreateTargetDatabaseSchemas(string schemaCreationScript, string dbConnectionString)
    {
        Msg.Write(MessageType.Info, "Creating target database schema...");
        var isSuccess = await ScriptExecutionService.ExecuteSqlScript(schemaCreationScript, dbConnectionString);
        if (!isSuccess)
        {
            Msg.Write(MessageType.Error, "Failed to create the target database schema.");
            Environment.Exit(1);
        }

        Msg.Write(MessageType.Success, "Target database schema created successfully.");
        Console.WriteLine("");
    }

    /// <summary>
    /// Creates functions with retry logic for circular dependencies.
    /// </summary>
    private async Task CreateTargetDatabaseFunctions(List<ProgrammabilityModel> functionsToCreate, string dbConnectionString, bool isRetryPass)
    {
        if (isRetryPass)
        {
            Msg.Write(MessageType.Info, $"Retrying {functionsToCreate.Count} functions that depend on tables...");
        }
        else
        {
            Msg.Write(MessageType.Info, "Creating target database functions (Pass 1)...");
        }

        foreach (var function in functionsToCreate)
        {
            var isSuccess = await ScriptExecutionService.ExecuteSqlScript(function.Definition, dbConnectionString);
            
            if (!isSuccess)
            {
                if (!isRetryPass)
                {
                    // Pass 1 Failure: Expected for functions that query tables.
                    // Add to retry list and suppress the warning.
                    _failedFunctions.Add(function);
                    Msg.Write(MessageType.Debug, $"Deferred creation of function {function.SchemaName}.{function.ObjectName} (likely depends on tables).");
                }
                else
                {
                    // Pass 2 Failure: This is a real error.
                    Msg.Write(MessageType.Warning, $"Could not create function {function.SchemaName}.{function.ObjectName} even after table creation.");
                }
            }
        }

        if (isRetryPass && _failedFunctions.Any())
        {
            // Optional: Logic to report final success rate
            Msg.Write(MessageType.Success, "Function creation retry pass complete.");
        }
        Console.WriteLine("");
    }

    /// <summary>
    ///  Creates the target database user-defined data types.
    /// </summary>
    /// <param name="udfCreationScript">The script</param>
    /// <param name="dbConnectionString">The target db connection string</param>
    private static async Task CreateTargetDatabaseUDTs(string udfCreationScript, string dbConnectionString)
    {
        Msg.Write(MessageType.Info, "Creating target database user-defined data types...");
        var isSuccess = await ScriptExecutionService.ExecuteSqlScript(udfCreationScript, dbConnectionString);
        if (!isSuccess)
        {
            Msg.Write(MessageType.Error, "Failed to create the target database user-defined data types.");
            Environment.Exit(1);
        }

        Msg.Write(MessageType.Success, "Target database user-defined data types created successfully.");
        Console.WriteLine("");
    }

    /// <summary>
    ///  Creates the target database tables.
    /// </summary>
    /// <param name="tableCreationScript">The script</param>
    /// <param name="dbConnectionString">The target db connection string</param>
    private static async Task CreateTargetDatabaseTables(string tableCreationScript, string dbConnectionString)
    {
        Msg.Write(MessageType.Info, "Creating target database tables...");
        var isSuccess = await ScriptExecutionService.ExecuteSqlScript(tableCreationScript, dbConnectionString);
        if (!isSuccess)
        {
            Msg.Write(MessageType.Error, "Failed to create the target database tables.");
            Environment.Exit(1);
        }

        Msg.Write(MessageType.Success, "Target database tables created successfully.");
        Console.WriteLine("");
    }

    /// <summary>
    ///  Creates the target database constraints.
    /// </summary>
    /// <param name="sourceDbData">The source data that contains the individual constraints</param>
    /// <param name="dbConnectionString">The target db connection string</param>
    private static async Task CreateTargetDatabaseConstraints(SourceDbModel sourceDbData, string dbConnectionString)
    {
        var hasWarnings = false;
        Msg.Write(MessageType.Info, "Creating Constraints...");
        //Constraints need to be created before primary keys and relationships because they may include defaults, etc
        foreach (var constraint in sourceDbData.Constraints)
        {
            if (string.IsNullOrEmpty(constraint.Definition)) continue;
            var isSuccess = await ScriptExecutionService.ExecuteSqlScript(constraint.Definition, dbConnectionString);
            if (isSuccess) continue;
            Msg.Write(MessageType.Warning,
                $"Could not create constraint {constraint.SchemaName}.{constraint.ConstraintName} on table {constraint.TableName}. Continuing...");
            hasWarnings = true;
        }

        if (hasWarnings)
        {
            Msg.Write(MessageType.Warning,
                "There were some warnings while creating constraints. Please review the messages above.");
        }
        else
        {
            Msg.Write(MessageType.Success, "Target database constraints created successfully.");
        }

        Console.WriteLine("");
    }

    /// <summary>
    ///  Creates the target database primary keys.
    /// </summary>
    /// <param name="primaryKeyCreationScript">The script</param>
    /// <param name="dbConnectionString">The target db connection string</param>
    private static async Task CreateTargetDatabasePKs(string primaryKeyCreationScript, string dbConnectionString)
    {
        Msg.Write(MessageType.Info, "Creating target database primary keys...");
        var isSuccess = await ScriptExecutionService.ExecuteSqlScript(primaryKeyCreationScript, dbConnectionString);
        if (!isSuccess)
        {
            Msg.Write(MessageType.Error, "Failed to create the target database primary keys.");
            Environment.Exit(1);
        }

        Msg.Write(MessageType.Success, "Target database primary keys created successfully.");
        Console.WriteLine("");
    }

    /// <summary>
    ///  Creates the target database relationships/foreign keys.
    /// </summary>
    /// <param name="relationshipCreationScript">The script</param>
    /// <param name="dbConnectionString">The target db connection string</param>
    private static async Task CreateTargetDatabaseRelationships(string relationshipCreationScript,
        string dbConnectionString)
    {
        Msg.Write(MessageType.Info, "Creating target database relationships/foreign keys...");
        var isSuccess = await ScriptExecutionService.ExecuteSqlScript(relationshipCreationScript, dbConnectionString);
        if (!isSuccess)
        {
            Msg.Write(MessageType.Error, "Failed to create the target database relationships/foreign keys.");
            Environment.Exit(1);
        }

        Msg.Write(MessageType.Success, "Target database relationships/foreign keys created successfully.");
        Console.WriteLine("");
    }

    /// <summary>
    ///  Creates the target database indexes.
    /// </summary>
    /// <param name="indexCreationScript">The script</param>
    /// <param name="dbConnectionString">The target db connection string</param>
    private static async Task CreateTargetDatabaseIndexes(string indexCreationScript, string dbConnectionString)
    {
        Msg.Write(MessageType.Info, "Creating target database indexes...");
        var isSuccess = await ScriptExecutionService.ExecuteSqlScript(indexCreationScript, dbConnectionString);
        if (!isSuccess)
        {
            Msg.Write(MessageType.Error, "Failed to create the target database indexes.");
            Environment.Exit(1);
        }

        Msg.Write(MessageType.Success, "Target database indexes created successfully.");
        Console.WriteLine("");
    }

    /// <summary>
    ///  Creates the target database stored procedures.
    /// </summary>
    /// <param name="sourceDbData">The source data that contains the individual stored procedures</param>
    /// <param name="dbConnectionString">The target db connection string</param>
    private static async Task CreateTargetDatabaseStoredProcedures(SourceDbModel sourceDbData,
        string dbConnectionString)
    {
        var hasWarnings = false;
        Msg.Write(MessageType.Info, "Creating target database stored procedures...");
        foreach (var storedProcedure in sourceDbData.StoredProcedures)
        {
            var isSuccess =
                await ScriptExecutionService.ExecuteSqlScript(storedProcedure.Definition, dbConnectionString);
            if (!isSuccess)
            {
                Msg.Write(MessageType.Warning,
                    $"Could not create stored procedure {storedProcedure.SchemaName}.{storedProcedure.ObjectName}. Continuing...");
                hasWarnings = true;
            }
        }

        if (hasWarnings)
        {
            Msg.Write(MessageType.Warning,
                "There were some warnings while creating stored procedures. Please review the messages above.");
        }
        else
        {
            Msg.Write(MessageType.Success, "Target database stored procedures created successfully.");
        }

        Console.WriteLine("");
    }

    /// <summary>
    ///  Creates the target database views.
    /// </summary>
    /// <param name="sourceDbData">The source data that contains the individual views</param>
    /// <param name="dbConnectionString">The target db connection string</param>
    private static async Task CreateTargetDatabaseViews(SourceDbModel sourceDbData, string dbConnectionString)
    {
        var hasWarnings = false;
        Msg.Write(MessageType.Info, "Creating target database views...");
        foreach (var view in sourceDbData.Views)
        {
            var isSuccess = await ScriptExecutionService.ExecuteSqlScript(view.Definition, dbConnectionString);
            if (!isSuccess)
            {
                Msg.Write(MessageType.Warning,
                    $"Could not create view {view.SchemaName}.{view.ViewName}. Continuing...");
                hasWarnings = true;
            }
        }

        if (hasWarnings)
        {
            Msg.Write(MessageType.Warning,
                "There were some warnings while creating views. Please review the messages above.");
        }
        else
        {
            Msg.Write(MessageType.Success, "Target database views created successfully.");
        }

        Console.WriteLine("");
    }
}