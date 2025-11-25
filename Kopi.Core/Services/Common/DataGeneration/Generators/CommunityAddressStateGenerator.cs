using Bogus;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityAddressStateGenerator : IDataGenerator
{
    public string TypeName => "address_state";
    
    private readonly Faker _faker = new(); 
    
    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var maxLength = DataTypeHelper.GetMaxLength(column);
        
        // FIX 1: Smart Strategy Selection
        // If the column is tiny (2 or 3 chars), assume it wants a State Code (e.g. "WA", "NY").
        // If it's bigger, assume it wants the full name (e.g. "Washington", "New York").
        bool useAbbreviation = maxLength > 0 && maxLength <= 3;
        
        if (!isUnique)
        {
            var values = new List<object?>(count);
            for (var i = 0; i < count; i++)
            {
                // Use the smart helper
                values.Add(GetStateValue(maxLength, useAbbreviation));
            }
            
            if (!column.IsNullable) return values;
            
            for (var i = 0; i < values.Count; i++)
            {
                if (_faker.Random.Bool(0.1f)) values[i] = null;
            }
            
            return values;
        }
        
        var uniqueStates = new HashSet<string>();
        
        // Recalculate max based on strategy (50 codes vs 50 names)
        var theoreticalMax = 50; 
        var targetCount = (int)Math.Min(count, theoreticalMax);
        
        if (theoreticalMax < count)
        {
            Msg.Write(MessageType.Info, 
                $"Generator '{TypeName}' capped at {theoreticalMax} unique values (US States).");
        }
        
        var maxAttempts = Math.Max(targetCount * 10, 100); 
        var totalAttempts = 0;
        
        while (uniqueStates.Count < targetCount && totalAttempts < maxAttempts)
        {
            var state = GetStateValue(maxLength, useAbbreviation);
            uniqueStates.Add(state);
            totalAttempts++;
        }
        
        if (uniqueStates.Count < targetCount)
        {
            Msg.Write(MessageType.Warning, 
                $"Generator '{TypeName}' exhausted unique values. Generated {uniqueStates.Count}/{targetCount}.");
        }
        
        return uniqueStates.Cast<object?>().ToList();
    }
    
    // FIX 2: The Smart Helper
    private string GetStateValue(int maxLength, bool useAbbreviation)
    {
        // 1. Generate the appropriate format
        var state = useAbbreviation 
            ? _faker.Address.StateAbbr()  // "VT"
            : _faker.Address.State();     // "Vermont"

        // 2. Safety Truncate (Just in case)
        // Even if we chose Abbreviation, if the column is char(1) for some crazy reason, we must respect it.
        if (maxLength > 0 && state.Length > maxLength)
        {
            state = state.Substring(0, maxLength);
        }
        return state;
    }
}