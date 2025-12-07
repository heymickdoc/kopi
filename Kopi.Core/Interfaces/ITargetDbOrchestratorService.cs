namespace Kopi.Core.Interfaces;

public interface ITargetDbOrchestratorService
{
    /// <summary>
    /// Creates the target database and tables. Returns the connection string.
    /// </summary>
    Task<string> PrepareTargetDb();

    /// <summary>
    /// Creates Views, Stored Procedures, and Functions after data load.
    /// </summary>
    Task CreateDatabaseProgrammability(string connectionString);
}