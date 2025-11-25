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
        var dataType = column.DataType.ToLower();
        long min = 0;

        // 1. Pre-calculate Limits and Converter
        // We determine HOW to convert only once, not 1000 times.
        long max;
        Func<long, object> converter;

        switch (dataType)
        {
            case "tinyint":
                max = byte.MaxValue;
                converter = l => (byte)l;
                break;
            case "smallint":
                max = short.MaxValue;
                converter = l => (short)l;
                break;
            case "bigint":
                max = long.MaxValue;
                converter = l => l;
                break;
            default: // int
                max = int.MaxValue;
                converter = l => (int)l;
                break;
        }

        // 2. UNIQUE PATH
        if (isUnique)
        {
            // OPTIMIZATION: Small Range Strategy (Shuffle)
            // If the range is small (like tinyint), creating a pool and shuffling 
            // is O(N) and much faster than guessing random numbers (Retry Loop).
            if (max <= 10_000) // Arbitrary threshold where List overhead < Retry overhead
            {
                // Generate ALL possible numbers in range
                var pool = Enumerable.Range(0, (int)max + 1).ToList();

                // Shuffle deterministically and take what we need
                var values = _faker.Random.Shuffle(pool)
                    .Take(count)
                    .Select(i => converter((long)i))
                    .Cast<object?>()
                    .ToList();
                return values;
            }

            // LARGE Range Strategy (Retry Loop)
            // For Int/BigInt, the pool is too big to list, but collisions are rare.
            var uniqueSet = new HashSet<long>();

            // Safety: Cap count to max possible values (avoid infinite loop)
            // Note: We use decimal to hold the size because 'max' could be long.MaxValue
            if ((decimal)count > (decimal)max + 1) count = (int)max + 1;

            while (uniqueSet.Count < count)
            {
                // Use Bogus for deterministic Long generation
                uniqueSet.Add(_faker.Random.Long(min, max));
            }

            return uniqueSet.Select(l => converter(l)).ToList();
        }

        // 3. STANDARD PATH (Fastest)
        var results = new List<object?>(count);
        for (var i = 0; i < count; i++)
        {
            var val = _faker.Random.Long(min, max);
            results.Add(converter(val)); // Use the pre-calculated converter
        }

        if (!column.IsNullable) return results;

        for (var i = 0; i < results.Count; i++)
        {
            //10% chance
            if (_faker.Random.Bool(0.1f)) results[i] = null;
        }

        return results;
    }
}