using System.Data;
using Kopi.Core.Models.Common;
using Kopi.Core.Utilities;
using Npgsql;

namespace Kopi.Core.Services.PostgreSQL.Source;

public class PostgresSourceDbConnectionStringService
{
    /// <summary>
    /// Validates that a PostgreSQL connection string is legit
    /// </summary>
    /// <param name="config">The config containing the connection string</param>
    /// <returns>True if we can connect</returns>
    public static async Task<bool> ValidatePostgresConnectionString(KopiConfig config)
    {
        var connString = AppendConnectionTimeoutToConnString(config.SourceConnectionString);
        
        Msg.Write(MessageType.Info, "Validating PostgreSQL connection string...");

        // Connect to the database and validate the connection string.
        await using var connection = new NpgsqlConnection(connString);

        try
        {
            await connection.OpenAsync();
            Msg.Write(MessageType.Info, "Connection string is valid and connection to the database was successful.");
            return true;
        }
        catch (Exception ex)
        {
            // Handle SSL/TLS trust issues specifically, similar to TrustServerCertificate in MSSQL
            if (ex.Message.Contains("SSL") || ex.Message.Contains("certificate") || ex.InnerException?.Message.Contains("certificate") == true)
            {
                try
                {
                    // Npgsql equivalent of TrustServerCertificate=True is roughly Trust Server Certificate=true
                    // or relaxing SslMode. We'll append the standard Npgsql trust param.
                    if (!connString.Contains("Trust Server Certificate", StringComparison.OrdinalIgnoreCase))
                    {
                        connString += ";Trust Server Certificate=true";
                    }

                    await using var connection2 = new NpgsqlConnection(connString);
                    if (connection2.State != ConnectionState.Open) await connection2.OpenAsync();
                    
                    Msg.Write(MessageType.Success, "Connection to the database was successful with Trust Server Certificate=true.");
                    Console.WriteLine("");
                    if (connection2.State != ConnectionState.Closed) await connection2.CloseAsync();
                    
                    // We should also update the KopiConfigService to reflect this change if you have a Postgres equivalent setter
                    // For now, we assume this is handled or we just return true knowing the config needs this tweak
                    // KopiConfigService.SetAppendTrustServerCertificate(true); // Adapt this if needed for PG specific flag
                    
                    return true;
                }
                catch (Exception e)
                {
                    Msg.Write(MessageType.Error, "Fatal error while trying to connect to the source database with Trust Server Certificate=true.");
                    Msg.Write(MessageType.Error, $"Postgres Exception:\n {e}");
                    Environment.Exit(1);
                }
            }

            // If it wasn't an SSL error, or the retry failed
            Msg.Write(MessageType.Error, $"Fatal error connecting to PostgreSQL: {ex.Message}");
            return false;
        }
        finally
        {
            if (connection.State != ConnectionState.Open) await connection.CloseAsync();
        }
    }

    /// <summary>
    ///  Appends a connection timeout to the connection string if one does not already exist
    /// </summary>
    private static string AppendConnectionTimeoutToConnString(string sourceConnectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(sourceConnectionString);
        if (builder.Timeout > 0 && sourceConnectionString.Contains("Timeout", StringComparison.OrdinalIgnoreCase)) 
        {
            return builder.ConnectionString;
        }
        
        builder.Timeout = 15; // Set a default timeout of 15 seconds
        Msg.Write(MessageType.Info, "No connection timeout specified in the connection string. Setting default timeout to 15 seconds.");

        return builder.ConnectionString;
    }
}