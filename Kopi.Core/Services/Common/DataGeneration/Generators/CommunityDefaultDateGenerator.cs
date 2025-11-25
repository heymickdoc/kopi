using Bogus;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityDefaultDateGenerator : IDataGenerator
{
    public string TypeName => "default_date";
    
    private readonly Faker _faker = new(); 

    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var values = new List<object?>(count);
        var startDate = new DateTime(2000, 1, 1);
        var endDate = DateTime.Today;

        var totalDays = (endDate - startDate).Days;

        if (!isUnique)
        {
            for (var i = 0; i < count; i++)
            {
                var randomDaysToAdd = _faker.Random.Int(0, totalDays);
                var date = startDate.AddDays(randomDaysToAdd);
                values.Add(date);
            }

            
            if (!column.IsNullable) return values;
            
            for (var i = 0; i < values.Count; i++)
            {
                //10% chance
                if (_faker.Random.Bool(0.1f)) values[i] = null;
            }
            
            return values;
        }

        var uniqueDates = new HashSet<DateTime>();

        //Calculate the *true* max we can generate based on date range
        var theoreticalMax = totalDays + 1;

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
        // We try 10x as hard, with a minimum of 100, to account for collisions
        var maxAttempts = Math.Max(targetCount * 10, 100);
        var totalAttempts = 0;

        // Loop *until* we hit our target, or we give up
        while (uniqueDates.Count < targetCount && totalAttempts < maxAttempts)
        {
            var randomDaysToAdd = _faker.Random.Int(0, totalDays);
            var date = startDate.AddDays(randomDaysToAdd);
            uniqueDates.Add(date); // Add() returns bool, but we just check Count
            totalAttempts++;
        }

        // 4. Handle failure (if we hit maxAttempts before targetCount)
        if (uniqueDates.Count < targetCount)
        {
            Msg.Write(MessageType.Warning,
                $"Generator '{TypeName}' for column '{column.ColumnName}' " +
                $"could only generate {uniqueDates.Count} unique values out of requested {count} " +
                $"after {totalAttempts} attempts.");
        }
        
        return uniqueDates.Cast<object?>().ToList();
    }
}