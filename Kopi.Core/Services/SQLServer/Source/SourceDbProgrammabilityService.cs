using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using Microsoft.Data.SqlClient;

namespace Kopi.Core.Services.SQLServer.Source;

public class SourceDbProgrammabilityService
{
    /// <summary>
    /// Returns a tuple containing two lists: the first list contains stored procedures, and the second list contains functions.
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public static async Task<(List<ProgrammabilityModel>, List<ProgrammabilityModel>)> GetStoredProceduresAndFunctions(
        KopiConfig config)
    {
        var rawStoredProceduresAndFunctions = await GetRawStoredProceduresAndFunctions(config);

        var storedProcedures = TransformRawStoredProceduresAndFunctions(rawStoredProceduresAndFunctions, true);
        var functions = TransformRawStoredProceduresAndFunctions(rawStoredProceduresAndFunctions, false);
        
        return (storedProcedures, functions);
    }

    /// <summary>
    /// Gets the programmability objects (stored procedures and functions) from the source database and returns them as a list of <see cref="RawStoredProceduresAndFunctionsModel"/> objects.
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    private static async Task<List<RawStoredProceduresAndFunctionsModel>> GetRawStoredProceduresAndFunctions(
        KopiConfig config)
    {
        const string programmabilitySql = @"
            SELECT 
                s.name AS SchemaName,
                o.name AS ObjectName,
                o.type_desc AS ObjectType,
                m.definition AS Definition
            FROM sys.objects o
            INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
            LEFT JOIN sys.sql_modules m ON o.object_id = m.object_id
            WHERE o.is_ms_shipped = 0
                AND o.type IN ('P', 'FN', 'TF', 'IF')  -- Procedures and functions
            ORDER BY 
                CASE o.type 
                    WHEN 'P' THEN 1
                    WHEN 'FN' THEN 2
                    WHEN 'TF' THEN 3
                    WHEN 'IF' THEN 4
                    ELSE 5
                END,
                s.name, o.name;";

        using IDbConnection conn = new SqlConnection(config.SourceConnectionString);
        try
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var data = await conn.QueryAsync<RawStoredProceduresAndFunctionsModel>(programmabilitySql);
            Msg.Write(MessageType.Info,
                $"Found {data.Count()} stored procedures and functions in source database.");
            return [.. data];
        }
        catch (SqlException ex)
        {
            Msg.Write(MessageType.Error,
                $"SQL Exception while reading stored procedures and functions from source database: {ex.Message}");
            Environment.Exit(1);
        }
        finally
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }

        return new List<RawStoredProceduresAndFunctionsModel>();
    }

    private static List<ProgrammabilityModel> TransformRawStoredProceduresAndFunctions(
        List<RawStoredProceduresAndFunctionsModel> rawProgrammabilityItems, bool isStoredProcedure = true)
    {
        if (isStoredProcedure)
        {
            return rawProgrammabilityItems
                .Where(p => p.ObjectType == "SQL_STORED_PROCEDURE")
                .Select(p => new ProgrammabilityModel
                {
                    SchemaName = p.SchemaName,
                    ObjectName = p.ObjectName,
                    ObjectType = p.ObjectType,
                    Definition = p.Definition,
                })
                .ToList();
        }

        return rawProgrammabilityItems
            .Where(p => p.ObjectType != "SQL_STORED_PROCEDURE")
            .Select(p => new ProgrammabilityModel
            {
                SchemaName = p.SchemaName,
                ObjectName = p.ObjectName,
                ObjectType = p.ObjectType,
                Definition = p.Definition,
            })
            .ToList();
    }
}