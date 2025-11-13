using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using Microsoft.Data.SqlClient;

namespace Kopi.Core.Services.SQLServer.Source;

public class SourceDbPrimaryKeyService
{
    /// <summary>
    /// Gets the primary keys from the source database
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public static async Task<List<PrimaryKeyModel>> GetPrimaryKeys(KopiConfig config)
    {
        var rawPrimaryKeyData = await GetRawPrimaryKeyData(config);
        
        var primaryKeyData = MapRawPrimaryKeysToPrimaryKeyModel(rawPrimaryKeyData);
        
        return primaryKeyData;
    }

    private static async Task<List<RawPrimaryKeyModel>> GetRawPrimaryKeyData(KopiConfig config)
    {
        const string sql = @"
            SELECT 
                s.name AS SchemaName,
                t.name AS TableName,
                i.name AS PrimaryKeyName,
                c.name AS ColumnName,
                ic.key_ordinal AS KeyOrder
            FROM sys.indexes i
            INNER JOIN sys.tables t ON i.object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
            WHERE i.is_primary_key = 1
                AND t.is_ms_shipped = 0
            ORDER BY s.name, t.name, i.name, ic.key_ordinal;";

        using IDbConnection conn = new SqlConnection(config.SourceConnectionString);
        try
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var data = await conn.QueryAsync<RawPrimaryKeyModel>(sql);
            Msg.Write(MessageType.Info,
                $"Found {data.Count()} primary key columns in source database.");
            return [.. data];
        }
        catch (SqlException ex)
        {
            Msg.Write(MessageType.Error, $"SQL Exception while reading primary keys from source database: {ex.Message}");
            Environment.Exit(1);
        }
        finally
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }

        return [];
    }
    
    
    private static List<PrimaryKeyModel> MapRawPrimaryKeysToPrimaryKeyModel(List<RawPrimaryKeyModel> rawPrimaryKeyData)
    {
        return rawPrimaryKeyData
            .GroupBy(pk => new { pk.SchemaName, pk.TableName, pk.PrimaryKeyName })
            .Select(g => new PrimaryKeyModel
            {
                SchemaName = g.Key.SchemaName,
                TableName = g.Key.TableName,
                PrimaryKeyName = g.Key.PrimaryKeyName,
                PrimaryKeyColumns = g.OrderBy(pk => pk.KeyOrder).Select(pk => pk.ColumnName).ToList()
            })
            .ToList();
    }
}