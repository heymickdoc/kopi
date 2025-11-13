using Bogus;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityDefaultIntegerGenerator : IDataGenerator
{
    public string TypeName => "default_integer";
    
    private readonly Faker _faker = new(); 
    
    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var maxLength = DataTypeHelper.GetMaxLength(column);
        
        var (minValue, maxValue) = DataTypeHelper.GetMinMaxIntegerValues(column.DataType);

        var values = new List<object?>(count);
        for (var i = 0; i < count; i++)
        {
            if (column.DataType.Equals("tinyint", StringComparison.OrdinalIgnoreCase))
            {
                values.Add(_faker.Random.Byte(0, (byte)maxValue));
            }
            else if (column.DataType.Equals("smallint", StringComparison.OrdinalIgnoreCase))
            {
                values.Add(_faker.Random.Short(0, (short)maxValue));
            }
            else if (column.DataType.Equals("int", StringComparison.OrdinalIgnoreCase))
            {
                values.Add(_faker.Random.Int(0, (int)maxValue));
            }
            else if (column.DataType.Equals("bigint", StringComparison.OrdinalIgnoreCase))
            {
                values.Add(_faker.Random.Long(0, maxValue));
            }
            else
            {
                //Default to int
                values.Add(_faker.Random.Int(0, (int)maxValue));
            }
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