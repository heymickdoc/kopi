using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityAddressRegionGenerator : IDataGenerator
{
    public string TypeName => "address_region";
    
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
            
            var uniqueValues = _regionData.OrderBy(x => Random.Shared.Next())
                .Take(takeCount)
                .Cast<object?>();
            
            values.AddRange(uniqueValues);
        }
        else
        {
            // Default path: Just pick 'count' random items with replacement
            for (int i = 0; i < count; i++)
            {
                values.Add(_regionData[Random.Shared.Next(_regionData.Count)]);
            }
        }
        
        // Your nullability logic is fine for the community edition.
        // It runs *after* generation, so it can make a "unique" value null.
        if (!column.IsNullable) return values;
        
        // Add a single null if unique and nullable, and we have space
        if (isUnique && column.IsNullable && count <= _regionData.Count && Random.Shared.NextDouble() < 0.1)
        {
            // Only add a null if we haven't already hit the max (list size)
            if (values.Count < _regionData.Count)
            {
                values.Add(null);
            }
            // If we're full, just replace one at random
            else if (values.Count > 0)
            {
                values[Random.Shared.Next(values.Count)] = null;
            }
        }
        // For non-unique, your 10% chance is perfect
        else if (!isUnique)
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (Random.Shared.NextDouble() < 0.1) // 10% chance
                {
                    values[i] = null;
                }
            }
        }

        return values;
    }
}