using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.PostgreSQL;
using Kopi.Core.Utilities;
using Npgsql;

namespace Kopi.Core.Services.PostgreSQL.Source;

public class PostgresSourceDbProgrammabilityService
{
    /// <summary>
    /// Retrieves both Stored Procedures and Functions
    /// </summary>
    public static async Task<(List<ProgrammabilityModel> Procs, List<ProgrammabilityModel> Funcs)> GetProgrammability(
        KopiConfig config)
    {
        var rawRoutines = await GetRawRoutineData(config);
        return MapRawRoutinesToModel(rawRoutines);
    }

    private static async Task<List<RawPostgresRoutineModel>> GetRawRoutineData(KopiConfig config)
    {
        // pg_proc contains both functions and procedures.
        // prokind: 'f' = function, 'p' = procedure.
        const string sql = @"
            SELECT 
                n.nspname AS SchemaName,
                p.proname AS RoutineName,
                CASE 
                    WHEN p.prokind = 'p' THEN 'PROCEDURE'
                    ELSE 'FUNCTION'
                END AS RoutineType,
                pg_get_functiondef(p.oid) AS Definition,
                format_type(p.prorettype, null) AS DataType
            FROM pg_proc p
            JOIN pg_namespace n ON p.pronamespace = n.oid
            WHERE n.nspname NOT IN ('information_schema', 'pg_catalog', 'pg_toast')
              AND n.nspname NOT LIKE 'pg_temp_%' -- Temporary schemas
              AND n.nspname NOT LIKE 'pg_toast_%' 
              AND p.prokind IN ('f', 'p')
              -- Optional: Filter out extension-owned functions if you don't want to copy them
              AND NOT EXISTS (SELECT 1 FROM pg_depend WHERE objid = p.oid AND deptype = 'e')
            ORDER BY n.nspname, p.proname;";

        using IDbConnection conn = new NpgsqlConnection(config.SourceConnectionString);
        try
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var data = await conn.QueryAsync<RawPostgresRoutineModel>(sql);
            return data.ToList();
        }
        catch (NpgsqlException ex)
        {
            Msg.Write(MessageType.Error, $"Postgres Exception reading routines: {ex.Message}");
            throw;
        }
        finally
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }
    }

    private static (List<ProgrammabilityModel>, List<ProgrammabilityModel>) MapRawRoutinesToModel(
        List<RawPostgresRoutineModel> rawList)
    {
        var procs = new List<ProgrammabilityModel>();
        var funcs = new List<ProgrammabilityModel>();

        foreach (var row in rawList)
        {
            var model = new ProgrammabilityModel
            {
                SchemaName = row.SchemaName,
                ObjectName = row.RoutineName,
                ObjectType = row.RoutineType,
                Definition = row.Definition
            };

            if (row.RoutineType == "PROCEDURE")
            {
                procs.Add(model);
            }
            else
            {
                // Postgres has TABLE VALUED functions (returns SETOF), 
                // but for simplicity we treat all non-procedures as functions here.
                funcs.Add(model);
            }
        }

        return (procs, funcs);
    }
}