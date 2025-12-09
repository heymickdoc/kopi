namespace Kopi.Core.Interfaces;

public interface IEfMigrationHistoryService
{
    Task CopyMigrationHistoryAsync(string targetConnectionString);
}