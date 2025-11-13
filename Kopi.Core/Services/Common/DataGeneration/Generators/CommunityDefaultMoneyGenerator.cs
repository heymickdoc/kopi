using Bogus;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

/// <summary>
/// Generates random money values for money and smallmoney data types.
/// </summary>
public class CommunityDefaultMoneyGenerator : IDataGenerator
{
    public string TypeName => "default_money";
    
    private readonly Faker _faker = new(); 
    
    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var values = new List<object?>(count);
        
        //We know it's a money data type but it might be smallmoney
        var isSmallMoney = column.DataType.ToLower().Equals("smallmoney");
        
        //Set limts based on whether it's smallmoney or money
        //Smallmoney can be from 0 to 5000
        //Money can be from 0 to 1,000,000
        var (minValue, maxValue) = isSmallMoney ? (0m, 5000m) : (0m, 1_000_000m);
        
        for (var i = 0; i < count; i++)
        {
            var moneyValue = _faker.Finance.Amount(minValue, maxValue, 2);
            values.Add(moneyValue);
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