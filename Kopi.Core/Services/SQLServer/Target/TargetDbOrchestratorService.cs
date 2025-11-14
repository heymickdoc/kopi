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

    /// <summary>
    /// Begins the process of preparing the target database schema, indexes, relationships, etc.
    /// </summary>
    /// <returns></returns>
    public async Task<string> PrepareTargetDb()
    {
        var (sqlPort, sqlPassword) = await HandleDockerContainerLifecycle(config, sourceDbData);

        //At this point, we have a running container/database. Let's generate the SQL scripts to create the schema, indexes, relationships, etc.
        Msg.Write(MessageType.Info, "Generating target database creation scripts...");
        var dbCreationScript = ScriptCreationService.GenerateDatabaseCreationScript(config, sourceDbData);
        var schemaCreationScript = ScriptCreationService.GenerateSchemaCreationScript(config, sourceDbData);
        var udfCreationScript = ScriptCreationService.GenerateUserDefinedDataTypeCreationScript(config, sourceDbData);
        var tableCreationScript = ScriptCreationService.GenerateTableCreationScript(config, sourceDbData);
        var primaryKeyCreationScript = ScriptCreationService.GeneratePrimaryKeyCreationScript(config, sourceDbData);
        var relationshipCreationScript = ScriptCreationService.GenerateRelationshipCreationScript(config, sourceDbData);
        var indexCreationScript = ScriptCreationService.GenerateIndexCreationScript(config, sourceDbData);
        var masterDbConnectionString =
            ScriptExecutionService.GenerateTargetDbConnectionString(config, sqlPort, sqlPassword, isDbCreation: true);
        var targetDbConnectionString =
            ScriptExecutionService.GenerateTargetDbConnectionString(config, sqlPort, sqlPassword, isDbCreation: false);
        Msg.Write(MessageType.Success, "Target database creation scripts generated successfully.");
        Console.WriteLine("");

        //Execute the scripts in order
        Msg.Write(MessageType.Info, "Creating target database...");
        await CreateTargetDatabaseCore(dbCreationScript, masterDbConnectionString);
        await CreateTargetDatabaseSchema(schemaCreationScript, targetDbConnectionString);
        await CreateTargetDatabaseFunctions(sourceDbData, targetDbConnectionString);
        await CreateTargetDatabaseUDFs(udfCreationScript, targetDbConnectionString);
        await CreateTargetDatabaseTables(tableCreationScript, targetDbConnectionString);
        await CreateTargetDatabasePKs(primaryKeyCreationScript, targetDbConnectionString);
        await CreateTargetDatabaseIndexes(indexCreationScript, targetDbConnectionString);
        await CreateTargetDatabaseConstraints(sourceDbData, targetDbConnectionString);
        await CreateTargetDatabaseRelationships(relationshipCreationScript, targetDbConnectionString);

        var generatedData = await dataOrchestratorService.OrchestrateDataGeneration();
        var dataInsertionService = new DataInsertionService();
        await dataInsertionService.InsertData(config, sourceDbData, generatedData, targetDbConnectionString);
        // --- End Data Generation ---

        await CreateTargetDatabaseStoredProcedures(sourceDbData, targetDbConnectionString);
        await CreateTargetDatabaseViews(sourceDbData, targetDbConnectionString);


        Msg.Write(MessageType.Success, "Target database is ready.");
        Console.WriteLine("");

        return targetDbConnectionString;
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
    /// Creates the target database schema.
    /// </summary>
    /// <param name="schemaCreationScript">The script</param>
    /// <param name="dbConnectionString">The target db connection string</param>
    private static async Task CreateTargetDatabaseSchema(string schemaCreationScript, string dbConnectionString)
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
    ///  Creates the target database functions.
    /// </summary>
    /// <param name="sourceDbData">The source data that contains the individual functions</param>
    /// <param name="dbConnectionString">The target db connection string</param>
    private static async Task CreateTargetDatabaseFunctions(SourceDbModel sourceDbData, string dbConnectionString)
    {
        var hasWarnings = false;
        Msg.Write(MessageType.Info, "Creating target database functions...");
        foreach (var function in sourceDbData.Functions)
        {
            var isSuccess = await ScriptExecutionService.ExecuteSqlScript(function.Definition, dbConnectionString);
            if (isSuccess) continue;
            Msg.Write(MessageType.Warning,
                $"Could not create function {function.SchemaName}.{function.ObjectName}. Continuing...");
            hasWarnings = true;
        }

        if (hasWarnings)
        {
            Msg.Write(MessageType.Warning,
                "There were some warnings while creating functions. Please review the messages above.");
        }
        else
        {
            Msg.Write(MessageType.Success, "Target database functions created successfully.");
        }

        Console.WriteLine("");
    }

    /// <summary>
    ///  Creates the target database user-defined data types.
    /// </summary>
    /// <param name="udfCreationScript">The script</param>
    /// <param name="dbConnectionString">The target db connection string</param>
    private static async Task CreateTargetDatabaseUDFs(string udfCreationScript, string dbConnectionString)
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