using System.Data.SqlClient;
using Dapper;
using Kopi.Core.Interfaces;
using Kopi.Core.Models.SQLServer; // To access SourceDbModel
using Kopi.Core.Models.Common;
using Kopi.Core.Utilities;
using Microsoft.Data.SqlClient;

namespace Kopi.Core.Services.SQLServer.PostProcessing;

public class SqlServerEfMigrationHistoryService(
    KopiConfig config,
    SourceDbModel sourceDbModel) : IEfMigrationHistoryService
{
    private const string TableName = "__EFMigrationsHistory";

    public async Task CopyMigrationHistoryAsync(string targetConnectionString)
    {
        // 1. Find the table in our cached schema
        var historyTable = sourceDbModel.Tables
            .FirstOrDefault(t => t.TableName.Equals(TableName, StringComparison.OrdinalIgnoreCase));

        if (historyTable == null)
        {
            Msg.Write(MessageType.Debug, "No EF Migrations table found in source model. Skipping.");
            return;
        }

        Msg.Write(MessageType.Info, "Detected Entity Framework. Syncing migration history...");

        // 2. Read from Source (using the correct schema found in the model)
        var rows = await GetSourceHistory(historyTable.SchemaName);

        // 3. Insert into Target
        if (rows.Any())
        {
            await InsertIntoTarget(rows, historyTable.SchemaName, targetConnectionString);
            Msg.Write(MessageType.Success, $"Synced {rows.Count} migrations from {historyTable.SchemaName}.{TableName}.");
        }
    }

    private async Task<List<EfHistoryRow>> GetSourceHistory(string schema)
    {
        using var conn = new SqlConnection(config.SourceConnectionString);
        // Use the schema we discovered
        var sql = $"SELECT MigrationId, ProductVersion FROM [{schema}].[{TableName}]";
        
        var rows = await conn.QueryAsync<EfHistoryRow>(sql);
        return rows.ToList();
    }

    private async Task InsertIntoTarget(List<EfHistoryRow> rows, string schema, string targetConnectionString)
    {
        using var conn = new SqlConnection(targetConnectionString);
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();

        try
        {
            // We use a merge-style check to be safe (idempotent)
            var insertSql = $@"
                IF NOT EXISTS (SELECT 1 FROM [{schema}].[{TableName}] WHERE MigrationId = @MigrationId)
                BEGIN
                    INSERT INTO [{schema}].[{TableName}] (MigrationId, ProductVersion)
                    VALUES (@MigrationId, @ProductVersion)
                END";

            await conn.ExecuteAsync(insertSql, rows, transaction: transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}