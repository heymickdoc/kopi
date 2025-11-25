using Bogus;
using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityDefaultDecimalGenerator : IDataGenerator
{
    public string TypeName => "default_decimal";
    
    private readonly Faker _faker = new(); 
    
    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var dataType = column.DataType.ToLowerInvariant();
        var values = new List<object?>(count);

        if (dataType == "decimal" || dataType == "numeric")
        {
            var precision = column.NumericPrecision;
            var scale = column.NumericScale;
            
            // Calculate range (Same as before)
            var maxValue = (decimal)(Math.Pow(10, precision - scale) - Math.Pow(10, -scale));
            var minValue = -maxValue;

            for (var i = 0; i < count; i++)
            {
                // FIX: Use Faker's built-in Decimal helper
                // This handles the random generation within the range deterministically
                var value = _faker.Random.Decimal(minValue, maxValue);
                
                // Rounding is still needed to fit the scale
                value = Math.Round(value, scale);
                values.Add(value);
            }
        }
        else if (dataType == "float")
        {
            for (var i = 0; i < count; i++)
            {
                var sign = _faker.Random.Bool() ? -1 : 1;
                var value = _faker.Random.Double() * sign * double.MaxValue;
                values.Add(value);
            }
        }
        else if (dataType == "real")
        {
            for (var i = 0; i < count; i++)
            {
                // FIX: Same logic for float/real
                var sign = _faker.Random.Bool() ? -1 : 1;
                var value = (float)(_faker.Random.Double() * sign * float.MaxValue);
                values.Add(value);
            }
        }
        else
        {
             throw new InvalidOperationException($"CommunityDefaultDecimalGenerator cannot generate data for column with data type: {column.DataType}");
        }

        // Nullability Check (Deterministic)
        if (!column.IsNullable) return values;
        
        for (var i = 0; i < values.Count; i++)
        {
            //10% chance
            if (_faker.Random.Bool(0.1f)) values[i] = null;
        }

        return values;
    }
}