using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.PostgreSQL;
using Kopi.Core.Utilities;
using Npgsql;

namespace Kopi.Core.Services.PostgreSQL.Source;

public class PostgresSourceDbRelationshipService
{
    public static async Task<List<RelationshipModel>> GetRelationships(KopiConfig config)
        {
            var rawRels = await GetRawRelationshipData(config);
            return MapRawRelationshipsToModel(rawRels);
        }

        private static async Task<List<RawPostgresRelationshipModel>> GetRawRelationshipData(KopiConfig config)
        {
            const string sql = @"
                SELECT
                    tc.constraint_name AS ConstraintName,
                    tc.table_schema AS SchemaName,
                    tc.table_name AS TableName,
                    kcu.column_name AS ColumnName,
                    ccu.table_schema AS ForeignSchemaName,
                    ccu.table_name AS ForeignTableName,
                    ccu.column_name AS ForeignColumnName,
                    kcu.ordinal_position AS OrdinalPosition
                FROM 
                    information_schema.table_constraints AS tc 
                    JOIN information_schema.key_column_usage AS kcu
                      ON tc.constraint_name = kcu.constraint_name
                      AND tc.table_schema = kcu.table_schema
                    JOIN information_schema.constraint_column_usage AS ccu
                      ON ccu.constraint_name = tc.constraint_name
                      AND ccu.table_schema = tc.table_schema
                WHERE tc.constraint_type = 'FOREIGN KEY'
                    AND tc.table_schema NOT IN ('information_schema', 'pg_catalog')
                ORDER BY tc.constraint_name, kcu.ordinal_position;";

            using IDbConnection conn = new NpgsqlConnection(config.SourceConnectionString);
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                var data = await conn.QueryAsync<RawPostgresRelationshipModel>(sql);
                return data.ToList();
            }
            catch (NpgsqlException ex)
            {
                Msg.Write(MessageType.Error, $"Postgres Exception reading relationships: {ex.Message}");
                throw;
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private static List<RelationshipModel> MapRawRelationshipsToModel(List<RawPostgresRelationshipModel> rawList)
        {
            var relationships = new List<RelationshipModel>();

            foreach (var row in rawList)
            {
                var existingRel = relationships.FirstOrDefault(r => r.ForeignKeyName == row.ConstraintName && r.ParentSchema == row.SchemaName);

                if (existingRel == null)
                {
                    existingRel = new RelationshipModel
                    {
                        ForeignKeyName = row.ConstraintName,
                        // Parent in generic model usually refers to the table holding the constraint (The Child in SQL terms)
                        ParentSchema = row.SchemaName,
                        ParentTable = row.TableName,
                        ReferencedSchema = row.ForeignSchemaName,
                        ReferencedTable = row.ForeignTableName,
                        ForeignKeyColumns = new List<ForeignKeyColumnModel>()
                    };
                    relationships.Add(existingRel);
                }

                existingRel.ForeignKeyColumns.Add(new ForeignKeyColumnModel
                {
                    ParentColumnName = row.ColumnName,
                    ReferencedColumnName = row.ForeignColumnName,
                    KeyOrdinal = row.OrdinalPosition
                });
            }

            return relationships;
        }
}