using System.Text;
using Bogus;
using Kopi.Core.Models.Common;
using Kopi.Core.Utilities;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Common.DataGeneration.Generators;
using Kopi.Core.Services.SQLServer.DataGeneration;
using System.Collections.Generic;
using System.Linq;
using Kopi.Core.Interfaces;

namespace Kopi.Core.Services.Common;

/// <summary>
///  Orchestrates the data generation process across all tables.
/// </summary>
public class DataOrchestratorService(
    KopiConfig config,
    SourceDbModel sourceDbData,
    ITableDataStrategy dataStrategy)
{
    private const StringComparison SC = StringComparison.OrdinalIgnoreCase;
    private readonly List<TargetDataModel> _generatedData = new();

    public async Task<List<TargetDataModel>> OrchestrateDataGeneration()
    {
        var orderedTables = TableTopologyService.GenerateOrderedTableTopology(config, sourceDbData);
        await GenerateData(orderedTables);
        
        if (_generatedData.Count != 0) return _generatedData;
        
        Msg.Write(MessageType.Warning, "No data was generated...");
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
                    $"Table {table.SchemaName}.{table.TableName} has unique constraints limiting rows to {effectiveMaxRows}.");
            }

            // DELEGATE TO STRATEGY
            // Pass the current cache (_generatedData) so it can resolve Foreign Keys
            var rows = await dataStrategy.GenerateTableDataAsync(table, effectiveMaxRows, _generatedData);

            var tableModel = new TargetDataModel
            {
                SchemaName = table.SchemaName,
                TableName = table.TableName,
                Rows = rows
            };
            
            _generatedData.Add(tableModel);
        }
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