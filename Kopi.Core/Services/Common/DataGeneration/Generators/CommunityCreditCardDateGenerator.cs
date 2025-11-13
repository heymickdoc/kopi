using Bogus;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityCreditCardDateGenerator : IDataGenerator
{
    public string TypeName => "credit_card_date";
    
    private readonly Faker _faker = new(); 
    
    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var maxLength = DataTypeHelper.GetMaxLength(column);
        var isIntegerDataType = DataTypeHelper.IsIntegerType(column.DataType);

        if (isIntegerDataType)
        {
            //Check if it contains "year" or "yr" to determine if we should generate year only
             var columnNameLower = column.ColumnName.ToLower();
            if (columnNameLower.Contains("year") && !columnNameLower.Contains("month"))
            {
                //Generate year only
                var values = new List<object?>(count);
                for (var i = 0; i < count; i++)
                {
                    values.Add(_faker.Date.Future().Year);
                }
                
                //Check for nullability. If so, make a maximum of 10% nulls
                if (!column.IsNullable) return values;
                
                for (var i = 0; i < values.Count; i++)
                {
                    if (Random.Shared.NextDouble() < 0.1) //10% chance
                    {
                        values[i] = null;
                    }
                }
                
                return values;
            }
            
            if (!columnNameLower.Contains("year") && columnNameLower.Contains("month"))
            {
                var values = new List<object?>(count);
                for (var i = 0; i < count; i++)
                {
                    values.Add(_faker.Date.Future().Month);
                }
                
                //Check for nullability. If so, make a maximum of 10% nulls
                if (!column.IsNullable) return values;
                
                for (var i = 0; i < values.Count; i++)
                {
                    if (Random.Shared.NextDouble() < 0.1) //10% chance
                    {
                        values[i] = null;
                    }
                }
                
                return values;
            }
            
        }
        
        //Default to string generation
        var stringValues = new List<object?>(count);
        for (var i = 0; i < count; i++)
        {
            var expDate = _faker.Date.Future();
            var expDateStr = expDate.ToString("MM/yy");
            if (expDateStr.Length > maxLength)
            {
                expDateStr = expDateStr.Substring(0, maxLength);
            }
            stringValues.Add(expDateStr);
        }
        
        //Check for nullability. If so, make a maximum of 10% nulls
        if (!column.IsNullable) return stringValues;
        
        for (var i = 0; i < stringValues.Count; i++)
        {
            if (Random.Shared.NextDouble() < 0.1) //10% chance
            {
                stringValues[i] = null;
            }
        }
        return stringValues;
    }
}