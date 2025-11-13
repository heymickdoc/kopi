using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using Microsoft.Data.SqlClient;

namespace Kopi.Core.Services.SQLServer.Source;

public class SourceDbRelationshipService
{
    public static async Task<List<RelationshipModel>> GetRelationships(KopiConfig config)
    {
        var rawRelationshipData = await GetRawRelationshipData(config);
        
        var relationshipData = MapRawRelationshipsToRelationshipModel(rawRelationshipData);
        
        return relationshipData;
    }
    
    /// <summary>
    /// Gets the relationships from the source SQL Server database
    /// </summary>
    /// <param name="config">the config file</param>
    /// <returns></returns>
    private static async Task<List<RawRelationshipModel>> GetRawRelationshipData(KopiConfig config)
    {
        const string sql = @"
            SELECT 
                fk.name AS ForeignKeyName,
                ps.name AS ParentSchema,
                tp.name AS ParentTable,
                cp.name AS ParentColumn,
                rs.name AS ReferencedSchema,
                tr.name AS ReferencedTable,
                cr.name AS ReferencedColumn,
                fkc.constraint_column_id AS ColumnOrder
            FROM sys.foreign_keys fk 
            INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id 
            INNER JOIN sys.tables tp ON fkc.parent_object_id = tp.object_id 
            INNER JOIN sys.schemas ps ON tp.schema_id = ps.schema_id
            INNER JOIN sys.columns cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id 
            INNER JOIN sys.tables tr ON fkc.referenced_object_id = tr.object_id 
            INNER JOIN sys.schemas rs ON tr.schema_id = rs.schema_id
            INNER JOIN sys.columns cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id 
            ORDER BY ps.name, tp.name, fk.name, fkc.constraint_column_id;";
        
        using IDbConnection conn = new SqlConnection(config.SourceConnectionString);
        try
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var data = await conn.QueryAsync<RawRelationshipModel>(sql);
            Msg.Write(MessageType.Info,
                $"Found {data.Count()} foreign key columns in source database.");

            return [.. data];
        }
        catch (SqlException ex)
        {
            Msg.Write(MessageType.Error,
                $"SQL Exception while reading relationships from source database: {ex.Message}");
            Environment.Exit(1);
        }
        finally
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }

        return new List<RawRelationshipModel>();
    }
    
    private static List<RelationshipModel> MapRawRelationshipsToRelationshipModel(List<RawRelationshipModel> rawRelationships)
    {
        var relationships = new List<RelationshipModel>();

        foreach (var rel in rawRelationships)
        {
            //Check to see if we already have this foreign key relationship
            var exists = relationships.Any(r =>
                r.ForeignKeyName == rel.ForeignKeyName);
            
            //If it exists, we add the column to the existing relationship
            if (exists)
            {
                var existingRel = relationships.First(r => r.ForeignKeyName == rel.ForeignKeyName);
                existingRel.ForeignKeyColumns.Add(new ForeignKeyColumnModel
                {
                    ParentColumnName = rel.ParentColumn,
                    ReferencedColumnName = rel.ReferencedColumn,
                    KeyOrdinal = existingRel.ForeignKeyColumns.Count + 1
                });
                continue;
            }
            
            relationships.Add(new RelationshipModel
            {
                ForeignKeyName = rel.ForeignKeyName,
                ParentSchema = rel.ParentSchema,
                ParentTable = rel.ParentTable,
                ReferencedSchema = rel.ReferencedSchema,
                ReferencedTable = rel.ReferencedTable,
                ForeignKeyColumns = new List<ForeignKeyColumnModel>
                {
                    new()
                    {
                        ParentColumnName = rel.ParentColumn,
                        ReferencedColumnName = rel.ReferencedColumn,
                        KeyOrdinal = 1
                    }
                }
            });
        }

        return relationships;
    }
}