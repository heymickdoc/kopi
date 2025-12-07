using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Interfaces;

public interface ITableDataStrategy
{
    /// <summary>
    /// Generates (or retrieves) the data rows for a specific table.
    /// </summary>
    /// <param name="table">The table metadata.</param>
    /// <param name="maxRowCount">The calculated row cap.</param>
    /// <param name="existingData">Data generated for previous tables (needed for Foreign Keys).</param>
    Task<List<RowData>> GenerateTableDataAsync(
        TableModel table, 
        int maxRowCount, 
        List<TargetDataModel> existingData);
}