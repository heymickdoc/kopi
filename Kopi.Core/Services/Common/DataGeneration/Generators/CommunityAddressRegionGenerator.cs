using Bogus;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityAddressRegionGenerator : IDataGenerator
{
    public string TypeName => "address_region";
    
    private readonly Faker _faker = new(); 
    
    private readonly List<string> _regionData = 
    [
        // Compass
        "North",
        "South",
        "East",
        "West",
        "Northeast",
        "Northwest",
        "Southeast",
        "Southwest",
        "Central",

        // US-centric
        "West Coast",
        "East Coast",
        "Midwest",
        "New England",
    
        // International
        "EMEA", // Europe, Middle East, Africa
        "APAC", // Asia-Pacific
        "LATAM", // Latin America
        "North America",
        "Europe",
        "Asia"
    ];
    
    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var values = new List<object?>(count);

        // 2. FIX: Added logic to handle the (common) isUnique = false case
        if (isUnique)
        {
            // Your original logic for unique values
            int takeCount = Math.Min(count, _regionData.Count);

            var uniqueValues = _faker.Random.Shuffle(_regionData)
                .Take(takeCount)
                .Cast<object?>()
                .ToList();
            
            values.AddRange(uniqueValues);
        }
        else
        {
            // Default path: Just pick 'count' random items with replacement
            for (int i = 0; i < count; i++)
            {
                values.Add(_faker.PickRandom(_regionData));
            }
        }
        
        // Your nullability logic is fine for the community edition.
        // It runs *after* generation, so it can make a "unique" value null.
        if (!column.IsNullable) return values;
        
        // Add a single null if unique and nullable, and we have space
        if (isUnique && column.IsNullable && count <= _regionData.Count && _faker.Random.Bool(0.1f))
        {
            // Only add a null if we haven't already hit the max (list size)
            if (values.Count < _regionData.Count)
            {
                values.Add(null);
            }
            // If we're full, just replace one at random
            else if (values.Count > 0)
            {
                // Use Faker to pick an index between 0 and Count-1
                var index = _faker.Random.Int(0, values.Count - 1);
                values[index] = null;
            }
        }
        // For non-unique, your 10% chance is perfect
        else if (!isUnique)
        {
            for (var i = 0; i < values.Count; i++)
            {
                //10% chance
                if (_faker.Random.Bool(0.1f)) values[i] = null;
            }
        }

        return values;
    }
}