using System.Data;
using Dapper;
using Kopi.Core.Models;
using Kopi.Core.Models.Common;
using Kopi.Core.Utilities;
using Microsoft.Data.SqlClient;

namespace Kopi.Core.Services.SQLServer.Target;

public static class ScriptExecutionService
{
    private static int _retryCount = 0;
    private const int MAX_RETRIES = 20;
    private const int RETRY_TIME_MS = 1000;

    /// <summary>
    ///  Executes the provided SQL script against the target database with retry logic for specific connection issues.
    /// </summary>
    /// <param name="sqlScript">The SQL script we're executing</param>
    /// <param name="targetDbConnectionString">The connection string</param>
    /// <param name="isRetry">True if this is a retry</param>
    /// <returns>True if it worked ok</returns>
    public static async Task<bool> ExecuteSqlScript(string sqlScript, string targetDbConnectionString, bool isRetry = false)
    {
        if (isRetry)
        {
            Msg.Write(MessageType.Warning, $"Waiting for database: {_retryCount} of {MAX_RETRIES} tries...");
        }
        else
        {
            //Reset retry count for new execution
            _retryCount = 0;
        }
        
        using IDbConnection conn = new SqlConnection(targetDbConnectionString);
        try
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            await conn.ExecuteAsync(sqlScript);
            
            //Message if successful after retries
            if (_retryCount > 0) Msg.Write(MessageType.Info, "Database is up now.");
            
            return true;
        }
        catch (SqlException ex)
        {
            if (ex.Message.Contains("an existing connection was forcibly closed by the remote host", StringComparison.CurrentCultureIgnoreCase))
            {
                if (_retryCount < MAX_RETRIES)
                {
                    _retryCount++;
                    await Task.Delay(RETRY_TIME_MS);
                    return await ExecuteSqlScript(sqlScript, targetDbConnectionString, true);
                }
                else
                {
                    Msg.Write(MessageType.Error, "Database not up after multiple retries. Exiting.");
                    return false;
                }
            }
            else
            {
                Msg.Write(MessageType.Error, $"SQL Error: {ex.Message}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Msg.Write(MessageType.Error, $"Error: {ex.Message}");
            return false;
        }
        finally
        {
            if (conn.State == ConnectionState.Open) conn.Close();
        }
    }

    /// <summary>
    /// Generates the target database connection string based on the provided configuration and SQL Server details.
    /// </summary>
    /// <param name="config">The user-supplied config file</param>
    /// <param name="sqlPort">The target SQL Port</param>
    /// <param name="sqlPassword">The target SQL Password</param>
    /// <param name="isDbCreation">If true, then target the Master database</param>
    /// <returns></returns>
    public static string GenerateTargetDbConnectionString(KopiConfig config, int sqlPort, string sqlPassword,
        bool isDbCreation = false)
    {
        const string server = "localhost";
        var database = isDbCreation ? "master" : DatabaseHelper.GetDatabaseName(config.SourceConnectionString, DatabaseType.SqlServer);
        const string userId = "sa";
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{server},{sqlPort}",
            InitialCatalog = database,
            UserID = userId,
            Password = sqlPassword,
            ConnectTimeout = 30,
            Encrypt = false,
            TrustServerCertificate = true,
            ApplicationIntent = ApplicationIntent.ReadWrite,
            MultiSubnetFailover = false
        };

        return builder.ConnectionString;
    }
}