using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.PostgreSQL;
using Kopi.Core.Utilities;
using Npgsql;

namespace Kopi.Core.Services.PostgreSQL.Source;

public class PostgresSourceDbViewsService
{
    public static async Task<List<ViewModel>> GetViews(KopiConfig config)
    {
        var rawViews = await GetRawViewData(config);
        return MapRawViewsToModel(rawViews);
    }

    private static async Task<List<RawPostgresViewModel>> GetRawViewData(KopiConfig config)
    {
        const string sql = @"
                SELECT 
                    schemaname AS SchemaName, 
                    viewname AS ViewName, 
                    definition AS Definition 
                FROM pg_views
                WHERE schemaname NOT IN ('information_schema', 'pg_catalog');";

        using IDbConnection conn = new NpgsqlConnection(config.SourceConnectionString);
        try
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var data = await conn.QueryAsync<RawPostgresViewModel>(sql);
            return data.ToList();
        }
        catch (NpgsqlException ex)
        {
            Msg.Write(MessageType.Error, $"Postgres Exception reading views: {ex.Message}");
            throw;
        }
        finally
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }
    }

    private static List<ViewModel> MapRawViewsToModel(List<RawPostgresViewModel> rawList)
    {
        return rawList.Select(v => new ViewModel
        {
            SchemaName = v.SchemaName,
            ViewName = v.ViewName,
            Definition = v.Definition,
            CreateScript = $"CREATE OR REPLACE VIEW \"{v.SchemaName}\".\"{v.ViewName}\" AS {v.Definition}"
        }).ToList();
    }
}