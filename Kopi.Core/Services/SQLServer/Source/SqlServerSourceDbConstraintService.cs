using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using Microsoft.Data.SqlClient;

namespace Kopi.Core.Services.SQLServer.Source;

/// <summary>
/// Gets the constraints (unique, check, default) from the source database.
/// </summary>
public class SqlServerSourceDbConstraintService
{
    public static async Task<List<ConstraintModel>> GetConstraints(KopiConfig config)
    {
        var rawConstraintData = await GetRawConstraintData(config);
        var constraintData = MapRawConstraintsToConstraintModel(rawConstraintData);

        return constraintData;
    }

    /// <summary>
    /// Maps the raw denormalized constraint data to a list of <see cref="ConstraintModel"/> objects.
    /// For now, they're identical, but this is where any transformation logic would go if needed in the future.
    /// </summary>
    /// <param name="rawConstraintData"></param>
    /// <returns></returns>
    private static List<ConstraintModel> MapRawConstraintsToConstraintModel(List<RawSqlServerConstraintModel> rawConstraintData)
    {
        var constraintData = new List<ConstraintModel>();
        foreach (var rawConstraint in rawConstraintData)
        {
            var existingConstraint = constraintData.FirstOrDefault(c =>
                c.SchemaName == rawConstraint.SchemaName &&
                c.TableName == rawConstraint.TableName &&
                c.ConstraintName == rawConstraint.ConstraintName);

            if (existingConstraint != null) continue;
            
            var constraintModel = new ConstraintModel
            {
                SchemaName = rawConstraint.SchemaName,
                TableName = rawConstraint.TableName,
                ConstraintName = rawConstraint.ConstraintName,
                ConstraintType = rawConstraint.ConstraintType,
                Definition = rawConstraint.CreateScript
            };
            constraintData.Add(constraintModel);
        }
        return constraintData;
    }

    private static async Task<List<RawSqlServerConstraintModel>> GetRawConstraintData(KopiConfig config)
    {
        const string sql = @"
            -- Get all constraint definitions with proper multi-column handling (excluding DEFAULT constraints)
            SELECT 
                s.name AS SchemaName,
                t.name AS TableName,
                c.name AS ConstraintName,
                c.type_desc AS ConstraintType,
                NULL AS ColumnName,
                CASE 
                    WHEN c.type = 'C' THEN cc.definition  -- Check constraints
                    ELSE NULL  -- Unique constraints handled in CreateScript
                END AS Definition,
                CASE 
                    WHEN c.type = 'C' THEN  -- Check constraint
                        'IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N''[' + s.name + '].[' + c.name + ']'') AND type = ''C'') ' +
                        'ALTER TABLE [' + s.name + '].[' + t.name + '] ADD CONSTRAINT [' + c.name + '] CHECK ' + cc.definition
                    WHEN c.type = 'UQ' THEN  -- Unique constraint (multi-column)
                        'IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N''[' + s.name + '].[' + c.name + ']'') AND type = ''UQ'') ' +
                        'ALTER TABLE [' + s.name + '].[' + t.name + '] ADD CONSTRAINT [' + c.name + '] UNIQUE (' + 
                        STUFF((
                            SELECT ', [' + c2.name + ']'
                            FROM sys.index_columns ic2
                            INNER JOIN sys.columns c2 ON ic2.object_id = c2.object_id AND ic2.column_id = c2.column_id
                            WHERE ic2.object_id = i.object_id AND ic2.index_id = i.index_id
                            ORDER BY ic2.key_ordinal
                            FOR XML PATH('')
                        ), 1, 2, '') + ')'
                    ELSE ''
                END AS CreateScript
            FROM sys.tables t 
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id 
            INNER JOIN sys.objects c ON t.object_id = c.parent_object_id 
            LEFT JOIN sys.check_constraints cc ON c.object_id = cc.object_id 
            LEFT JOIN sys.indexes i ON c.object_id = i.object_id  -- For unique constraints
            WHERE t.is_ms_shipped = 0 
            AND c.type IN ('C', 'UQ')  -- C=Check, UQ=Unique (removed D=Default)
            ORDER BY s.name, t.name, c.name;";

        using IDbConnection conn = new SqlConnection(config.SourceConnectionString);
        try
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var data = await conn.QueryAsync<RawSqlServerConstraintModel>(sql);
            Msg.Write(MessageType.Info,
                $"Found {data.Count()} constraints in source database.");

            return data.ToList();
        }
        catch (SqlException ex)
        {
            Msg.Write(MessageType.Error, $"SQL Exception while reading constraints from source database: {ex.Message}");
            Environment.Exit(1);
        }
        finally
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }
        return new List<RawSqlServerConstraintModel>();
    }
}