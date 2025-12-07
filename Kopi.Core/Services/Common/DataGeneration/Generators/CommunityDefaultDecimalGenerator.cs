using Bogus;
using Kopi.Core.Models.Common;
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
                var value = _faker.Random.Decimal(minValue, maxValue);
                
                // Rounding is still needed to fit the scale
                value = Math.Round(value, scale);
                values.Add(value);
            }
        }
        // PostgreSQL: float8 is double precision
        else if (dataType == "float" || dataType == "float8" || dataType == "double precision")
        {
            for (var i = 0; i < count; i++)
            {
                var sign = _faker.Random.Bool() ? -1 : 1;
                var value = _faker.Random.Double() * sign * double.MaxValue;
                values.Add(value);
            }
        }
        // PostgreSQL: float4 is real (single precision)
        else if (dataType == "real" || dataType == "float4")
        {
            for (var i = 0; i < count; i++)
            {
                //Limit between 0 and 1000
                var value = _faker.Random.Float(0, 1000);
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