using System.Data;
using Kopi.Core.Models.Common;
using Kopi.Core.Services.Common;
using Kopi.Core.Utilities;
using Microsoft.Data.SqlClient;

namespace Kopi.Core.Services.SQLServer.Source;

public static class SqlServerSourceDbConnectionStringService
{
    /// <summary>
    /// Validates that a SQL Server connection string is legit
    /// </summary>
    /// <param name="sourceConnectionString">The connection string to validate</param>
    /// <returns>True if we can connect</returns>
    public static async Task<bool> ValidateSqlServerConnectionString(KopiConfig config)
    {
        var connString = AppendConnectionTimeoutToConnString(config.SourceConnectionString);
        
        Msg.Write(MessageType.Info,"Validating SQL Server connection string...");

        //Connect to the database and validate the connection string.
        await using var connection = new SqlConnection(connString);

        try
        {
            await connection.OpenAsync();
            Msg.Write(MessageType.Info,"Connection string is valid and connection to the database was successful.");
            return true;
        }
        catch (Exception ex)
        {
            try
            {
                connString += ";TrustServerCertificate=True";
                await using var connection2 = new SqlConnection(connString);
                if (connection2.State != ConnectionState.Open) await connection2.OpenAsync();
                Msg.Write(MessageType.Success,"Connection to the database was successful with TrustServerCertificate=True.");
                Console.WriteLine("");
                if (connection2.State != ConnectionState.Closed) await connection2.CloseAsync();
                
                //We should also update the KopiConfigService to reflect this change.
                KopiConfigService.SetAppendTrustServerCertificate(true);
                
                return true;
            }
            catch (Exception e)
            {
                Msg.Write(MessageType.Error, "Fatal error while trying to connect to the source database with TrustServerCertificate=True.");
                Msg.Write(MessageType.Error, $"SQL Exception:\n {e}");
                Environment.Exit(1);
            }
        }
        finally
        {
            if (connection.State != ConnectionState.Open) await connection.CloseAsync();
        }
        
        return false;
    }

    /// <summary>
    ///  Appends a connection timeout to the connection string if one does not already exist
    /// </summary>
    /// <param name="sourceConnectionString"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static string AppendConnectionTimeoutToConnString(string sourceConnectionString)
    {
        var builder = new SqlConnectionStringBuilder(sourceConnectionString);
        if (sourceConnectionString.Contains("connect timeout", StringComparison.CurrentCultureIgnoreCase)) return builder.ConnectionString;
        
        builder.ConnectTimeout = 15; //Set a default timeout of 15 seconds... we don't want to wait too long just to find out the connection string is invalid.
        Msg.Write(MessageType.Info, "No connection timeout specified in the connection string. Setting default timeout to 15 seconds.");

        return builder.ConnectionString;
    }
}