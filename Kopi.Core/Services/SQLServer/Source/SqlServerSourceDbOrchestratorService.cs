using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Common;

namespace Kopi.Core.Services.SQLServer.Source;

public class SqlServerSourceDbOrchestratorService
{
    /// <summary>
    /// Kicks off the process of reading the source database schema, indexes, relationships, etc.
    /// </summary>
    /// <param name="config">The config file</param>
    /// <returns></returns>
    public static async Task<SourceDbModel> Begin(KopiConfig config)
    {
        if (KopiConfigService.ShouldAppendTrustServerCertificate())
            config.SourceConnectionString += ";TrustServerCertificate=True";
    
        var sqlServerVersion = await SqlServerSourceDbVersionService.GetVersion(config);
        var (storedProcedures, functions) =
            await SqlServerSourceDbProgrammabilityService.GetStoredProceduresAndFunctions(config);
        var tables = await SqlServerSourceDbTableService.GetTables(config);
        var constraints = await SqlServerSourceDbConstraintService.GetConstraints(config);
        var views = await SqlServerSourceDbViewsService.GetViews(config);
        var userDefinedDataTypes = await SqlServerSourceDbUserDefinedDataTypesService.GetUserDefinedDataTypes(config);
        var primaryKeys = await SqlServerSourceDbPrimaryKeyService.GetPrimaryKeys(config);
        var indexes = await SqlServerSourceDbIndexService.GetIndexes(config);
        var relationships = await SqlServerSourceDbRelationshipService.GetRelationships(config);
    
        var sourceDbModel = new SourceDbModel
        {
            DatabaseVersion = sqlServerVersion,
            UserDefinedDataTypes = userDefinedDataTypes,
            Views = views,
            Tables = tables,
            Constraints = constraints,
            PrimaryKeys = primaryKeys,
            Relationships = relationships,
            Indexes = indexes,
            StoredProcedures = storedProcedures,
            Functions = functions
        };
    
        return sourceDbModel;
    }
}