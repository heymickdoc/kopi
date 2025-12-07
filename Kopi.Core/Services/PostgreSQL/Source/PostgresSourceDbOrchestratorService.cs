using Kopi.Core.Models.Common;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.PostgreSQL.Source;

public class PostgresSourceDbOrchestratorService
{
    /// <summary>
    /// Kicks off the process of reading the source PostgreSQL database schema.
    /// </summary>
    public static async Task<SourceDbModel> Begin(KopiConfig config)
    {
        Msg.Write(MessageType.Info, "Starting PostgreSQL Schema Extraction...");

        // 1. Version (Sequential as it's fast)
        var dbVersion = await PostgresSourceDbVersionService.GetDatabaseVersion(config);

        // 2. Fire off all schema tasks in parallel
        // We use the specific Postgres service implementations here
        var tables = await PostgresSourceDbTableService.GetTables(config);
        var views = await PostgresSourceDbViewsService.GetViews(config);
        var constraints = await PostgresSourceDbConstraintService.GetConstraints(config);
        var pks = await PostgresSourceDbPrimaryKeyService.GetPrimaryKeys(config);
        var indexes = await PostgresSourceDbIndexService.GetIndexes(config);
        var relationships = await PostgresSourceDbRelationshipService.GetRelationships(config);
        var (storedProcedures, functions) = await PostgresSourceDbProgrammabilityService.GetProgrammability(config);
        var udt = await PostgresSourceDbUserDefinedDataTypesService.GetUserDefinedDataTypes(config);
        var extensions = await PostgresSourceDbExtensionService.GetExtensions(config);

        // 3. Assemble the Model
        var sourceDbModel = new SourceDbModel
        {
            // Ensure your SourceDbModel has this property, or map to SqlServerVersion if shared
            DatabaseVersion = dbVersion,
            Extensions = extensions,
            UserDefinedDataTypes = udt,
            Views = views,
            Tables = tables,
            Constraints = constraints,
            PrimaryKeys = pks,
            Relationships = relationships,
            Indexes = indexes,
            StoredProcedures = storedProcedures,
            Functions = functions
        };

        Msg.Write(MessageType.Success, "PostgreSQL Schema Extraction Complete.");
        return sourceDbModel;
    }
}