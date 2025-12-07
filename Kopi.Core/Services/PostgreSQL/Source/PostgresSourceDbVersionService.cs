using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Npgsql;

namespace Kopi.Core.Services.PostgreSQL.Source;

public class PostgresSourceDbVersionService
{
    public static async Task<string> GetDatabaseVersion(KopiConfig config)
    {
        using IDbConnection conn = new NpgsqlConnection(config.SourceConnectionString);
        if (conn.State != ConnectionState.Open) conn.Open();
        // "PostgreSQL 16.1 on x86_64..."
        return await conn.ExecuteScalarAsync<string>("SELECT version();");
    }
}