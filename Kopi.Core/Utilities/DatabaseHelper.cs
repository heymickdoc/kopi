using System.Data;
using Kopi.Core.Models.Common;
using Kopi.Core.Services.Common;
using Microsoft.Data.SqlClient;

namespace Kopi.Core.Utilities;

public static class DatabaseHelper
{
    /// <summary>
    ///  Checks if the provided password meets SQL Server complexity requirements.
    /// </summary>
    /// <param name="password">The user-supplied password</param>
    /// <returns>True if it's ok</returns>
    public static bool IsPasswordComplexEnough(string password)
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

        switch (dbType)
        {
            case DatabaseType.SqlServer:
                //I need to look for "Database=" or "Initial Catalog=" or "AttachDbFileName=" and extract the value after it.
                var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (part.StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                    {
                        databaseName = part.Substring("Database=".Length);
                        break;
                    }

                    if (part.StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                    {
                        databaseName = part.Substring("Initial Catalog=".Length);
                        break;
                    }

                    if (part.StartsWith("AttachDbFileName=", StringComparison.OrdinalIgnoreCase))
                    {
                        var filePath = part.Substring("AttachDbFileName=".Length);
                        databaseName = Path.GetFileNameWithoutExtension(filePath);
                        break;
                    }
                }
                break;
            case DatabaseType.PostgreSQL:
                throw new NotImplementedException("PostgreSQL database name extraction not implemented yet.");
            case DatabaseType.Unknown:
                Msg.Write(MessageType.Error, "Cannot extract database name from unknown database type.");
                break;
            default:
                Msg.Write(MessageType.Error, "Unsupported database type for extracting database name.");
                break;
        }

        return databaseName;
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
    /// <returns><see cref="DatabaseType"/></returns>
    public static DatabaseType GetDatabaseType(string connectionString)
    {
        //A very naive way to determine the database type from the connection string.
        if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Database=", StringComparison.OrdinalIgnoreCase))
        {
            Msg.Write(MessageType.Success, "Detected Microsoft SQL Server.");
            return DatabaseType.SqlServer;
        }

        if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Port=", StringComparison.OrdinalIgnoreCase))
        {
            Msg.Write(MessageType.Success, "Detected PostgreSQL.");
            return DatabaseType.PostgreSQL;
        }

        //TODO: Show a menu to select the database type if it can't be determined. A message is fine for now.
        Msg.Write(MessageType.Warning,
            "Could not determine database type from connection string. Defaulting to SQL Server.");

        //Default to SQL Server for now.
        return DatabaseType.SqlServer;
    }
}