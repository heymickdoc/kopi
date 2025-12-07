using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Utilities;
using Microsoft.Data.SqlClient;

namespace Kopi.Core.Services.SQLServer.Source;

public class SqlServerSourceDbVersionService
{
    /// <summary>
    /// Gets the version of the source database
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public static async Task<string> GetVersion(KopiConfig config)
    {

        try
        {
            var version = await GetSqlServerVersion(config);
            Msg.Write(MessageType.Info, $"Source database version: {version}");
            return version;
        }
        catch (SqlException ex)
        {
            Msg.Write(MessageType.Error, $"SQL Exception while reading source database version: {ex.Message}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Msg.Write(MessageType.Error, $"Unexpected error while reading source database version: {ex.Message}");
            Environment.Exit(1);
        }

        return "Unknown";
    }

    /// <summary>
    /// Gets the SQL Server version from the source database
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    private static async Task<string> GetSqlServerVersion(KopiConfig config)
    {
        const string sql = "SELECT SERVERPROPERTY('ProductVersion')";

        using IDbConnection conn = new SqlConnection(config.SourceConnectionString);

        try
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var productVersion = await conn.ExecuteScalarAsync<string>(sql) ?? "SQL Server 2017";

            // Extract major version from product version (e.g., "14.0.1000" -> 14)
            var versionParts = productVersion.Split('.');
            var majorVersion = int.Parse(versionParts[0]);

            // Map to SQL Server version names
            var version = majorVersion switch
            {
                17 => "SQL Server 2025",
                16 => "SQL Server 2022",
                15 => "SQL Server 2019",
                14 => "SQL Server 2017",
                13 => "SQL Server 2016",
                12 => "SQL Server 2014",
                11 => "SQL Server 2012",
                10 => "SQL Server 2008",
                9 => "SQL Server 2005",
                8 => "SQL Server 2000",
                _ => $"Unknown Version ({majorVersion})"
            };

            Msg.Write(MessageType.Success, $"Source SQL Server version: {version} (Product Version: {productVersion})");
            Console.WriteLine("");
            return version;
        }
        catch (SqlException ex)
        {
            Msg.Write(MessageType.Warning, $"Cannot get SQL Server version. Defaulting to SQL Server 2017.\r\nSQL Error: {ex.Message}");
            return "SQL Server 2017";
        }
        finally
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }
    }
}