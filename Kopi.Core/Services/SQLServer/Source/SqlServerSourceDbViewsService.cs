using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using Microsoft.Data.SqlClient;

namespace Kopi.Core.Services.SQLServer.Source;

/// <summary>
/// Get views from the source database
/// </summary>
public class SqlServerSourceDbViewsService
{
    /// <summary>
    /// Gets the views from the source database
    /// </summary>
    /// <param name="config">The initial config</param>
    /// <returns>The list of <see cref="ViewModel"/> data</returns>
    public static async Task<List<ViewModel>> GetViews(KopiConfig config)
    {
        //Get the raw denormalized view data from the source database.
        var rawViews = await GetRawViewData(config);
        
        var views = MapRawViewsToModel(rawViews);
        return views;
    }

    private static List<ViewModel> MapRawViewsToModel(List<RawSqlServerViewModel> rawViews)
    {
        var views = rawViews
            .Select(r => new ViewModel
            {
                SchemaName = r.SchemaName,
                ViewName = r.ViewName,
                Definition = r.Definition,
                CreateScript = r.CreateScript
            })
            .ToList();

        return views;
    }

    /// <summary>
    /// Gets the list of views from the source SQL Server database. They're returned in a denormalized format (one row per view).
    /// </summary>
    /// <param name="config">The config file</param>
    /// <returns></returns>
    private static async Task<List<RawSqlServerViewModel>> GetRawViewData(KopiConfig config)
    {
        const string sql = @"
            SELECT 
                SCHEMA_NAME(v.schema_id) AS SchemaName,
                v.name AS ViewName,
                m.definition AS Definition,
                'CREATE VIEW ' + QUOTENAME(SCHEMA_NAME(v.schema_id)) + '.' + QUOTENAME(v.name) + ' AS ' + CHAR(13) + CHAR(10) + m.definition AS CreateScript
            FROM 
                sys.views v
            JOIN 
                sys.sql_modules m ON v.object_id = m.object_id
            WHERE 
                v.is_ms_shipped = 0
            ORDER BY 
                SchemaName, ViewName;
        ";
        
        using IDbConnection conn = new SqlConnection(config.SourceConnectionString);
        try
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var data = await conn.QueryAsync<RawSqlServerViewModel>(sql);
            Msg.Write(MessageType.Info,
                $"Found {data.Count()} views in source database.");
            return [.. data];
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

}