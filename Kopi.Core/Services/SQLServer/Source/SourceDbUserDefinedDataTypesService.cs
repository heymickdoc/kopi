using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using Microsoft.Data.SqlClient;

namespace Kopi.Core.Services.SQLServer.Source;

/// <summary>
/// Used to get user defined data types from the source database
/// </summary>
public class SourceDbUserDefinedDataTypesService
{
    /// <summary>
    /// Gets the user defined data types from the source database
    /// </summary>
    /// <param name="config">The initial config</param>
    /// <returns>The list of <see cref="RawUserDefinedDataTypesModel"/> data</returns>
    public static async Task<List<UserDefinedDataTypeModel>> GetUserDefinedDataTypes(KopiConfig config)
    {
        //Get the raw denormalized user defined data type data from the source database.
        var rawUserDefinedDataTypes = await GetRawUserDefinedDataTypeData(config);

        var userDefinedDataTypes = MapRawUserDefinedDataTypesToModel(rawUserDefinedDataTypes);
        return userDefinedDataTypes;
    }

    private static List<UserDefinedDataTypeModel> MapRawUserDefinedDataTypesToModel(
        List<RawUserDefinedDataTypesModel> rawUserDefinedDataTypes)
    {
        var userDefinedDataTypes = rawUserDefinedDataTypes
            .Select(r => new UserDefinedDataTypeModel
            {
                SchemaName = r.SchemaName,
                TypeName = r.TypeName,
                BaseTypeName = r.BaseTypeName,
                MaxLength = r.MaxLength.ToString(),
                Precision = r.Precision,
                Scale = r.Scale,
                IsNullable = r.IsNullable,
                CreateScript = r.CreateScript
            })
            .ToList();

        return userDefinedDataTypes;
    }

    /// <summary>
    /// Gets the list of user defined data types from the source SQL Server database. They're returned in a denormalized format (one row per user defined data type).
    /// </summary>
    /// <param name="config">The config file</param>
    /// <returns></returns>
    private static async Task<List<RawUserDefinedDataTypesModel>> GetRawUserDefinedDataTypeData(KopiConfig config)
    {
        const string sql = @"
            SELECT 
                SCHEMA_NAME(t.schema_id) AS SchemaName,
                t.name AS TypeName,
                b.name AS BaseTypeName,
                t.max_length,
                t.precision,
                t.scale,
                t.is_nullable,
                'CREATE TYPE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + '.' + QUOTENAME(t.name) + 
                ' FROM ' + QUOTENAME(b.name) + 
                CASE 
                    WHEN b.name IN ('varchar', 'char', 'varbinary', 'binary') 
                        THEN CASE WHEN t.max_length = -1 THEN '(MAX)' ELSE '(' + CAST(t.max_length AS VARCHAR) + ')' END
                    WHEN b.name IN ('nvarchar', 'nchar') 
                        THEN CASE WHEN t.max_length = -1 THEN '(MAX)' ELSE '(' + CAST(t.max_length/2 AS VARCHAR) + ')' END
                    WHEN b.name IN ('decimal', 'numeric') 
                        THEN '(' + CAST(t.precision AS VARCHAR) + ',' + CAST(t.scale AS VARCHAR) + ')'
                    ELSE ''
                END +
                CASE WHEN t.is_nullable = 0 THEN ' NOT NULL' ELSE '' END + ';' AS CreateScript
            FROM sys.types t
            INNER JOIN sys.types b ON t.system_type_id = b.user_type_id
            WHERE t.is_user_defined = 1
                AND t.is_table_type = 0
            ORDER BY SchemaName, TypeName;";

        using IDbConnection conn = new SqlConnection(config.SourceConnectionString);
        try
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var data = await conn.QueryAsync<RawUserDefinedDataTypesModel>(sql);
            Msg.Write(MessageType.Info,
                $"Found {data.Count()} user defined data types in source database.");
            return [.. data];
        }
        catch (SqlException ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }
}