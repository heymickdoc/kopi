using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityDefaultIntegerGenerator : IDataGenerator
{
    public string TypeName => "default_integer";

    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var values = new List<object?>(count);
        var dataType = column.DataType.ToLower();

        // 1. Determine Limits (Min is always 0 for Community Edition)
        long min = 0;

        var max = dataType switch
        {
            "tinyint" => byte.MaxValue, // 255
            "smallint" => short.MaxValue, // 32767
            "int" => int.MaxValue, // 2B
            "bigint" => long.MaxValue, // 900 Bajillion or so :)
            _ => int.MaxValue
        };

        // 2. UNIQUE GENERATION (Strict Retry Logic)
        if (isUnique)
        {
            // Safety: Calculate total possible values to avoid infinite loops
            // Since min is 0, range is just max + 1.
            var rangeSize = (decimal)max + 1;
            
            if (count > rangeSize)
            {
                // If they ask for 300 unique tinyints (max 256), cap it.
                count = (int)rangeSize;
            }

            var uniqueSet = new HashSet<long>();

            // Retry Loop: Keep generating until we have enough unique items
            while (uniqueSet.Count < count)
            {
                var val = GenerateRandomLong(min, max);
                uniqueSet.Add(val); // Returns false if duplicate, so loop just continues
            }

            // Convert to specific type and add to list
            foreach (var val in uniqueSet)
            {
                values.Add(ConvertValue(val, dataType));
            }
        }
        // 3. STANDARD GENERATION (Fast)
        else
        {
            for (var i = 0; i < count; i++)
            {
                var val = GenerateRandomLong(min, max);
                values.Add(ConvertValue(val, dataType));
            }

            // Apply nulls strictly for non-unique columns
            if (column.IsNullable)
            {
                for (var i = 0; i < values.Count; i++)
                {
                    if (Random.Shared.NextDouble() < 0.1) // 10% chance
                    {
                        values[i] = null;
                    }
                }
            }
        }

        return values;
    }

    /// <summary>
    /// Generates a random long between min (inclusive) and max (inclusive).
    /// Handles the specific edge case where max is long.MaxValue.
    /// </summary>
    private long GenerateRandomLong(long min, long max)
    {
        // Random.Shared.NextInt64(min, max) is EXCLUSIVE of the upper bound.
        // If max is less than long.MaxValue, we can add 1 to include it.
        if (max < long.MaxValue)
        {
            return Random.Shared.NextInt64(min, max + 1);
        }
        
        // Edge case for long.MaxValue: direct calls often easier
        return Random.Shared.NextInt64(min, max); 
    }

    private object ConvertValue(long value, string dataType)
    {
        return dataType switch
        {
            "tinyint" => (byte)value,
            "smallint" => (short)value,
            "int" => (int)value,
            "bigint" => value,
            _ => (int)value
        };
    }
}