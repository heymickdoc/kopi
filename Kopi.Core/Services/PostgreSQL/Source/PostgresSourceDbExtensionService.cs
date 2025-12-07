using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Utilities;
using Npgsql;

namespace Kopi.Core.Services.PostgreSQL.Source;

public class PostgresSourceDbExtensionService
{
    public static async Task<List<DatabaseExtensionModel>> GetExtensions(KopiConfig config)
    {
        // We filter out 'plpgsql' because it's installed by default in modern Postgres
        const string sql = @"
            SELECT 
                extname AS Name, 
                extversion AS Version, 
                n.nspname AS SchemaName
            FROM pg_extension e
            JOIN pg_namespace n ON e.extnamespace = n.oid
            WHERE extname != 'plpgsql';";

        using IDbConnection conn = new NpgsqlConnection(config.SourceConnectionString);
        try
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var data = await conn.QueryAsync<DatabaseExtensionModel>(sql);
            return data.ToList();
        }
        catch (Exception ex)
        {
            Msg.Write(MessageType.Error, $"Error reading extensions: {ex.Message}");
            return [];
        }
        finally
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }
    }
}