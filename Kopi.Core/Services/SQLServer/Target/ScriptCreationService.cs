using System.Text;
using Kopi.Core.Models;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.SQLServer.Target;

public static class ScriptCreationService
{
    /// <summary>
    ///  Generates the T-SQL script to create the database.
    /// </summary>
    /// <param name="config">The config file</param>
    /// <param name="sourceDbData">The source db data needed for the script</param>
    /// <returns>The script</returns>
    public static string GenerateDatabaseCreationScript(KopiConfig config, SourceDbModel sourceDbData)
    {
        var dbName = DatabaseHelper.GetDatabaseName(config.SourceConnectionString, DatabaseType.SqlServer);
        
        var sb = new StringBuilder();
        sb.AppendLine("CREATE DATABASE [" + dbName + "];");
        
        return sb.ToString().ReplaceLineEndings();
    }
    
    public static string GenerateTableCreationScript(KopiConfig config, SourceDbModel sourceDbData)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("USE [" + DatabaseHelper.GetDatabaseName(config.SourceConnectionString, DatabaseType.SqlServer) + "];");
        sb.AppendLine();

        foreach (var table in sourceDbData.Tables)
        {
            if (table.TableName.Equals("employee", StringComparison.OrdinalIgnoreCase))
            {
                // Just a test to limit the number of tables created during development
            }
            
            sb.AppendLine("CREATE TABLE [" + table.SchemaName + "].[" + table.TableName + "] (");
            var columnDefinitions = new List<string>();
            foreach (var column in table.Columns)
            {
                var nullability = column.IsNullable ? "NULL" : "NOT NULL";
                //Identity
                if (column.IsIdentity)
                {
                    nullability = $"NOT NULL IDENTITY({column.IdentitySeed}, {column.IdentityIncrement})";
                }
                //Computed
                if (column.IsComputed && !string.IsNullOrWhiteSpace(column.ComputedDefinition))
                {
                    //Check if it is persisted
                    var persisted = column.IsPersisted ? "PERSISTED" : "";
                    nullability = $"AS {column.ComputedDefinition} {persisted}";
                }
                //Default
                if (!string.IsNullOrWhiteSpace(column.DefaultDefinition) && !column.IsComputed)
                {
                    nullability += $" CONSTRAINT [{column.DefaultConstraintName}] DEFAULT {column.DefaultDefinition}";
                }
                
                //We don't need a datatype with a computed column
                if (column.IsComputed)
                {
                    columnDefinitions.Add($"    [{column.ColumnName}] {nullability}");;
                    continue;
                }
                
                //Data Type with length/precision/scale
                var dataType = column.DataType;
                if (column.DataType.Equals("nvarchar", StringComparison.OrdinalIgnoreCase) ||
                    column.DataType.Equals("varchar", StringComparison.OrdinalIgnoreCase) ||
                    column.DataType.Equals("char", StringComparison.OrdinalIgnoreCase) ||
                    column.DataType.Equals("nchar", StringComparison.OrdinalIgnoreCase))
                {
                    dataType += column.MaxLength == "-1" ? "(MAX)" : $"({column.MaxLength})";
                }
                else if (column.DataType.Equals("decimal", StringComparison.OrdinalIgnoreCase) ||
                         column.DataType.Equals("numeric", StringComparison.OrdinalIgnoreCase))
                {
                    dataType += $"({column.NumericPrecision}, {column.NumericScale})";
                }

                columnDefinitions.Add($"    [{column.ColumnName}] {dataType} {nullability}");
            }
            sb.AppendLine(string.Join(",\n", columnDefinitions));
            sb.AppendLine(");");
            sb.AppendLine();
        }
        
        return sb.ToString().ReplaceLineEndings();
    }
    
    public static string GeneratePrimaryKeyCreationScript(KopiConfig config, SourceDbModel sourceDbData)
    {
        var sb = new StringBuilder();
        sb.AppendLine("USE [" + DatabaseHelper.GetDatabaseName(config.SourceConnectionString, DatabaseType.SqlServer) + "];");
        sb.AppendLine();
        
        foreach (var pk in sourceDbData.PrimaryKeys)
        {
            var pkColumns = string.Join(", ", pk.PrimaryKeyColumns.Select(c => $"[{c}]"));
            sb.AppendLine($"ALTER TABLE [{pk.SchemaName}].[{pk.TableName}] ADD CONSTRAINT [{pk.PrimaryKeyName}] PRIMARY KEY ({pkColumns});");
            sb.AppendLine();
        }
        
        return sb.ToString().ReplaceLineEndings();
    }

    /// <summary>
    /// Generates the DB schemas list, e.g. dbo, sales, admin etc. NOT the tables within the schemas.
    /// </summary>
    /// <param name="config"></param>
    /// <param name="sourceDbData"></param>
    /// <returns></returns>
    public static string GenerateDbSchemasCreationScript(KopiConfig config, SourceDbModel sourceDbData)
    {
        var sb = new StringBuilder();
        sb.AppendLine("USE [" + DatabaseHelper.GetDatabaseName(config.SourceConnectionString, DatabaseType.SqlServer) + "];");
        
        var allSchemas = sourceDbData.Tables.Select(t => t.SchemaName).Distinct();
        foreach (var schema in allSchemas)
        {
            sb.AppendLine($"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{schema}')");
            sb.AppendLine($"BEGIN");
            sb.AppendLine($"    EXEC('CREATE SCHEMA [{schema}]');");
            sb.AppendLine($"END");
            sb.AppendLine();
        }
        
        return sb.ToString().ReplaceLineEndings();
    }

    public static string GenerateRelationshipCreationScript(KopiConfig config, SourceDbModel sourceDbData)
    {
        var sb = new StringBuilder();
        sb.AppendLine("USE [" + DatabaseHelper.GetDatabaseName(config.SourceConnectionString, DatabaseType.SqlServer) + "];");
        sb.AppendLine();

        foreach (var relationship in sourceDbData.Relationships)
        {
            var fkColumns = string.Join(", ", relationship.ForeignKeyColumns.OrderBy(fc => fc.KeyOrdinal).Select(fc => $"[{fc.ParentColumnName}]"));
            var pkColumns = string.Join(", ", relationship.ForeignKeyColumns.OrderBy(fc => fc.KeyOrdinal).Select(fc => $"[{fc.ReferencedColumnName}]"));
            sb.AppendLine($"ALTER TABLE [{relationship.ParentSchema}].[{relationship.ParentTable}] ADD CONSTRAINT [{relationship.ForeignKeyName}] FOREIGN KEY ({fkColumns}) REFERENCES [{relationship.ReferencedSchema}].[{relationship.ReferencedTable}] ({pkColumns});");
            sb.AppendLine();
        }

        return sb.ToString().ReplaceLineEndings();
    }

    public static string GenerateIndexCreationScript(KopiConfig config, SourceDbModel sourceDbData)
    {
        
        
        var sb = new StringBuilder();
        sb.AppendLine("USE [" + DatabaseHelper.GetDatabaseName(config.SourceConnectionString, DatabaseType.SqlServer) + "];");
        sb.AppendLine();

        foreach (var index in sourceDbData.Indexes)
        {
            // Skip primary key indexes as they're typically created with the table
            if (index.IsPrimaryKey)
                continue;
            
            var unique = index.IsUnique ? "UNIQUE " : "";
            //Need to take into account included columns
            var keyColumns = index.IndexColumns.Where(c => !c.IsIncludedColumn)
                .OrderBy(c => c.KeyOrdinal)
                .Select(c => $"[{c.ColumnName}]");
            var includedColumns = index.IndexColumns.Where(c => c.IsIncludedColumn)
                .OrderBy(c => c.KeyOrdinal)
                .Select(c => $"[{c.ColumnName}]");
        
            // Add existence check for ANY object with the same name
            sb.AppendLine($"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = '{index.IndexName}' AND object_id = OBJECT_ID('{index.SchemaName}.{index.TableName}'))");
            sb.AppendLine($"    CREATE {unique}INDEX [{index.IndexName}] ON [{index.SchemaName}].[{index.TableName}] ({string.Join(", ", keyColumns)})" +
                          (includedColumns.Any() ? $" INCLUDE ({string.Join(", ", includedColumns)})" : "") + ";");
            sb.AppendLine();
        }

        return sb.ToString().ReplaceLineEndings();
    }



    public static string GenerateStoredProcedureCreationScript(KopiConfig config, SourceDbModel sourceDbData)
    {
        var sb = new StringBuilder();
        //sb.AppendLine("USE [" + DatabaseHelper.GetDatabaseName(config.SourceConnectionString) + "];");
        //sb.AppendLine();

        int count = 0;
        foreach (var sp in sourceDbData.StoredProcedures)
        {
            if (count == 2)
            {
                //Just a safety to not generate too much TSQL in one go.
                break;
            }
            count++;
            
            //sb.AppendLine($"IF OBJECT_ID('{sp.SchemaName}.{sp.ObjectName}', 'P') IS NOT NULL");
            //sb.AppendLine($"    DROP PROCEDURE [{sp.SchemaName}].[{sp.ObjectName}];");
            //sb.AppendLine("GO");
            sb.AppendLine(sp.Definition);
            //sb.AppendLine("GO");
            sb.AppendLine();
        }
        return sb.ToString().ReplaceLineEndings();
    }
    
    public static string GenerateFunctionCreationScript(KopiConfig config, SourceDbModel sourceDbData)
    {
        var sb = new StringBuilder();
        // sb.AppendLine("USE [" + DatabaseHelper.GetDatabaseName(config.SourceConnectionString) + "];");
        // sb.AppendLine();
        
        foreach (var fn in sourceDbData.Functions)
        {
            // sb.AppendLine($"IF OBJECT_ID('{fn.SchemaName}.{fn.ObjectName}', 'FN') IS NOT NULL");
            // sb.AppendLine($"    DROP FUNCTION [{fn.SchemaName}].[{fn.ObjectName}];");
            // sb.AppendLine("GO");
            sb.AppendLine(fn.Definition);
            // sb.AppendLine("GO");
            sb.AppendLine();
        }
        return sb.ToString().ReplaceLineEndings();
    }

    public static string GenerateUserDefinedDataTypeCreationScript(KopiConfig config, SourceDbModel sourceDbData)
    {
        var sb = new StringBuilder();
        sb.AppendLine("USE [" + DatabaseHelper.GetDatabaseName(config.SourceConnectionString, DatabaseType.SqlServer) + "];");
        sb.AppendLine();

        foreach (var udf in sourceDbData.UserDefinedDataTypes)
        {
            sb.AppendLine(udf.CreateScript);
            sb.AppendLine();
        }
        
        return sb.ToString().ReplaceLineEndings();
    }
}