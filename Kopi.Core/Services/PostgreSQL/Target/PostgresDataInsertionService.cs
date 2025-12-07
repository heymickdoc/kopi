using Kopi.Core.Interfaces;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using Npgsql;

namespace Kopi.Core.Services.PostgreSQL.Target;

public class PostgresDataInsertionService : IDataInsertionService
{
    public async Task InsertData(
        KopiConfig config, 
        SourceDbModel sourceDb, 
        List<TargetDataModel> generatedData, 
        string targetConnectionString)
    {
        await using var conn = new NpgsqlConnection(targetConnectionString);
        await conn.OpenAsync();

        // 1. Disable Constraints (Foreign Keys & Triggers)
        // Setting session_replication_role to 'replica' prevents FK checks and triggers from firing.
        // This is essential for bulk loading generated data that may be out of order.
        // Note: This requires the connecting user to have appropriate privileges (usually superuser/admin).
        Msg.Write(MessageType.Info, "Disabling constraints for bulk insert (session_replication_role = 'replica')...");
        await using (var cmd = new NpgsqlCommand("SET session_replication_role = 'replica';", conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        try
        {
            foreach (var table in generatedData)
            {
                if (table.Rows.Count == 0) continue;

                var schema = table.SchemaName;
                var tableName = table.TableName;
                
                // Quote identifiers to handle case sensitivity and keywords in Postgres
                var fullTableName = $"\"{schema}\".\"{tableName}\"";

                // We grab column names from the first row to build the COPY signature
                var firstRow = table.Rows[0];
                var columnNames = firstRow.Columns.Select(c => $"\"{c.ColumnName}\"");
                
                // Construct the COPY command (BINARY format is faster and safer for types)
                var copyCommand = $"COPY {fullTableName} ({string.Join(", ", columnNames)}) FROM STDIN (FORMAT BINARY)";

                Msg.Write(MessageType.Info, $"Inserting {table.Rows.Count} rows into {fullTableName}...");

                // 2. Perform Binary Import
                // This opens a stream directly to the server
                await using var writer = await conn.BeginBinaryImportAsync(copyCommand);

                foreach (var row in table.Rows)
                {
                    await writer.StartRowAsync();

                    foreach (var col in row.Columns)
                    {
                        if (col.RawValue == null || col.RawValue == DBNull.Value)
                        {
                            await writer.WriteNullAsync();
                        }
                        else
                        {
                            // WriteAsync handles most CLR -> Postgres type mappings automatically.
                            // e.g. C# int -> PG integer, C# DateTime -> PG timestamp
                            await writer.WriteAsync(col.RawValue);
                        }
                    }
                }

                // Commit the stream for this table
                await writer.CompleteAsync();
            }
        }
        catch (Exception ex)
        {
            Msg.Write(MessageType.Error, $"PostgreSQL Data Insertion Failed: {ex.Message}");
            throw;
        }
        finally
        {
            // 3. Re-enable Constraints
            // We must reset this to 'origin' (default) so the database enforces integrity 
            // for normal operations after we disconnect.
            Msg.Write(MessageType.Info, "Re-enabling constraints (session_replication_role = 'origin')...");
            try 
            {
                await using (var cmd = new NpgsqlCommand("SET session_replication_role = 'origin';", conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            } 
            catch (Exception ex)
            {
                Msg.Write(MessageType.Warning, $"Failed to re-enable constraints: {ex.Message}");
            }
        }
    }
}