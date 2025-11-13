using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Common;

namespace Kopi.Core.Services.SQLServer.Source;

public class SourceDbOrchestratorService
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
    
        var sqlServerVersion = await SourceDbVersionService.GetVersion(config);
        var (storedProcedures, functions) =
            await SourceDbProgrammabilityService.GetStoredProceduresAndFunctions(config);
        var tables = await SourceDbTableService.GetTables(config);
        var constraints = await SourceDbConstraintService.GetConstraints(config);
        var views = await SourceDbViewsService.GetViews(config);
        var userDefinedDataTypes = await SourceDbUserDefinedDataTypesService.GetUserDefinedDataTypes(config);
        var primaryKeys = await SourceDbPrimaryKeyService.GetPrimaryKeys(config);
        var indexes = await SourceDbIndexService.GetIndexes(config);
        var relationships = await SourceDbRelationshipService.GetRelationships(config);
    
        var sourceDbModel = new SourceDbModel
        {
            SqlServerVersion = sqlServerVersion,
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