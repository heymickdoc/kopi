using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using Microsoft.Data.SqlClient;

namespace Kopi.Core.Services.SQLServer.Source;

public class SqlServerSourceDbTableService
{
    /// <summary>
    /// Gets the table and column information from the source database and returns it as a list of <see cref="TableModel"/> objects.
    /// </summary>
    /// <param name="config">The initial config</param>
    /// <returns>The list of <see cref="TableModel"/> data</returns>
    public static async Task<List<TableModel>> GetTables(KopiConfig config)
    {
        //Get the raw denormalized table and column data from the source database.
        var rawTables = await GetRawTableData(config);

        //Convert the denormalized list into a list of TableModel objects with nested ColumnModel lists.
        var tableData = MapRawTablesToTableModel(rawTables);

        return tableData;
    }

    /// <summary>
    /// Gets the list of tables from the source SQL Server database. They're returned in a denormalized format (one row per column).
    /// </summary>
    /// <param name="config">The config file</param>
    /// <returns></returns>
    private static async Task<List<RawSqlServerDenormalizedTableModel>> GetRawTableData(KopiConfig config)
    {
        const string sql = @"
        SELECT 
            t.name AS TableName,
            s.name AS SchemaName,
            c.name AS ColumnName,
            CASE 
                WHEN ty.is_user_defined = 1 THEN 
                    (SELECT name FROM sys.types WHERE user_type_id = ty.system_type_id)
                WHEN ty.name = 'sysname' THEN 'nvarchar'
                ELSE ty.name 
            END AS DataType,
            c.is_nullable AS IsNullable,
            c.max_length AS MaxLengthBytes,
            CASE 
                WHEN ty.name = 'sysname' THEN '128'
                WHEN ty.name IN ('bit', 'flag', 'namestyle') THEN ''
                WHEN ty.name IN ('nvarchar', 'nchar') AND c.max_length = -1 THEN 'MAX'
                WHEN ty.name IN ('nvarchar', 'nchar') THEN CAST(c.max_length / 2 AS NVARCHAR(10))
                WHEN ty.name IN ('varchar', 'char', 'varbinary') AND c.max_length = -1 THEN 'MAX'
                WHEN ty.name IN ('varchar', 'char', 'varbinary') THEN CAST(c.max_length AS NVARCHAR(10))
                WHEN c.max_length = -1 THEN 'MAX'
                ELSE CAST(c.max_length AS NVARCHAR(10))
            END AS MaxLength,
            c.precision AS NumericPrecision,
            c.scale AS NumericScale,
            c.is_identity AS IsIdentity,
            CASE 
                WHEN c.is_identity = 1 THEN IDENT_SEED(SCHEMA_NAME(t.schema_id) + '.' + t.name)
                ELSE NULL 
            END AS IdentitySeed,
            CASE 
                WHEN c.is_identity = 1 THEN IDENT_INCR(SCHEMA_NAME(t.schema_id) + '.' + t.name)
                ELSE NULL 
            END AS IdentityIncrement,
            c.is_computed AS IsComputed,
            cc.definition AS ComputedDefinition,
            OBJECT_DEFINITION(dc.object_id) AS DefaultDefinition,
            dc.name AS DefaultConstraintName,
            ty.is_user_defined AS IsUserDefinedType,
            cc.is_persisted AS IsPersisted,CAST(CASE 
                WHEN EXISTS (
                    SELECT 1
                    FROM sys.index_columns ic
                    INNER JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                    WHERE ic.object_id = c.object_id
                      AND ic.column_id = c.column_id
                      AND i.is_primary_key = 1
                ) THEN 1
                ELSE 0
            END AS BIT) AS IsPrimaryKey
        FROM sys.tables t 
        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id 
        INNER JOIN sys.columns c ON t.object_id = c.object_id 
        INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id 
        LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
        LEFT JOIN sys.computed_columns cc ON c.object_id = cc.object_id AND c.column_id = cc.column_id WHERE t.is_ms_shipped = 0 
        ORDER BY s.name, t.name, c.column_id;";

        using IDbConnection conn = new SqlConnection(config.SourceConnectionString);
        try
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var data = await conn.QueryAsync<RawSqlServerDenormalizedTableModel>(sql);
            Msg.Write(MessageType.Info,
                $"Found {data.Count()} columns in source database.");
            return [.. data];
        }
        catch (SqlException ex)
        {
            Msg.Write(MessageType.Error, $"SQL Exception while reading tables from source database: {ex.Message}");
            Environment.Exit(1);
        }
        finally
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }

        return new List<RawSqlServerDenormalizedTableModel>();
    }

    /// <summary>
    /// Maps the raw table data (one row per column) into a list of TableModel objects with nested ColumnModel lists.
    /// </summary>
    /// <param name="rawTableList">The raw table data with denormalized data</param>
    /// <returns>The list of <see cref="TableModel"/> data</returns>
    private static List<TableModel> MapRawTablesToTableModel(List<RawSqlServerDenormalizedTableModel> rawTableList)
    {
        var tables = new List<TableModel>();

        foreach (var table in rawTableList)
        {
            var existingTable = tables.FirstOrDefault(t => t.TableName == table.TableName);
            if (existingTable == null)
            {
                existingTable = new TableModel
                {
                    SchemaName = table.SchemaName,
                    TableName = table.TableName,
                    Columns = new List<ColumnModel>()
                };
                tables.Add(existingTable);
            }

            existingTable.Columns.Add(new ColumnModel
            {
                SchemaName = table.SchemaName,
                TableName = table.TableName,
                ColumnName = table.ColumnName,
                DataType = table.DataType,
                MaxLength = table.MaxLength,
                IsNullable = table.IsNullable,
                NumericPrecision = table.NumericPrecision,
                NumericScale = table.NumericScale,
                IsIdentity = table.IsIdentity,
                IdentitySeed = table.IdentitySeed,
                IdentityIncrement = table.IdentityIncrement,
                IsPrimaryKey = table.IsPrimaryKey,
                IsComputed = table.IsComputed,
                IsPersisted = table.IsPersisted,
                ComputedDefinition = table.ComputedDefinition,
                DefaultDefinition = table.DefaultDefinition,
                DefaultConstraintName = table.DefaultConstraintName,
                IsUserDefinedType = table.IsUserDefinedType
            });
        }

        return tables;
    }
}