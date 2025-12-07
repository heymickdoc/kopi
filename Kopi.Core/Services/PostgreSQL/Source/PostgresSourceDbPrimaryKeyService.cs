using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.PostgreSQL;
using Kopi.Core.Utilities;
using Npgsql;

namespace Kopi.Core.Services.PostgreSQL.Source;

public class PostgresSourceDbPrimaryKeyService
{
    public static async Task<List<PrimaryKeyModel>> GetPrimaryKeys(KopiConfig config)
        {
            var rawPks = await GetRawPrimaryKeyData(config);
            return MapRawPksToModel(rawPks);
        }

        private static async Task<List<RawPostgresPrimaryKeyModel>> GetRawPrimaryKeyData(KopiConfig config)
        {
            // We need the constraint name and the columns in order
            const string sql = @"
                SELECT 
                    tc.table_schema AS SchemaName,
                    tc.table_name AS TableName,
                    tc.constraint_name AS PrimaryKeyName,
                    kcu.column_name AS ColumnName,
                    kcu.ordinal_position AS KeyOrder
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu 
                  ON tc.constraint_name = kcu.constraint_name 
                  AND tc.table_schema = kcu.table_schema
                WHERE tc.constraint_type = 'PRIMARY KEY'
                  AND tc.table_schema NOT IN ('information_schema', 'pg_catalog')
                ORDER BY tc.table_schema, tc.table_name, kcu.ordinal_position;";

            using IDbConnection conn = new NpgsqlConnection(config.SourceConnectionString);
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                // You will need to create this simple Raw model in Models/PostgreSQL
                return (await conn.QueryAsync<RawPostgresPrimaryKeyModel>(sql)).ToList();
            }
            catch (Exception ex)
            {
                Msg.Write(MessageType.Error, $"Error reading PKs: {ex.Message}");
                throw;
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private static List<PrimaryKeyModel> MapRawPksToModel(List<RawPostgresPrimaryKeyModel> rawList)
        {
            var result = new List<PrimaryKeyModel>();

            foreach (var row in rawList)
            {
                var existing = result.FirstOrDefault(x => x.SchemaName == row.SchemaName && x.TableName == row.TableName);
                if (existing == null)
                {
                    existing = new PrimaryKeyModel
                    {
                        SchemaName = row.SchemaName,
                        TableName = row.TableName,
                        PrimaryKeyName = row.PrimaryKeyName,
                        PrimaryKeyColumns = new List<string>()
                    };
                    result.Add(existing);
                }
                existing.PrimaryKeyColumns.Add(row.ColumnName);
            }
            return result;
        }
}