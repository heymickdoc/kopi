using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityCreditCardNumberMatcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "credit_card_number";

    private static readonly HashSet<string> ColumnNames = new()
    {
        "creditcardnumber",
        "creditcardnum",
        "ccnumber",
        "cardnumber",
        "ccn",
        "cardnum",
        "pan",
        "creditcard",
        "cc",
        "card"
    };
    
    private static readonly HashSet<string> InvalidWords = new()
    {
        "type",
        "code",
        "exp",
        "date",
        "name"
    };

    private static readonly HashSet<string> TableNames = new()
    {
        "payment",
        "transaction",
        "order",
        "invoice",
        "customer"
    };

    private static readonly HashSet<string> SchemaNames = new()
    {
        "billing",
        "sale",
        "finance"
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        var score = 0;

        var colName = new string(column.ColumnName.ToLower().Where(char.IsLetterOrDigit).ToArray());
        var tableName = tableContext.TableName.ToLower();
        var schemaName = tableContext.SchemaName.ToLower();
        var dataType = column.DataType.ToLower();
        
        //Let's rule out things like "cardtype" or "cardexpdate"
        if (InvalidWords.Any(k => colName.EndsWith(k) || colName.StartsWith(k)))
        {
            return false;
        }

        //Check data types
        if (SchemaNames.Any(k => schemaName.Contains(k)))
        {
            score += 4;
        }

        if (TableNames.Any(k => tableName.Contains(k)))
        {
            score += 8;
        }

        //We want to match exact column names highest
        if (ColumnNames.Contains(colName))
        {
            score += 32;
        }
        else if (ColumnNames.Any(k => colName.Contains(k)))
        {
            score += 16;
        }

        if (DataTypeHelper.IsStringType(column.DataType))
        {
            //I just want the value we'll use with SQL Server, so ignore double-byte accounting
            var maxLength = DataTypeHelper.GetMaxLength(column, accountForDoubleByte:false);

            // Run lowest-score checks first
            if (maxLength >= 16)
            {
                score += 16;
            }
            else if (maxLength >= 13 && maxLength <= 25) // Accommodate for formatting with spaces or dashes
            {
                score += 32;
            }
            else if (maxLength >= 13 && maxLength <= 19)
            {
                score += 64;
            }
        }

        if (dataType == "numeric" || dataType == "decimal")
        {
            if (column.NumericScale == 0 && column.NumericPrecision >= 13 && column.NumericPrecision <= 19)
            {
                score += 128;
            }
        }

        // --- Penalties ---
        if (column.NumericScale > 0)
        {
            score -= 10;
        }

        // --- Low-Confidence ---
        if (!column.IsNullable)
        {
            score += 1;
        }

        return score >= 40;
    }

    
}