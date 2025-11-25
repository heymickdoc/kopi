using System.Data;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Types;

namespace Kopi.Core.Services.SQLServer.Target;

public class DataInsertionService
{
    public async Task InsertData(KopiConfig config, SourceDbModel sourceDbData, List<TargetDataModel> generatedData, string dbConnectionString)
    {
        foreach (var entry in generatedData)
        {
            var tableFullName = $"{entry.SchemaName}.{entry.TableName}";
            
            Msg.Write(MessageType.Info, $"Inserting data into table: {tableFullName}");
            
            await BulkInsertIntoTable(dbConnectionString, entry);
            Console.WriteLine("");
            //Msg.Write(MessageType.Info, $"Inserted {rows.Count} rows into table: {tableFullName}");
        }
    }
    
    private async Task BulkInsertIntoTable(string dbConnectionString,
        TargetDataModel entry)
    {
        var tableFullName = $"{entry.SchemaName}.{entry.TableName}";
        var rows = entry.Rows;
    
        if (rows.Count == 0) return;
    
        using var conn = new SqlConnection(dbConnectionString);
        try
        {
            await conn.OpenAsync();
    
            using var bulkCopy = new SqlBulkCopy(dbConnectionString,
                SqlBulkCopyOptions.TableLock |
                SqlBulkCopyOptions.FireTriggers |
                SqlBulkCopyOptions.KeepIdentity);
            bulkCopy.DestinationTableName = tableFullName;
            bulkCopy.BatchSize = 1000;
            bulkCopy.BulkCopyTimeout = 300;
    
            // Create DataTable with only non-identity, non-computed columns
            var dataTable = new DataTable();
    
            // Add columns based on the data we actually have
            if (rows.Count > 0)
            {
                foreach (var column in rows[0].Columns)
                {
                    var columnName = column.ColumnName;
    
                    if (column.SqlDataType.Equals("hierarchyid", StringComparison.OrdinalIgnoreCase))
                    {
                        dataTable.Columns.Add(columnName, typeof(SqlHierarchyId));
                    }
                    else if (column.SqlDataType.Equals("geography", StringComparison.OrdinalIgnoreCase))
                    {
                        dataTable.Columns.Add(columnName, typeof(SqlGeography));
                    }
                    else if (column.SqlDataType.Equals("geometry", StringComparison.OrdinalIgnoreCase))
                    {
                        dataTable.Columns.Add(columnName, typeof(SqlGeometry));
                    }
                    else
                    {
                        dataTable.Columns.Add(columnName, column.RawValue?.GetType() ?? typeof(object));
                    }
    
                    bulkCopy.ColumnMappings.Add(columnName, columnName);
                }
                
                // Add all columns from the first row (should already be filtered, but validate)
                foreach (var rowData in rows)
                {
                    var dataRow = dataTable.NewRow();

                    foreach (var column in rowData.Columns)
                    {
                        if (column.RawValue == null)
                        {
                            dataRow[column.ColumnName] = DBNull.Value;
                        }
                        else
                        {
                            if (column.SqlDataType.Equals("hierarchyid", StringComparison.OrdinalIgnoreCase))
                            {
                                var hierarchyIdString = column.RawValue.ToString();
                                var sqlHierarchyId = SqlHierarchyId.Parse(hierarchyIdString);
                                dataRow[column.ColumnName] = sqlHierarchyId;
                            }
                            else if (column.SqlDataType.Equals("geography", StringComparison.OrdinalIgnoreCase))
                            {
                                var geographyText = column.RawValue.ToString();
                                // Use SqlGeography directly - this is what SqlBulkCopy expects
                                var sqlGeography = SqlGeography.Parse(geographyText);
                                dataRow[column.ColumnName] = sqlGeography;
                            }
                            else if (column.SqlDataType.Equals("geometry", StringComparison.OrdinalIgnoreCase))
                            {
                                var geometryText = column.RawValue.ToString();
                                // Use SqlGeometry directly - this is what SqlBulkCopy expects
                                var sqlGeometry = SqlGeometry.Parse(geometryText);
                                dataRow[column.ColumnName] = sqlGeometry;
                            }
                            else
                            {
                                dataRow[column.ColumnName] = column.RawValue ?? DBNull.Value;
                            }
                        }
                    }

                    dataTable.Rows.Add(dataRow);
                }
            }
            
            await bulkCopy.WriteToServerAsync(dataTable);
            Msg.Write(MessageType.Success, $"Successfully inserted {rows.Count} rows into {tableFullName}");
        }
        catch (SqlException ex)
        {
            Msg.Write(MessageType.Error, $"SQL Error inserting data into {tableFullName}: {ex.Message}");
            Console.WriteLine($"SQL Error Details: {ex}");
            throw;
        }
        catch (Exception ex)
        {
            Msg.Write(MessageType.Error, $"Error inserting data into {tableFullName}: {ex.Message}");
            Console.WriteLine($"Error Details: {ex}");
            throw;
        }
    }
}