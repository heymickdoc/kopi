using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.PostgreSQL;
using Kopi.Core.Utilities;
using Npgsql;

namespace Kopi.Core.Services.PostgreSQL.Source;

public class PostgresSourceDbIndexService
{
    public static async Task<List<IndexModel>> GetIndexes(KopiConfig config)
        {
            var rawIndexes = await GetRawIndexData(config);
            return MapRawIndexesToIndexModel(rawIndexes);
        }

        private static async Task<List<RawPostgresIndexModel>> GetRawIndexData(KopiConfig config)
        {
            // We unroll the 'indkey' array to get one row per column per index
            const string sql = @"
                SELECT 
                    n.nspname AS SchemaName,
                    t.relname AS TableName,
                    i.relname AS IndexName,
                    a.attname AS ColumnName,
                    ix.indisunique AS IsUnique,
                    ix.indisprimary AS IsPrimaryKey,
                    
                    -- Calculate the position of the column within the index
                    array_position(ix.indkey, a.attnum) AS KeyOrdinal,
                    
                    -- PG11+ supports included columns via 'indnkeyatts' vs 'indnatts'
                    -- For compatibility, we'll assume false for now or check if ordinal > indnkeyatts
                    CASE 
                        WHEN array_position(ix.indkey, a.attnum) > ix.indnkeyatts THEN true 
                        ELSE false 
                    END AS IsIncludedColumn

                FROM pg_index ix
                JOIN pg_class t ON ix.indrelid = t.oid
                JOIN pg_class i ON ix.indexrelid = i.oid
                JOIN pg_namespace n ON t.relnamespace = n.oid
                JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(ix.indkey)
                WHERE n.nspname NOT IN ('information_schema', 'pg_catalog')
                ORDER BY n.nspname, t.relname, i.relname, KeyOrdinal;";

            using IDbConnection conn = new NpgsqlConnection(config.SourceConnectionString);
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                var data = await conn.QueryAsync<RawPostgresIndexModel>(sql);
                return data.ToList();
            }
            catch (NpgsqlException ex)
            {
                Msg.Write(MessageType.Error, $"Postgres Exception reading indexes: {ex.Message}");
                throw;
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private static List<IndexModel> MapRawIndexesToIndexModel(List<RawPostgresIndexModel> rawList)
        {
            var indexes = new List<IndexModel>();

            foreach (var row in rawList)
            {
                var existingIndex = indexes.FirstOrDefault(x => x.SchemaName == row.SchemaName && x.TableName == row.TableName && x.IndexName == row.IndexName);
                
                if (existingIndex == null)
                {
                    existingIndex = new IndexModel
                    {
                        SchemaName = row.SchemaName,
                        TableName = row.TableName,
                        IndexName = row.IndexName,
                        IsUnique = row.IsUnique,
                        IsPrimaryKey = row.IsPrimaryKey,
                        IndexColumns = new List<IndexColumnModel>()
                    };
                    indexes.Add(existingIndex);
                }

                existingIndex.IndexColumns.Add(new IndexColumnModel
                {
                    ColumnName = row.ColumnName,
                    KeyOrdinal = row.KeyOrdinal ?? 0,
                    IsIncludedColumn = row.IsIncludedColumn
                });
            }

            return indexes;
        }
}