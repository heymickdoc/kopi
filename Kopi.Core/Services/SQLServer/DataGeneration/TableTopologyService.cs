using Kopi.Core.Models;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.SQLServer.DataGeneration;

/// <summary>
/// Handles the generation of table topology based on foreign key relationships to ensure correct data insertion order.
/// </summary>
public static class TableTopologyService
{
    /// <summary>
    /// Generates a list of tables in the correct order for data insertion, based on foreign key relationships.
    /// This ensures that parent tables are always processed before their child tables.
    /// </summary>
    /// <param name="config"></param>
    /// <param name="sourceDbData"></param>
    /// <returns></returns>
    public static List<TableModel> GenerateOrderedTableTopology(KopiConfig config, SourceDbModel sourceDbData)
    {
        // 1. Get initial tables from config
        var initialTables = GenerateConfigTablesList(config);

        // 2. Build complete dependency set (find ALL required tables)
        var allRequiredTables = GenerateAllRequiredTables(sourceDbData, initialTables);

        // 3. Convert to TableModel objects for easier processing
        var tableModels = ConvertTableListToTableModels(sourceDbData, allRequiredTables);

        // 4. Topological sort to get correct insertion order
        var orderedTables = TopologicalSort(tableModels, sourceDbData.Relationships);

        return orderedTables;
    }
    
    /// <summary>
    /// Generates a set of tables specified in the config file.
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    private static HashSet<string> GenerateConfigTablesList(KopiConfig config)
    {
        var initialTables = new HashSet<string>();
        foreach (var table in config.Tables)
        {
            var tableSplit = table.Split('.');
            if (tableSplit.Length != 2)
            {
                Msg.Write(MessageType.Error,
                    $"Invalid table format in config file: {table}. Expected format: schema.table");
                Environment.Exit(1);
            }
            initialTables.Add($"{tableSplit[0]}.{tableSplit[1]}");
        }

        return initialTables;
    }
    
    /// <summary>
    /// Generates the full set of required tables by traversing foreign key relationships.
    /// Starts with the initial set of tables and adds any parent tables they reference.
    /// </summary>
    /// <param name="sourceDbData"></param>
    /// <param name="initialTables"></param>
    /// <returns></returns>
    private static HashSet<string> GenerateAllRequiredTables(SourceDbModel sourceDbData, HashSet<string> initialTables)
    {
        var allRequiredTables = new HashSet<string>(initialTables);
        var queue = new Queue<string>(initialTables);

        while (queue.Count > 0)
        {
            var currentTable = queue.Dequeue();
            var parts = currentTable.Split('.');
            var schemaName = parts[0];
            var tableName = parts[1];

            // Find all relationships where THIS table is the child (references parents)
            var relationshipsAsChild = sourceDbData.Relationships
                .Where(r => r.ParentSchema == schemaName && r.ParentTable == tableName)
                .ToList();

            foreach (var relationship in relationshipsAsChild)
            {
                var parentTable = $"{relationship.ReferencedSchema}.{relationship.ReferencedTable}";
                
                // Only add if it's not already in our required set
                if (!allRequiredTables.Contains(parentTable))
                {
                    allRequiredTables.Add(parentTable);
                    queue.Enqueue(parentTable); // Check this parent for its own parents
                }
            }
        }

        return allRequiredTables;
    }

    /// <summary>
    /// Converts a set of table names (schema.table) into their corresponding TableModel objects.
    /// </summary>
    /// <param name="sourceDbData"></param>
    /// <param name="allRequiredTables"></param>
    /// <returns></returns>
    private static List<TableModel> ConvertTableListToTableModels(SourceDbModel sourceDbData, HashSet<string> allRequiredTables)
    {
        var tableModels = new List<TableModel>();
        foreach (var tableKey in allRequiredTables)
        {
            var parts = tableKey.Split('.');
            var schemaName = parts[0];
            var tableName = parts[1];
            
            var tableModel = sourceDbData.Tables.FirstOrDefault(t => 
                t.SchemaName == schemaName && t.TableName == tableName);
                
            if (tableModel != null)
                tableModels.Add(tableModel);
        }

        return tableModels;
    }

    

    

    private static List<TableModel> TopologicalSort(List<TableModel> tables, List<RelationshipModel> relationships)
    {
        // Build dependency graph: child -> list of parents
        var dependencies = new Dictionary<string, HashSet<string>>();
        var tableLookup = tables.ToDictionary(t => $"{t.SchemaName}.{t.TableName}");

        // Initialize dependencies for all tables
        foreach (var table in tables)
        {
            var tableKey = $"{table.SchemaName}.{table.TableName}";
            dependencies[tableKey] = new HashSet<string>();
        }

        // Find dependencies (what each table depends on)
        foreach (var table in tables)
        {
            var tableKey = $"{table.SchemaName}.{table.TableName}";
            
            // Find all foreign keys for this table
            var foreignKeys = relationships
                .Where(r => r.ParentSchema == table.SchemaName && r.ParentTable == table.TableName)
                .ToList();

            foreach (var fk in foreignKeys)
            {
                var parentTableKey = $"{fk.ReferencedSchema}.{fk.ReferencedTable}";
                
                // Only add dependency if the parent table is in our list
                if (tableLookup.ContainsKey(parentTableKey))
                {
                    dependencies[tableKey].Add(parentTableKey);
                }
            }
        }

        // Kahn's algorithm for topological sort
        var result = new List<TableModel>();
        var noDependencies = dependencies
            .Where(kvp => kvp.Value.Count == 0)
            .Select(kvp => kvp.Key)
            .ToList();

        while (noDependencies.Count > 0)
        {
            var tableKey = noDependencies.First();
            noDependencies.RemoveAt(0);
            
            result.Add(tableLookup[tableKey]);

            // Remove this table as a dependency from all other tables
            foreach (var kvp in dependencies)
            {
                if (kvp.Value.Contains(tableKey))
                {
                    kvp.Value.Remove(tableKey);
                    if (kvp.Value.Count == 0)
                    {
                        noDependencies.Add(kvp.Key);
                    }
                }
            }
        }

        // Add any remaining tables (shouldn't happen in a valid acyclic graph)
        var remaining = tables.Except(result).ToList();
        result.AddRange(remaining);

        return result;
    }
}