using System.Data;
using Dapper;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.PostgreSQL;
using Kopi.Core.Utilities;
using Npgsql;

namespace Kopi.Core.Services.PostgreSQL.Source;

public class PostgresSourceDbTableService
{
    /// <summary>
    /// Gets the table and column information from the Postgres source database.
    /// </summary>
    public static async Task<List<TableModel>> GetTables(KopiConfig config)
    {
        var rawTables = await GetRawTableData(config);
        return MapRawTablesToTableModel(rawTables);
    }

    private static async Task<List<RawPostgresDenormalizedTableModel>> GetRawTableData(KopiConfig config)
    {
        // This query joins information_schema columns with constraint info to find Primary Keys.
        // It handles Postgres 10+ identity columns.
        const string sql = @"
        SELECT 
            c.table_schema AS SchemaName,
            c.table_name AS TableName,
            c.column_name AS ColumnName,
            c.ordinal_position AS OrdinalPosition,
            c.data_type AS DataType,
            c.udt_name AS UdtName,
            c.is_nullable AS IsNullable,
            c.character_maximum_length AS CharacterMaxLength,
            c.numeric_precision AS NumericPrecision,
            c.numeric_scale AS NumericScale,
            c.is_identity AS IsIdentity,
            c.identity_generation AS IdentityGeneration,
            -- Cast sequence info to text to avoid type mismatch issues across PG versions
            CAST(s.seqstart AS text) AS IdentityStart,
            CAST(s.seqincrement AS text) AS IdentityIncrement,
            c.column_default AS DefaultDefinition,
            c.generation_expression AS GenerationExpression,
            
            -- Check for Primary Key
            CASE WHEN pk.column_name IS NOT NULL THEN true ELSE false END AS IsPrimaryKey

        FROM information_schema.columns c
        -- Left join to find Primary Keys
        LEFT JOIN (
            SELECT kcu.table_schema, kcu.table_name, kcu.column_name
            FROM information_schema.key_column_usage kcu
            JOIN information_schema.table_constraints tc 
              ON kcu.constraint_name = tc.constraint_name 
              AND kcu.table_schema = tc.table_schema
            WHERE tc.constraint_type = 'PRIMARY KEY'
        ) pk ON c.table_schema = pk.table_schema 
            AND c.table_name = pk.table_name 
            AND c.column_name = pk.column_name

        -- Left join to sequences to get identity details (PG 10+)
        LEFT JOIN pg_class t ON t.relname = c.table_name AND t.relnamespace = (SELECT oid FROM pg_namespace WHERE nspname = c.table_schema)
        LEFT JOIN pg_attribute a ON a.attrelid = t.oid AND a.attname = c.column_name
        LEFT JOIN pg_sequence s ON s.seqrelid = pg_get_serial_sequence(quote_ident(c.table_schema) || '.' || quote_ident(c.table_name), c.column_name)::regclass

        WHERE c.table_schema NOT IN ('information_schema', 'pg_catalog')
        ORDER BY c.table_schema, c.table_name, c.ordinal_position;";

        // Note: You might need to adjust the GetConnectionString() to return a proper Npgsql string
        // defined in your updated DatabaseConfig.
        var connectionString = config.SourceConnectionString; 

        using IDbConnection conn = new NpgsqlConnection(connectionString);
        try
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            
            var data = await conn.QueryAsync<RawPostgresDenormalizedTableModel>(sql);
            
            Msg.Write(MessageType.Info, $"Found {data.Count()} columns in source database.");
            return data.ToList();
        }
        catch (NpgsqlException ex)
        {
            Msg.Write(MessageType.Error, $"Postgres Exception while reading tables: {ex.Message}");
            throw; // Or Environment.Exit(1) to match your style
        }
        finally
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }
    }

    private static List<TableModel> MapRawTablesToTableModel(List<RawPostgresDenormalizedTableModel> rawTableList)
    {
        var tables = new List<TableModel>();

        foreach (var row in rawTableList)
        {
            var existingTable = tables.FirstOrDefault(t => t.TableName == row.TableName && t.SchemaName == row.SchemaName);
            if (existingTable == null)
            {
                existingTable = new TableModel
                {
                    SchemaName = row.SchemaName,
                    TableName = row.TableName,
                    Columns = new List<ColumnModel>()
                };
                tables.Add(existingTable);
            }
            
            // Identity Logic:
            // 1. If Postgres explicitly says it's an IDENTITY column (PG 10+ syntax)
            // 2. OR if we successfully parsed sequence metadata (Seed/Increment) which implies a SERIAL/SEQUENCE relationship
            
            var seed = int.TryParse(row.IdentityStart, out var s) ? s : (int?)null;
            var increment = int.TryParse(row.IdentityIncrement, out var i) ? i : (int?)null;
            
            // Fix: Trust the presence of sequence metadata as proof of identity behavior
            var isIdentity = (row.IsIdentity == "YES") || (seed.HasValue && increment.HasValue);

            // Map Postgres types/nuances to the Generic ColumnModel
            var col = new ColumnModel
            {
                SchemaName = row.SchemaName,
                TableName = row.TableName,
                ColumnName = row.ColumnName,
                
                // Use UdtName (e.g. 'int4') or DataType (e.g. 'integer'). 
                // UdtName is often safer for bulk copy mapping.
                DataType = row.UdtName, 
                
                IsNullable = row.IsNullable == "YES",
                
                // Map length: Null means unbounded (Text) -> "MAX"
                MaxLength = row.CharacterMaxLength.HasValue 
                    ? row.CharacterMaxLength.Value.ToString() 
                    : "MAX",

                NumericPrecision = row.NumericPrecision ?? 0,
                NumericScale = row.NumericScale ?? 0,
                
                IsPrimaryKey = row.IsPrimaryKey,
                
                // Identity logic
                IsIdentity = isIdentity,
                IdentitySeed = seed,
                IdentityIncrement = increment,

                // Computed / Generated
                IsComputed = !string.IsNullOrEmpty(row.GenerationExpression),
                ComputedDefinition = row.GenerationExpression,
                
                // If we detected it as Identity via sequence, we MUST clear the DefaultDefinition.
                // Otherwise, the Target Script Generator might try to add "DEFAULT nextval(...)" 
                // which will fail because the sequence doesn't exist in the target yet.
                DefaultDefinition = isIdentity ? null : row.DefaultDefinition,
                
                // Postgres doesn't strictly have "User Defined Types" in the SQL Server sense 
                // (alias types), but it has Enums/Composites. For now, assume false.
                IsUserDefinedType = false 
            };

            existingTable.Columns.Add(col);
        }

        return tables;
    }
}