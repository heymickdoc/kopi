using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.PostgreSQL;
using Kopi.Core.Utilities;
using Npgsql;

namespace Kopi.Core.Services.PostgreSQL.Source;

public class PostgresSourceDbUserDefinedDataTypesService
{
    public static async Task<List<UserDefinedDataTypeModel>> GetUserDefinedDataTypes(KopiConfig config)
        {
            var rawTypes = await GetRawData(config);
            return MapToModel(rawTypes);
        }

        private static async Task<List<RawPostgresUserDefinedDataTypeModel>> GetRawData(KopiConfig config)
        {
            // Fetch Domains ('d') and Enums ('e')
            const string sql = @"
                SELECT 
                    n.nspname AS SchemaName,
                    t.typname AS TypeName,
                    format_type(t.typbasetype, t.typtypmod) AS BaseTypeName,
                    t.typtype AS TypeType
                FROM pg_type t
                JOIN pg_namespace n ON t.typnamespace = n.oid
                WHERE t.typtype IN ('d', 'e')
                  AND n.nspname NOT IN ('information_schema', 'pg_catalog');";

            using IDbConnection conn = new NpgsqlConnection(config.SourceConnectionString);
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                var result = await conn.QueryAsync<RawPostgresUserDefinedDataTypeModel>(sql);
                return result.ToList();
            }
            catch (Exception ex)
            {
                Msg.Write(MessageType.Error, $"Error reading UDTs: {ex.Message}");
                throw;
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private static List<UserDefinedDataTypeModel> MapToModel(List<RawPostgresUserDefinedDataTypeModel> rawList)
        {
            return rawList.Select(r => new UserDefinedDataTypeModel
            {
                SchemaName = r.SchemaName,
                TypeName = r.TypeName,
                BaseTypeName = r.BaseTypeName,
                // Postgres doesn't strictly follow precision/scale for domains in the same way, 
                // typically it's embedded in the BaseTypeName (e.g. numeric(10,2))
                CreateScript = r.TypeType == 'e' 
                    ? $"-- Enum creation script placeholder" 
                    : $"CREATE DOMAIN \"{r.SchemaName}\".\"{r.TypeName}\" AS {r.BaseTypeName};"
            }).ToList();
        }
}