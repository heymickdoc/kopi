using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityDefaultDecimalGenerator : IDataGenerator
{
    public string TypeName => "default_decimal";
    
    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        //Check what decimal type it is and return an appropriate value within the range for that type
        var dataType = column.DataType.ToLowerInvariant();
        
        if (dataType == "decimal" || dataType == "numeric")
        {
            //Use precision and scale to determine range
            var precision = column.NumericPrecision;
            var scale = column.NumericScale;
            var maxValue = (decimal)(Math.Pow(10, precision - scale) - Math.Pow(10, -scale));
            var minValue = -maxValue;

            var result = new List<object?>();
            for (var i = 0; i < count; i++)
            {
                var value = (decimal)(Random.Shared.NextDouble() * (double)(maxValue - minValue) + (double)minValue);
                value = Math.Round(value, scale);
                result.Add(value);
            }
            return result;
        }
        else if (dataType == "float")
        {
            var result = new List<object?>();
            for (var i = 0; i < count; i++)
            {
                var value = Random.Shared.NextDouble() * (Random.Shared.Next(0, 2) == 0 ? -1 : 1) * double.MaxValue;
                result.Add(value);
            }
            return result;
        }
        else if (dataType == "real")
        {
            var result = new List<object?>();
            for (var i = 0; i < count; i++)
            {
                var value = (float)(Random.Shared.NextDouble() * (Random.Shared.Next(0, 2) == 0 ? -1 : 1) * float.MaxValue);
                result.Add(value);
            }
            return result;
        }
        else if (dataType == "money" || dataType == "smallmoney")
        {
            var result = new List<object?>();
            for (var i = 0; i < count; i++)
            {
                var value = (decimal)(Random.Shared.NextDouble() * (Random.Shared.Next(0, 2) == 0 ? -1 : 1) * (double)92233720368547758.07m);
                value = Math.Round(value, 4);
                result.Add(value);
            }
            return result;
        }
        
        
        
        throw new InvalidOperationException($"CommunityDefaultDecimalGenerator cannot generate data for column with data type: {column.DataType}");
    }
}