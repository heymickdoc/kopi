using System.Text;
using Bogus;
using Kopi.Core.Models.Common;
using Kopi.Core.Utilities;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Common.DataGeneration.Generators;
using Kopi.Core.Services.SQLServer.DataGeneration;
using System.Collections.Generic;
using System.Linq;

namespace Kopi.Core.Services.Common;

/// <summary>
///  Orchestrates the data generation process across all tables.
/// </summary>
public class DataOrchestratorService(
    KopiConfig config,
    SourceDbModel sourceDbData,
    DataGeneratorService generatorService)
{
    private const StringComparison SC = StringComparison.OrdinalIgnoreCase;

    // --- Internal Model Classes ---
    private class ForeignKeyInfo
    {
        public string ReferencedSchema { get; init; } = "";
        public string ReferencedTable { get; set; } = "";
        public string ReferencedColumnName { get; set; } = "";
    }

    private class CompositeForeignKey
    {
        public string ConstraintName { get; init; } = "";
        public string ReferencedSchema { get; init; } = "";
        public string ReferencedTable { get; init; } = "";
        public List<(string ParentColumn, string ReferencedColumn)> Columns { get; init; } = new();
    }
    // --- End Internal Model Classes ---

    private readonly Faker _faker = new();
    private readonly List<TargetDataModel> _generatedData = new();

    public async Task<List<TargetDataModel>> OrchestrateDataGeneration()
    {
        var orderedTables = TableTopologyService.GenerateOrderedTableTopology(config, sourceDbData);
        await GenerateData(orderedTables);
        if (_generatedData.Count != 0) return _generatedData;
        Msg.Write(MessageType.Warning, "No data was generated...");
        Environment.Exit(1);
        return _generatedData;
    }

    private async Task GenerateData(List<TableModel> orderedTables)
    {
        var configMaxRowCount = config.Settings.MaxRowCount > 0 ? config.Settings.MaxRowCount : 5;

        foreach (var table in orderedTables)
        {
            var effectiveMaxRows = GetEffectiveMaxRows(table, configMaxRowCount);
            if (effectiveMaxRows < configMaxRowCount)
            {
                Msg.Write(MessageType.Info,
                    $"Table {table.SchemaName}.{table.TableName} has unique constraints limiting rows to {effectiveMaxRows} (config was {configMaxRowCount}).");
            }

            var rows = GenerateDataForTable(table, effectiveMaxRows, sourceDbData.Relationships);

            var tableModel = new TargetDataModel
            {
                SchemaName = table.SchemaName,
                TableName = table.TableName,
                Rows = rows
            };
            _generatedData.Add(tableModel);
        }
    }

    /// <summary>
    /// This is your new framework, implemented with batch-first logic.
    /// It populates all data into batches, determines the final row count,
    /// and *then* assembles the rows.
    /// </summary>
    private List<RowData> GenerateDataForTable(TableModel table, int maxRowCount, List<RelationshipModel> relationships)
    {
        if (table.TableName.Equals("address", SC))
        {
            var debugVar = 1;
        }
        
        
        var processedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var generatedBatches = new Dictionary<string, List<object?>>(StringComparer.OrdinalIgnoreCase);
        int smallestBatch = maxRowCount; // The true number of rows we can generate

        // --- PASS 1: Composite Foreign Keys ---
        var compositeFks = FindCompositeForeignKeys(table, relationships);
        foreach (var cfk in compositeFks)
        {
            Msg.Write(MessageType.Debug,
                $"Processing Composite FK {cfk.ConstraintName} for {table.SchemaName}.{table.TableName}");
            var batch = GenerateCompositeFkBatch(cfk, smallestBatch);

            if (batch.Count < smallestBatch) smallestBatch = batch.Count;

            // De-interleave the composite batch into single-column batches
            foreach (var colName in cfk.Columns.Select(c => c.ParentColumn))
            {
                var columnBatch = batch.Select(dict => dict[colName]).ToList();
                generatedBatches[colName] = columnBatch;
                processedColumns.Add(colName);
            }
        }

        // --- PASS 2: Composite Primary Key (if not already handled) ---
        var primaryKey = GetCompositePrimaryKeys(table);
        if (primaryKey != null && !primaryKey.PrimaryKeyColumns.Any(processedColumns.Contains))
        {
            Msg.Write(MessageType.Debug, $"Processing Composite PK for {table.SchemaName}.{table.TableName}");
            var batch = GenerateCompositePkBatch(table, primaryKey, relationships, smallestBatch);

            if (batch.Count < smallestBatch) smallestBatch = batch.Count;

            foreach (var colName in primaryKey.PrimaryKeyColumns)
            {
                var columnBatch = batch.Select(dict => dict[colName]).ToList();
                generatedBatches[colName] = columnBatch;
                processedColumns.Add(colName);
            }
        }

        // --- PASS 2.5: Composite Unique Indexes (that are not PKs) ---
        // This fulfills the TODO.
        var uniqueCompositeIndexes = sourceDbData.Indexes
            .Where(idx => idx.SchemaName.Equals(table.SchemaName, SC) &&
                          idx.TableName.Equals(table.TableName, SC) &&
                          idx.IsUnique &&
                          !idx.IsPrimaryKey &&
                          idx.IndexColumns.Count > 1)
            .ToList();

        foreach (var uqIndex in uniqueCompositeIndexes)
        {
            var indexColumnNames = uqIndex.IndexColumns
                .OrderBy(c => c.KeyOrdinal)
                .Select(c => c.ColumnName)
                .ToList();

            // Follow existing pattern: if *any* column is already processed, skip this entire pass
            // and let it be handled by later, simpler passes (like Pass 4 or 5).
            if (indexColumnNames.Any(processedColumns.Contains))
            {
                Msg.Write(MessageType.Debug,
                    $"Skipping Composite UQ {uqIndex.IndexName} as one of its columns was already processed.");
                continue;
            }

            Msg.Write(MessageType.Debug,
                $"Processing Composite Unique Index {uqIndex.IndexName} for {table.SchemaName}.{table.TableName}");

            // Generate a unique batch for these columns
            var batch = GenerateCompositeUniqueIndexBatch(table, indexColumnNames, smallestBatch);

            if (batch.Count < smallestBatch) smallestBatch = batch.Count;

            // De-interleave the composite batch into single-column batches
            foreach (var colName in indexColumnNames)
            {
                var columnBatch = batch.Select(dict => dict[colName]).ToList();
                generatedBatches[colName] = columnBatch;
                processedColumns.Add(colName);
            }
        }

        // --- PASS 3: Identity Columns ---
        var identityColumn = table.Columns.FirstOrDefault(c => c.IsIdentity);
        if (identityColumn != null && !processedColumns.Contains(identityColumn.ColumnName))
        {
            var batch = GenerateIdentityBatch(identityColumn, smallestBatch);
            generatedBatches[identityColumn.ColumnName] = batch;
            processedColumns.Add(identityColumn.ColumnName);
        }

        // --- PASS 4: Single-Column Unique Constraints (PK, UQ, unique FK) ---
        foreach (var column in table.Columns.Where(c =>
                     !processedColumns.Contains(c.ColumnName) && IsColumnUnique(table, c)))
        {
            var batch = GenerateSingleUniqueBatch(column, table, relationships, smallestBatch);

            if (batch.Count < smallestBatch) smallestBatch = batch.Count;
            generatedBatches[column.ColumnName] = batch;
            processedColumns.Add(column.ColumnName);
        }

        // --- PASS 5: "Normal" Columns (non-unique FKs and regular data) ---
        foreach (var column in table.Columns.Where(c => !processedColumns.Contains(c.ColumnName) && !c.IsComputed))
        {
            var batch = GenerateRegularBatch(column, table, relationships, smallestBatch);
            generatedBatches[column.ColumnName] = batch;
            processedColumns.Add(column.ColumnName);
        }

        // --- FINAL ASSEMBLY ---
        if (smallestBatch < maxRowCount)
        {
            Msg.Write(MessageType.Info,
                $"Table {table.SchemaName}.{table.TableName} row count capped at {smallestBatch} due to unique constraints.");
        }

        var rowDataList = new List<RowData>(smallestBatch);

        for (int i = 0; i < smallestBatch; i++)
        {
            var row = new RowData();
            foreach (var column in table.Columns.Where(c => !c.IsComputed))
            {
                if (generatedBatches.TryGetValue(column.ColumnName, out var batch))
                {
                    if (i < batch.Count) // Safety check for batches that might be shorter
                    {
                        row.Columns.Add(new ColumnData(column.ColumnName, batch[i], column.DataType));
                    }
                    else // This column's batch was shorter than the final smallestBatch, insert null
                    {
                        row.Columns.Add(new ColumnData(column.ColumnName, null, column.DataType));
                    }
                }
            }

            rowDataList.Add(row);
        }

        return rowDataList;
    }


    /// <summary>
    /// Finds all *distinct* composite foreign keys on a table.
    /// </summary>
    private List<CompositeForeignKey> FindCompositeForeignKeys(TableModel table, List<RelationshipModel> relationships)
    {
        var compositeFkGroups = relationships
            .Where(r => r.ParentSchema.Equals(table.SchemaName, SC) &&
                        r.ParentTable.Equals(table.TableName, SC) &&
                        r.ForeignKeyColumns.Count > 1) // Only composite
            .GroupBy(r => r.ForeignKeyName)
            .Select(g => g.First()); // Get one representative RelationshipModel for each constraint name

        var compositeFks = new List<CompositeForeignKey>();
        foreach (var rel in compositeFkGroups)
        {
            var columns = rel.ForeignKeyColumns
                .OrderBy(c => c.KeyOrdinal)
                .Select(c => (c.ParentColumnName, c.ReferencedColumnName))
                .ToList();

            var compositeFk = new CompositeForeignKey
            {
                ConstraintName = rel.ForeignKeyName,
                ReferencedSchema = rel.ReferencedSchema,
                ReferencedTable = rel.ReferencedTable,
                Columns = columns
            };
            compositeFks.Add(compositeFk);
        }

        return compositeFks;
    }

    /// <summary>
    /// Gets a batch of *actual, valid combinations* from the parent table (T2).
    /// </summary>
    private List<Dictionary<string, object?>> GenerateCompositeFkBatch(CompositeForeignKey cfk, int maxRowCount)
    {
        var parentTable = _generatedData.FirstOrDefault(t =>
            t.SchemaName.Equals(cfk.ReferencedSchema, SC) &&
            t.TableName.Equals(cfk.ReferencedTable, SC));

        if (parentTable == null || !parentTable.Rows.Any())
        {
            Msg.Write(MessageType.Warning,
                $"Parent table {cfk.ReferencedSchema}.{cfk.ReferencedTable} for composite FK {cfk.ConstraintName} has no data. 0 rows will be generated.");
            return new List<Dictionary<string, object?>>();
        }

        var validCombinations = new List<Dictionary<string, object?>>();
        var seenCombinations = new HashSet<string>();
        var sb = new StringBuilder();

        // Extract all valid, unique combinations from the parent table
        foreach (var row in parentTable.Rows)
        {
            sb.Clear();
            var combination = new Dictionary<string, object?>(StringComparer.CurrentCultureIgnoreCase);
            bool allFound = true;

            foreach (var (parentCol, referencedCol) in cfk.Columns)
            {
                var parentColData = row.Columns.FirstOrDefault(c => c.ColumnName.Equals(referencedCol, SC));

                if (parentColData == null)
                {
                    allFound = false;
                    break;
                }

                combination[parentCol] = parentColData.RawValue;
                sb.Append(parentColData.RawValue?.ToString() ?? "NULL").Append('-');
            }

            if (allFound && seenCombinations.Add(sb.ToString()))
            {
                validCombinations.Add(combination);
            }
        }

        if (!validCombinations.Any())
        {
            Msg.Write(MessageType.Warning,
                $"Parent table {cfk.ReferencedSchema}.{cfk.ReferencedTable} has data, but no valid composite key combinations were found for {cfk.ConstraintName}.");
            return new List<Dictionary<string, object?>>();
        }

        // Shuffle and take the amount we need
        return validCombinations.OrderBy(x => _faker.Random.Int()).Take(maxRowCount).ToList();
    }


    /// <summary>
    /// Finds the composite primary key for a table, if one exists.
    /// </summary>
    private PrimaryKeyModel? GetCompositePrimaryKeys(TableModel table)
    {
        return sourceDbData.PrimaryKeys
            .FirstOrDefault(pk => pk.SchemaName.Equals(table.SchemaName, SC) &&
                                  pk.TableName.Equals(table.TableName, SC) &&
                                  pk.PrimaryKeyColumns.Count > 1);
    }

    /// <summary>
    /// Generates a batch of unique composite *primary* keys.
    /// UPDATED: Now checks if columns are Foreign Keys and pulls parent data if so.
    /// </summary>
    private List<Dictionary<string, object?>> GenerateCompositePkBatch(
        TableModel table, PrimaryKeyModel primaryKey, List<RelationshipModel> relationships, int maxRowCount)
    {
        var keyColumnValuePools = new Dictionary<string, List<object?>>();
        long totalPossibleCombinations = 1;

        // 1. Get the pool of unique values for each column in the composite key
        foreach (var pkColName in primaryKey.PrimaryKeyColumns)
        {
            var column = table.Columns.First(c => c.ColumnName.Equals(pkColName, SC));
            List<object?> values;

            // --- FIX START ---
            // Check if this part of the Composite PK is actually a Foreign Key
            var fkInfo = FindForeignKeyColumn(table.SchemaName, table.TableName, pkColName, relationships);

            if (fkInfo != null)
            {
                // It IS an FK. We must use existing values from the parent table.
                Msg.Write(MessageType.Debug, $"Composite PK column {pkColName} is an FK. Pulling from parent {fkInfo.ReferencedTable}.");
                values = GetPotentialFkValues(fkInfo);
            }
            else
            {
                // It is NOT an FK (e.g. the 'PhoneNumber' column). Generate fresh unique data.
                var generatorTypeKey = generatorService.FindGeneratorTypeFor(column, table);
                var generator = generatorService.GetGeneratorByKey(generatorTypeKey);
                values = generator.GenerateBatch(column, maxRowCount, true);
            }
            // --- FIX END ---

            keyColumnValuePools[pkColName] = values;

            try
            {
                if (values.Count == 0)
                {
                    totalPossibleCombinations = 0;
                    break;
                }
                totalPossibleCombinations = checked(totalPossibleCombinations * values.Count);
            }
            catch (OverflowException)
            {
                totalPossibleCombinations = long.MaxValue;
            }
        }

        int safeCombinationLimit = Math.Max(maxRowCount * 5, 1000);
        bool generateAll = totalPossibleCombinations > 0 && totalPossibleCombinations < safeCombinationLimit;

        if (totalPossibleCombinations == 0)
        {
            Msg.Write(MessageType.Warning,
                $"Table {table.SchemaName}.{table.TableName} has a composite PK column with no potential values. 0 rows will be generated.");
            return new List<Dictionary<string, object?>>();
        }

        // 2. Generate the unique combinations (Cartesian Product)
        if (generateAll)
        {
            var allCombinations = GetCartesianProduct(keyColumnValuePools);
            return allCombinations.OrderBy(x => _faker.Random.Int()).Take(maxRowCount).ToList();
        }

        // 3. Random sampling path (if Cartesian product is too large)
        var uniqueCombinations = new HashSet<string>();
        var finalKeyList = new List<Dictionary<string, object?>>();
        var sb = new StringBuilder();
        int attempts = 0;
        int maxAttempts = Math.Max(maxRowCount * 10, 1000);

        while (finalKeyList.Count < maxRowCount && attempts < maxAttempts)
        {
            var keyCombo = new Dictionary<string, object?>(StringComparer.CurrentCultureIgnoreCase);
            sb.Clear();

            foreach (var colName in primaryKey.PrimaryKeyColumns)
            {
                // Pick a random value from the pool (which might be the limited list of Parent IDs)
                var value = _faker.PickRandom(keyColumnValuePools[colName]);
                keyCombo[colName] = value;
                sb.Append(value?.ToString() ?? "NULL").Append('-');
            }

            if (uniqueCombinations.Add(sb.ToString()))
            {
                finalKeyList.Add(keyCombo);
            }

            attempts++;
        }

        return finalKeyList;
    }

    /// <summary>
    /// Generates a batch of unique composite *unique index* keys (that are not FKs or PKs).
    /// This is a copy of GenerateCompositePkBatch, adapted for IndexModel.
    /// </summary>
    private List<Dictionary<string, object?>> GenerateCompositeUniqueIndexBatch(
        TableModel table, List<string> indexColumnNames, int maxRowCount)
    {
        var keyColumnValuePools = new Dictionary<string, List<object?>>();
        long totalPossibleCombinations = 1;

        // 1. Get the pool of unique values for each column in the composite key
        foreach (var colName in indexColumnNames)
        {
            var column = table.Columns.First(c => c.ColumnName.Equals(colName, SC));
            List<object?> values; // <-- Will be populated below

            // --- START FIX ---
            // Check if this column in the composite index is ALSO a foreign key
            var fkInfo = FindForeignKeyColumn(table.SchemaName, table.TableName, colName, sourceDbData.Relationships);
    
            if (fkInfo != null)
            {
                // It is an FK! Get its values from the parent table.
                Msg.Write(MessageType.Debug, $"Composite UQ column {colName} is an FK. Pulling from parent.");
                values = GetPotentialFkValues(fkInfo);
            }
            else
            {
                // It's not an FK. Generate new unique data as before.
                var generatorTypeKey = generatorService.FindGeneratorTypeFor(column, table);
                var generator = generatorService.GetGeneratorByKey(generatorTypeKey);
                values = generator.GenerateBatch(column, maxRowCount, true);
            }

            keyColumnValuePools[colName] = values;

            try
            {
                if (values.Count == 0)
                {
                    totalPossibleCombinations = 0;
                    break;
                }

                totalPossibleCombinations = checked(totalPossibleCombinations * values.Count);
            }
            catch (OverflowException)
            {
                totalPossibleCombinations = long.MaxValue;
            }
        }

        int safeCombinationLimit = Math.Max(maxRowCount * 5, 1000);
        bool generateAll = totalPossibleCombinations > 0 && totalPossibleCombinations < safeCombinationLimit;

        if (totalPossibleCombinations == 0)
        {
            Msg.Write(MessageType.Warning,
                $"Table {table.SchemaName}.{table.TableName} has a composite UQ column with no potential values. 0 rows will be generated.");
            return new List<Dictionary<string, object?>>();
        }

        // 2. Generate the unique combinations (Cartesian Product)
        if (generateAll)
        {
            var allCombinations = GetCartesianProduct(keyColumnValuePools);
            return allCombinations.OrderBy(x => _faker.Random.Int()).Take(maxRowCount).ToList();
        }

        // 3. Random sampling path (if Cartesian product is too large)
        var uniqueCombinations = new HashSet<string>();
        var finalKeyList = new List<Dictionary<string, object?>>();
        var sb = new StringBuilder();
        int attempts = 0;
        int maxAttempts = Math.Max(maxRowCount * 10, 1000);

        while (finalKeyList.Count < maxRowCount && attempts < maxAttempts)
        {
            var keyCombo = new Dictionary<string, object?>(StringComparer.CurrentCultureIgnoreCase);
            sb.Clear();

            foreach (var colName in indexColumnNames)
            {
                var value = _faker.PickRandom(keyColumnValuePools[colName]);
                keyCombo[colName] = value;
                sb.Append(value?.ToString() ?? "NULL").Append('-');
            }

            if (uniqueCombinations.Add(sb.ToString()))
            {
                finalKeyList.Add(keyCombo);
            }

            attempts++;
        }

        return finalKeyList;
    }

    /// <summary>
    /// Generates a batch of Identity values.
    /// </summary>
    private List<object?> GenerateIdentityBatch(ColumnModel column, int count)
    {
        var values = new List<object?>(count);
        var currentIdentityValue = column.IdentitySeed ?? 0;
        var identityIncrement = column.IdentityIncrement ?? 1;

        for (int i = 0; i < count; i++)
        {
            values.Add(currentIdentityValue);
            currentIdentityValue += identityIncrement;
        }

        return values;
    }

    /// <summary>
    /// Checks if a column is part of a *single-column* Primary Key or Unique Index.
    /// </summary>
    private bool IsColumnUnique(TableModel table, ColumnModel column)
    {
        var colName = column.ColumnName;

        var pk = sourceDbData.PrimaryKeys
            .FirstOrDefault(pk => pk.SchemaName.Equals(table.SchemaName, SC) &&
                                  pk.TableName.Equals(table.TableName, SC));

        if (pk != null && pk.PrimaryKeyColumns.Count == 1 && pk.PrimaryKeyColumns[0].Equals(colName, SC))
        {
            return true;
        }

        var uniqueIndexes = sourceDbData.Indexes
            .Where(idx => idx.SchemaName.Equals(table.SchemaName, SC) &&
                          idx.TableName.Equals(table.TableName, SC) &&
                          idx.IsUnique && !idx.IsPrimaryKey);

        foreach (var index in uniqueIndexes)
        {
            if (index.IndexColumns.Count == 1 && index.IndexColumns[0].ColumnName.Equals(colName, SC))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Generates a batch for a single unique column (PK, UQ, or unique FK).
    /// </summary>
    private List<object?> GenerateSingleUniqueBatch(ColumnModel column, TableModel table,
        List<RelationshipModel> relationships, int maxRowCount)
    {
        var fkInfo = FindForeignKeyColumn(table.SchemaName, table.TableName, column.ColumnName, relationships);
        List<object?> batch;
        if (fkInfo != null)
        {
            // It's a single-column, unique FK
            batch = GenerateForeignKeyBatch(fkInfo, maxRowCount, true);
        }
        else
        {
            // It's a single-column PK or UQ (not an FK)
            var generatorTypeKey = generatorService.FindGeneratorTypeFor(column, table);
            var generator = generatorService.GetGeneratorByKey(generatorTypeKey);
            batch = generator.GenerateBatch(column, maxRowCount, true);
        }

        return batch;
    }

    /// <summary>
    /// Generates a batch for a "normal" non-unique column.
    /// </summary>
    private List<object?> GenerateRegularBatch(ColumnModel column, TableModel table,
        List<RelationshipModel> relationships, int maxRowCount)
    {
        var fkInfo = FindForeignKeyColumn(table.SchemaName, table.TableName, column.ColumnName, relationships);
        List<object?> batch;
        if (fkInfo != null)
        {
            // It's a non-unique FK
            batch = GenerateForeignKeyBatch(fkInfo, maxRowCount, false);
        }
        else
        {
            // It's just a regular column
            var generatorTypeKey = generatorService.FindGeneratorTypeFor(column, table);
            var generator = generatorService.GetGeneratorByKey(generatorTypeKey);
            batch = generator.GenerateBatch(column, maxRowCount, false);
        }

        return batch;
    }


    // --- HELPER METHODS FOR BATCH GENERATION ---

    private List<Dictionary<string, object?>> GetCartesianProduct(Dictionary<string, List<object?>> lists)
    {
        var combinations = new List<Dictionary<string, object?>>();
        var columnNames = lists.Keys.ToList();

        void GenerateCombinations(int colIndex, Dictionary<string, object?> currentCombo)
        {
            if (colIndex == columnNames.Count)
            {
                combinations.Add(new Dictionary<string, object?>(currentCombo));
                return;
            }

            var colName = columnNames[colIndex];
            if (!lists.TryGetValue(colName, out var values) || !values.Any())
            {
                // If a column has no values, we can't form a combination
                // But if it's nullable, should we proceed with null?
                // For now, let's assume non-empty lists for key generation.
                // A safer approach might be to just skip this combination.
                // Let's just generate the combination *without* this key.
                // This case should be rare and handled by the pool generation logic.
                GenerateCombinations(colIndex + 1, currentCombo);
                return;
            }

            foreach (var value in values)
            {
                currentCombo[colName] = value;
                GenerateCombinations(colIndex + 1, currentCombo);
            }
        }

        GenerateCombinations(0, new Dictionary<string, object?>(StringComparer.CurrentCultureIgnoreCase));
        return combinations;
    }

    private List<object?> GetPotentialFkValues(ForeignKeyInfo foreignKeyInfo)
    {
        var referencedTableData = _generatedData.FirstOrDefault(t =>
            t.SchemaName == foreignKeyInfo.ReferencedSchema &&
            t.TableName == foreignKeyInfo.ReferencedTable);

        if (referencedTableData == null || !referencedTableData.Rows.Any())
        {
            Msg.Write(MessageType.Warning,
                $"Referenced FK table {foreignKeyInfo.ReferencedSchema}.{foreignKeyInfo.ReferencedTable} " +
                $"has no data for FK generation. Returning empty list.");
            return new List<object?>();
        }

        var potentialFkValues = referencedTableData.Rows
            .Select(r => r.Columns
                .FirstOrDefault(c => c.ColumnName.Equals(foreignKeyInfo.ReferencedColumnName, SC))
                ?.RawValue)
            .Where(val => val != null) // Don't propagate nulls as potential FK values
            .Distinct()
            .ToList();

        if (!potentialFkValues.Any())
        {
            Msg.Write(MessageType.Warning,
                $"Referenced FK column {foreignKeyInfo.ReferencedColumnName} in parent table " +
                $"has no (non-null) data. Returning empty list.");
        }

        return potentialFkValues;
    }

    private List<object?> GenerateForeignKeyBatch(ForeignKeyInfo foreignKeyInfo, int count, bool isUnique)
    {
        var potentialFkValues = GetPotentialFkValues(foreignKeyInfo);
        if (!potentialFkValues.Any())
        {
            // If parent has no data, we must return a list of nulls to match the 'count'
            // This assumes the FK column is nullable. If not, this will likely fail on insert,
            // which is correct behavior (can't add child for non-existent parent).
            return Enumerable.Repeat<object?>(null, count).ToList();
        }

        var values = new List<object?>();
        if (isUnique)
        {
            if (potentialFkValues.Count < count)
            {
                Msg.Write(MessageType.Warning,
                    $"Cannot generate {count} unique FKs for {foreignKeyInfo.ReferencedTable}.{foreignKeyInfo.ReferencedColumnName}. " +
                    $"Only {potentialFkValues.Count} unique parent values exist. Capping rows.");

                values.AddRange(potentialFkValues.OrderBy(x => _faker.Random.Int()));
                return values; // This list is *shorter* than count
            }

            values.AddRange(potentialFkValues.OrderBy(x => _faker.Random.Int()).Take(count));
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                values.Add(_faker.PickRandom(potentialFkValues));
            }
        }

        return values;
    }

    private static ForeignKeyInfo? FindForeignKeyColumn(string schemaName, string tableName, string columnName,
        List<RelationshipModel> relationships)
    {
        foreach (var relationship in relationships)
        {
            if (relationship.ParentSchema != schemaName || relationship.ParentTable != tableName)
                continue;

            var fkColumn = relationship.ForeignKeyColumns
                .FirstOrDefault(fc => fc.ParentColumnName.Equals(columnName, SC));

            if (fkColumn != null)
            {
                return new ForeignKeyInfo
                {
                    ReferencedSchema = relationship.ReferencedSchema,
                    ReferencedTable = relationship.ReferencedTable,
                    ReferencedColumnName = fkColumn.ReferencedColumnName
                };
            }
        }

        return null;
    }

    // --- GetEffectiveMaxRows & EstimateColumnCardinality ---
    // (These are unchanged from your file and are correct)

    private int GetEffectiveMaxRows(TableModel table, int configMaxRowCount)
    {
        int smallestMax = configMaxRowCount;
        var pks = sourceDbData.PrimaryKeys
            .Where(pk => pk.SchemaName.Equals(table.SchemaName, SC) &&
                         pk.TableName.Equals(table.TableName, SC));

        foreach (var pk in pks)
        {
            if (pk.PrimaryKeyColumns.Count == 1)
            {
                var colName = pk.PrimaryKeyColumns[0];
                var colModel = table.Columns.FirstOrDefault(c => c.ColumnName.Equals(colName, SC));
                if (colModel != null)
                {
                    smallestMax = Math.Min(smallestMax, EstimateColumnCardinality(colModel));
                }
            }
        }

        var uniqueIndexes = sourceDbData.Indexes
            .Where(idx => idx.SchemaName.Equals(table.SchemaName, SC) &&
                          idx.TableName.Equals(table.TableName, SC) &&
                          idx.IsUnique && !idx.IsPrimaryKey);
        foreach (var index in uniqueIndexes)
        {
            if (index.IndexColumns.Count == 1)
            {
                var colName = index.IndexColumns[0].ColumnName;
                var colModel = table.Columns.FirstOrDefault(c => c.ColumnName.Equals(colName, SC));
                if (colModel != null)
                {
                    smallestMax = Math.Min(smallestMax, EstimateColumnCardinality(colModel));
                }
            }
        }

        return smallestMax;
    }

    private int EstimateColumnCardinality(ColumnModel column)
    {
        switch (column.DataType.ToLower())
        {
            case "bit":
                return 2;
            case "tinyint":
                return 256;
            case "smallint":
                return 65536;
            case "char":
            case "varchar":
            case "nchar":
            case "nvarchar":
                var maxLength = DataTypeHelper.GetMaxLength(column);
                if (maxLength == 1) return 95;
                if (maxLength == 2) return 95 * 95;
                return int.MaxValue;
            default:
                return int.MaxValue;
        }
    }
}