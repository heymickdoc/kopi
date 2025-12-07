using System.Data;
using Kopi.Core.Models.Common;
using Kopi.Core.Services.Common;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Kopi.Core.Utilities;

public static class DatabaseHelper
{
    /// <summary>
    ///  Checks if the provided password meets SQL Server complexity requirements.
    /// </summary>
    /// <param name="password">The user-supplied password</param>
    /// <returns>True if it's ok</returns>
    public static bool IsSqlServerPasswordComplexEnough(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            return false;

        bool hasUpper = false, hasLower = false, hasDigit = false, hasSpecial = false;

        foreach (char c in password)
        {
            if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsLower(c)) hasLower = true;
            else if (char.IsDigit(c)) hasDigit = true;
            else hasSpecial = true;
        }

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }
    
    /// <summary>
    /// Extracts the database name from a connection string.
    /// </summary>
    /// <param name="connectionString">The connection string from the config file</param>
    /// <param name="dbType">The database type</param>
    /// <returns>The name of the database in the connection string</returns>
    public static string GetDatabaseName(string connectionString, DatabaseType dbType)
    {
        var databaseName = "unknown_db";
        
        try
        {
            switch (dbType)
            {
                case DatabaseType.SqlServer:
                    var sqlServerConnStringBuilder = new SqlConnectionStringBuilder(connectionString);
                    databaseName = sqlServerConnStringBuilder.InitialCatalog;
                    break;
                case DatabaseType.PostgreSQL:
                    var postgresConnStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
                    databaseName = postgresConnStringBuilder.Database;
                    break;
                case DatabaseType.Unknown:
                    Msg.Write(MessageType.Error, "Cannot extract database name from unknown database type.");
                    break;
                default:
                    Msg.Write(MessageType.Error, "Unsupported database type for extracting database name.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Msg.Write(MessageType.Error, $"Error parsing connection string. Defaulting to '{databaseName}'.");
            Msg.Write(MessageType.Error, ex.Message);

            return databaseName;
        }
        
        return databaseName ?? "unknown_db";
    }


    /// <summary>
    /// Generates a semi-random password for the database user. It doesn't have to be super secure as it's only used locally
    /// and can be regenerated if needed.
    /// </summary>
    /// <returns>A rather lengthy string with the password for your new DB</returns>
    public static async Task<string> GenerateDbPassword()
    {
        var nouns = await File.ReadAllLinesAsync("SystemData/nouns.txt");
        var adjectives = await File.ReadAllLinesAsync("SystemData/adjectives.txt");

        //Generate a list of special chars that can join the words
        var specialChars = new List<string> { "!", "-", "_", "+", "=", ".", "~" };

        //Generate a random special char
        var specialChar1 = specialChars[Random.Shared.Next(specialChars.Count)];
        var specialChar2 = specialChars[Random.Shared.Next(specialChars.Count)];
        var specialChar3 = specialChars[Random.Shared.Next(specialChars.Count)];

        var word1 = nouns[Random.Shared.Next(nouns.Length)];
        var word2 = nouns[Random.Shared.Next(nouns.Length)];
        var word3 = nouns[Random.Shared.Next(nouns.Length)];
        var adjective1 = adjectives[Random.Shared.Next(adjectives.Length)];

        var password = $"{adjective1}{specialChar1}{word1}{specialChar2}{specialChar3}{word2}{word3}";

        return password;
    }

    /// <summary>
    /// Gets the database type from the connection string.
    /// </summary>
    /// <param name="connectionString">The connection string supplied in the config file</param>
    /// <returns><see cref="DatabaseType"/>The detected database type</returns>
    public static async Task<DatabaseType> GetDatabaseType(string connectionString)
    {
        var looksLikeSqlServer = IsSqlServerConnectionString(connectionString);
        var looksLikePostgres = IsPostgresConnectionString(connectionString);
        
        if (looksLikeSqlServer && !looksLikePostgres) return DatabaseType.SqlServer;
        if (looksLikePostgres && !looksLikeSqlServer) return DatabaseType.PostgreSQL;

        if (looksLikeSqlServer && looksLikePostgres)
        {
            Msg.Write(MessageType.Info, "Connection string format is ambiguous. Probing server to detect type...");

            // Try SQL Server first (most likely for Kopi users?)
            if (TryConnectSqlServer(connectionString)) return DatabaseType.SqlServer;
            
            // Try Postgres next
            if (TryConnectPostgres(connectionString)) return DatabaseType.PostgreSQL;
        }

        // 3. Fallback / Failure
        Msg.Write(MessageType.Warning, "Could not determine database type (Connection failed or format invalid). .");
        return DatabaseType.Unknown;
    }

    private static bool IsSqlServerConnectionString(string connectionString)
    {
        try { _ = new SqlConnectionStringBuilder(connectionString); return true; }
        catch { return false; }
    }

    private static bool IsPostgresConnectionString(string connectionString)
    {
        try { _ = new NpgsqlConnectionStringBuilder(connectionString); return true; }
        catch { return false; }
    }

    private static bool TryConnectSqlServer(string originalConnString)
    {
        var builder = new SqlConnectionStringBuilder(originalConnString) { ConnectTimeout = 5 };
        using IDbConnection conn = new SqlConnection(builder.ConnectionString);
        
        try
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            return true;
        }
        catch (Exception ex)
        {
            Msg.Write(MessageType.Error, $"SQL Server connection attempt failed: {ex.Message}");
            return false;
        }
        finally
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }
    }

    private static bool TryConnectPostgres(string originalConnString)
    {
        var builder = new NpgsqlConnectionStringBuilder(originalConnString) { Timeout = 5 };
        using IDbConnection conn = new NpgsqlConnection(builder.ConnectionString);
        
        try
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            return true;
        }
        catch (Exception ex)
        {
            Msg.Write(MessageType.Error, $"PostgreSQL connection attempt failed: {ex.Message}");
            return false;
        }
        finally
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }
    }
}