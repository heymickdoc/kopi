using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

public class CommunityCreditCardDateMatcher : IColumnMatcher
{
    public int Priority => 10;
    public string GeneratorTypeKey => "credit_card_date";
    
    private static readonly HashSet<string> ColumnNames = new()
    {
        "creditcardexpdate",
        "creditcardexpiration",
        "ccexpdate",
        "ccexpiration",
        "cardexpdate",
        "cardexpiration",
        "ccdate",
        "carddate",
        "ccexpiry",
        "cardexpiry",
        "creditcardexpiry",
        "creditcardexp",
        "expdate",
        "expmonth",
        "expyear"
    };
    
    private static readonly HashSet<string> InvalidWords = new()
    {
        "code",
        "name",
        "number",
        "num",
        "pan",
        "type"
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
        
        //Let's rule out things like "cardnumber" or "cardexpdate"
        if (InvalidWords.Any(k => colName.EndsWith(k) || colName.StartsWith(k)))
        {
            return false;
        }
        
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

        if (DataTypeHelper.IsIntegerType(dataType))
        {
            score += 2;
        }
        else if (DataTypeHelper.IsDateType(dataType))
        {
            score += 4;
        }
        
        return score >= 20;
    }
}