using Bogus;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityPersonFullnameGenerator : IDataGenerator
{
    public string TypeName => "full_name";
    
    private readonly Faker _faker = new(); 
    
    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var maxLength = DataTypeHelper.GetMaxLength(column);
        
        if (!isUnique)
        {
            var values = new List<object?>(count);
            for (var i = 0; i < count; i++)
            {
                values.Add(GetTruncatedFullName(maxLength));
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
        
        var uniqueNames = new HashSet<string>();
        
        //Calculate the *true* max we can generate based on data type
        var theoreticalMax = DataTypeHelper.GetTheoreticalMaxCardinality(column, maxLength);
        
        // 2. Determine our target count
        // We can't generate more than the theoretical max OR the requested count.
        var targetCount = (int)Math.Min(count, theoreticalMax);
        
        if (theoreticalMax < count)
        {
            Msg.Write(MessageType.Info, 
                $"Generator '{TypeName}' for column '{column.ColumnName}' has a theoretical max of {theoreticalMax} unique values. " +
                $"Capping at {targetCount}.");
        }
        
        // 3. Set a safety break based on our target count to account for collisions
        // We try 10x as hard, with a minimum of 100, to account for Bogus collisions
        var maxAttempts = Math.Max(targetCount * 10, 100); 
        var totalAttempts = 0;
        
        // Loop *until* we hit our target, or we give up
        while (uniqueNames.Count < targetCount && totalAttempts < maxAttempts)
        {
            var name = GetTruncatedFullName(maxLength);
            uniqueNames.Add(name); // Add() returns bool, but we just check Count
            totalAttempts++;
        }
        
        // 4. Handle failure (if we hit maxAttempts before targetCount)
        if (uniqueNames.Count < targetCount)
        {
            Msg.Write(MessageType.Warning, 
                $"Generator '{TypeName}' for column '{column.ColumnName}' " +
                $"could only generate {uniqueNames.Count} unique values out of requested {count}.");
        }
        
        return uniqueNames.Cast<object?>().ToList();
    }
    
    private string GetTruncatedFullName(int maxLength)
    {
        var fullName = _faker.Name.FullName();
        if (fullName.Length > maxLength)
        {
            fullName = fullName.Substring(0, maxLength);
        }
        return fullName;
    }
}