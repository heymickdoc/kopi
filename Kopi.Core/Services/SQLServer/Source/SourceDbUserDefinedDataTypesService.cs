using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using Microsoft.Data.SqlClient;
using System.Text; // Needed for StringBuilder

namespace Kopi.Core.Services.SQLServer.Source;

public class SourceDbUserDefinedDataTypesService
{
    public static async Task<List<UserDefinedDataTypeModel>> GetUserDefinedDataTypes(KopiConfig config)
    {
        var udtList = new List<UserDefinedDataTypeModel>();

        // 1. Get Scalar UDTs (e.g. 'SSN' -> varchar(11))
        var scalars = await GetScalarUserDefinedDataTypes(config);
        udtList.AddRange(scalars);

        // 2. Get Table Types (e.g. 'MyTableType' -> TABLE (Id int, Name varchar))
        var tableTypes = await GetTableUserDefinedDataTypes(config);
        udtList.AddRange(tableTypes);

        return udtList;
    }

    /// <summary>
    /// Fetches simple alias types (Scalar UDTs).
    /// </summary>
    private static async Task<List<UserDefinedDataTypeModel>> GetScalarUserDefinedDataTypes(KopiConfig config)
    {
        // This is your ORIGINAL query, slightly modified to handle collation if needed
        const string sql = @"
            SELECT 
                SCHEMA_NAME(t.schema_id) AS SchemaName,
                t.name AS TypeName,
                b.name AS BaseTypeName,
                t.max_length AS MaxLength,
                t.precision AS Precision,
                t.scale AS Scale,
                t.is_nullable AS IsNullable,
                'CREATE TYPE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + '.' + QUOTENAME(t.name) + 
                ' FROM ' + QUOTENAME(b.name) + 
                CASE 
                    WHEN b.name IN ('varchar', 'char', 'varbinary', 'binary') 
                        THEN CASE WHEN t.max_length = -1 THEN '(MAX)' ELSE '(' + CAST(t.max_length AS VARCHAR) + ')' END
                    WHEN b.name IN ('nvarchar', 'nchar') 
                        THEN CASE WHEN t.max_length = -1 THEN '(MAX)' ELSE '(' + CAST(t.max_length/2 AS VARCHAR) + ')' END
                    WHEN b.name IN ('decimal', 'numeric') 
                        THEN '(' + CAST(t.precision AS VARCHAR) + ',' + CAST(t.scale AS VARCHAR) + ')'
                    ELSE ''
                END +
                CASE WHEN t.is_nullable = 0 THEN ' NOT NULL' ELSE '' END + ';' AS CreateScript
            FROM sys.types t
            INNER JOIN sys.types b ON t.system_type_id = b.user_type_id
            WHERE t.is_user_defined = 1
                AND t.is_table_type = 0 -- Explicitly scalar only
            ORDER BY SchemaName, TypeName;";

        using IDbConnection conn = new SqlConnection(config.SourceConnectionString);
        if (conn.State != ConnectionState.Open) conn.Open();
        
        var rawData = await conn.QueryAsync<RawUserDefinedDataTypesModel>(sql);
        
        return rawData.Select(r => new UserDefinedDataTypeModel
        {
            SchemaName = r.SchemaName,
            TypeName = r.TypeName,
            CreateScript = r.CreateScript
            // Map other properties if needed
        }).ToList();
    }

    /// <summary>
    /// Fetches User-Defined Table Types.
    /// </summary>
    private static async Task<List<UserDefinedDataTypeModel>> GetTableUserDefinedDataTypes(KopiConfig config)
    {
        // We need a script generation approach for Table Types because they have columns, keys, etc.
        // SQL Server doesn't provide a one-liner "CreateScript" for these easily.
        // We will construct it manually.

        const string sql = @"
            SELECT 
                tt.user_type_id AS TypeId,
                SCHEMA_NAME(tt.schema_id) AS SchemaName,
                tt.name AS TypeName,
                c.name AS ColumnName,
                t.name AS DataType,
                c.max_length,
                c.precision,
                c.scale,
                c.is_nullable,
                c.is_identity,
                c.column_id
            FROM sys.table_types tt
            JOIN sys.columns c ON c.object_id = tt.type_table_object_id
            JOIN sys.types t ON c.user_type_id = t.user_type_id
            ORDER BY SchemaName, TypeName, c.column_id";

        using IDbConnection conn = new SqlConnection(config.SourceConnectionString);
        if (conn.State != ConnectionState.Open) conn.Open();

        var rows = await conn.QueryAsync<RawTableTypeColumnModel>(sql);

        var result = new List<UserDefinedDataTypeModel>();

        // Group by Type (since we get one row per column)
        var groupedTypes = rows.GroupBy(r => new { r.SchemaName, r.TypeName });

        foreach (var group in groupedTypes)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TYPE [{group.Key.SchemaName}].[{group.Key.TypeName}] AS TABLE(");

            var columns = group.OrderBy(x => x.column_id).ToList();
            for (int i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                sb.Append($"    [{col.ColumnName}] {GetSqlDataType(col)}");

                if (col.is_nullable) sb.Append(" NULL");
                else sb.Append(" NOT NULL");

                if (col.is_identity) sb.Append(" IDENTITY(1,1)"); // Simplified identity assumption

                if (i < columns.Count - 1) sb.AppendLine(",");
                else sb.AppendLine("");
            }

            // TODO: Ideally, you should also fetch Indexes/PKs on Table Types here!
            // For now, this handles the column structure.
            
            sb.AppendLine(");");

            result.Add(new UserDefinedDataTypeModel
            {
                SchemaName = group.Key.SchemaName,
                TypeName = group.Key.TypeName,
                CreateScript = sb.ToString()
            });
        }
        
        Msg.Write(MessageType.Info, $"Found {result.Count} user defined table types.");
        return result;
    }

    // Helper to format data types (varchar, decimal, etc)
    private static string GetSqlDataType(RawTableTypeColumnModel col)
    {
        var type = col.DataType.ToLower();
        if (type == "varchar" || type == "char" || type == "varbinary" || type == "binary")
        {
            return $"{type}({(col.max_length == -1 ? "MAX" : col.max_length.ToString())})";
        }
        if (type == "nvarchar" || type == "nchar")
        {
            return $"{type}({(col.max_length == -1 ? "MAX" : (col.max_length / 2).ToString())})";
        }
        if (type == "decimal" || type == "numeric")
        {
            return $"{type}({col.precision},{col.scale})";
        }
        return type;
    }

    // Internal class for Dapper mapping
    private class RawTableTypeColumnModel
    {
        public int TypeId { get; set; }
        public string SchemaName { get; set; } = "";
        public string TypeName { get; set; } = "";
        public string ColumnName { get; set; } = "";
        public string DataType { get; set; } = "";
        public short max_length { get; set; }
        public byte precision { get; set; }
        public byte scale { get; set; }
        public bool is_nullable { get; set; }
        public bool is_identity { get; set; }
        public int column_id { get; set; }
    }
}