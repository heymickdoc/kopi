using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using Microsoft.Data.SqlClient;

namespace Kopi.Core.Services.SQLServer.Source;

public class SourceDbIndexService
{
    public static async Task<List<IndexModel>> GetIndexes(KopiConfig config)
    {
        var rawIndexData = await GetRawIndexData(config);
        
        var indexData = MapRawIndexesToIndexModel(rawIndexData);
        return indexData;
    }
    
    private static async Task<List<RawIndexModel>> GetRawIndexData(KopiConfig config)
    {
        const string sql =
            @"SELECT 
                    s.name AS SchemaName,
                    t.name AS TableName, 
                    i.name AS IndexName,
                    c.name AS ColumnName,
                    i.is_unique AS IsUnique,
                    i.is_primary_key AS IsPrimaryKey,
                    ic.key_ordinal AS KeyOrdinal,
                    ic.is_included_column AS IsIncludedColumn
                FROM sys.tables t 
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id 
                INNER JOIN sys.indexes i ON t.object_id = i.object_id 
                INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id 
                INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id 
                WHERE t.is_ms_shipped = 0 
                AND i.type_desc IN ('CLUSTERED', 'NONCLUSTERED') 
                AND i.is_primary_key = 0
                ORDER BY s.name, t.name, i.name, ic.key_ordinal, ic.is_included_column;";

        using IDbConnection conn = new SqlConnection(config.SourceConnectionString);
        try
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var data = await conn.QueryAsync<RawIndexModel>(sql);
            Msg.Write(MessageType.Info,
                $"Found {data.Count()} index columns in source database.");

            return [.. data];
        }
        catch (SqlException ex)
        {
            Msg.Write(MessageType.Error, $"SQL Exception while reading indexes from source database: {ex.Message}");
            Environment.Exit(1);
        }
        finally
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }

        return new List<RawIndexModel>();
    }
    
    private static List<IndexModel> MapRawIndexesToIndexModel(List<RawIndexModel> rawIndexes)
    {
        var indexes = new List<IndexModel>();

        foreach (var index in rawIndexes)
        {
            var existingIndex =
                indexes.FirstOrDefault(idx => idx.IndexName == index.IndexName && idx.TableName == index.TableName);
            if (existingIndex == null)
            {
                existingIndex = new IndexModel
                {
                    SchemaName = index.SchemaName,
                    TableName = index.TableName,
                    IndexName = index.IndexName,
                    IndexColumns = [new() { KeyOrdinal = index.KeyOrdinal, ColumnName = index.ColumnName, IsIncludedColumn = index.IsIncludedColumn }],
                    IsUnique = index.IsUnique,
                    IsPrimaryKey = index.IsPrimaryKey
                };
                indexes.Add(existingIndex);
            }
            else
            {
                existingIndex.IndexColumns.Add(new IndexColumnModel
                    { KeyOrdinal = index.KeyOrdinal, ColumnName = index.ColumnName });
            }
        }

        //Ensure that columns are ordered correctly
        foreach (var idx in indexes)
        {
            idx.IndexColumns = idx.IndexColumns.OrderBy(ic => ic.KeyOrdinal).ToList();
        }

        return indexes;
    }
}