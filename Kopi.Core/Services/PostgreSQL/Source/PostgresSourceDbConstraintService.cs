using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.PostgreSQL;
using Kopi.Core.Utilities;
using Npgsql;

namespace Kopi.Core.Services.PostgreSQL.Source;

public class PostgresSourceDbConstraintService
{
    public static async Task<List<ConstraintModel>> GetConstraints(KopiConfig config)
        {
            var rawConstraints = await GetRawConstraintData(config);
            return MapRawConstraintsToModel(rawConstraints);
        }

        private static async Task<List<RawPostgresConstraintModel>> GetRawConstraintData(KopiConfig config)
        {
            // 'contype' values: p=primary, f=foreign, u=unique, c=check, t=trigger
            const string sql = @"
                SELECT 
                    n.nspname AS SchemaName,
                    t.relname AS TableName,
                    c.conname AS ConstraintName,
                    c.contype AS ConstraintType, 
                    pg_get_constraintdef(c.oid) AS Definition
                FROM pg_constraint c
                JOIN pg_class t ON c.conrelid = t.oid
                JOIN pg_namespace n ON t.relnamespace = n.oid
                WHERE n.nspname NOT IN ('information_schema', 'pg_catalog')";

            using IDbConnection conn = new NpgsqlConnection(config.SourceConnectionString);
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                
                // Dapper handles the mapping of Postgres 'char' type to C# 'char' automatically
                var result = await conn.QueryAsync<RawPostgresConstraintModel>(sql);
                return result.ToList();
            }
            catch (NpgsqlException ex)
            {
                Msg.Write(MessageType.Error, $"Postgres Exception reading constraints: {ex.Message}");
                throw;
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private static List<ConstraintModel> MapRawConstraintsToModel(List<RawPostgresConstraintModel> rawList)
        {
            return rawList.Select(c => new ConstraintModel
            {
                SchemaName = c.SchemaName,
                TableName = c.TableName,
                ConstraintName = c.ConstraintName,
                ConstraintType = MapConstraintType(c.ConstraintType),
                Definition = c.Definition
            }).ToList();
        }

        private static string MapConstraintType(char type)
        {
            return type switch
            {
                'p' => "PRIMARY KEY",
                'f' => "FOREIGN KEY",
                'u' => "UNIQUE",
                'c' => "CHECK",
                't' => "TRIGGER",
                _ => "UNKNOWN"
            };
        }
}