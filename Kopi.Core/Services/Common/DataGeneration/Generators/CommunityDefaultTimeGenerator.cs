using Bogus;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityDefaultTimeGenerator : IDataGenerator
{
    public string TypeName => "default_time";
    
    private readonly Faker _faker = new(); 

    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        const int secondsInDay = 86400;
        
        if (!isUnique)
        {
            var values = new List<object?>(count);
            for (var i = 0; i < count; i++)
            {
                // 1 Random call instead of 3
                var totalSeconds = _faker.Random.Int(0, secondsInDay - 1);
                values.Add(TimeSpan.FromSeconds(totalSeconds));
            }

            if (!column.IsNullable) return values;

            for (var i = 0; i < values.Count; i++)
            {
                //10% chance
                if (_faker.Random.Bool(0.1f)) values[i] = null;
            }

            return values;
        }
        
        // --- UNIQUE PATH ---
        var uniqueTimes = new HashSet<TimeSpan>();
        var targetCount = Math.Min(count, secondsInDay); // Cap at max possible times

        if (secondsInDay < count)
        {
            Msg.Write(MessageType.Info, 
                $"Generator '{TypeName}' is capped at {targetCount} unique values.");
        }

        var maxAttempts = Math.Max(targetCount * 10, 100);
        var totalAttempts = 0;

        while (uniqueTimes.Count < targetCount && totalAttempts < maxAttempts)
        {
            var totalSeconds = _faker.Random.Int(0, secondsInDay - 1);
            var time = TimeSpan.FromSeconds(totalSeconds);

            uniqueTimes.Add(time); 
            totalAttempts++;
        }

        if (uniqueTimes.Count < targetCount)
        {
            Msg.Write(MessageType.Warning,
                $"Generator '{TypeName}' exhausted attempts. Generated {uniqueTimes.Count}/{targetCount}.");
        }

        return uniqueTimes.Cast<object?>().ToList();
    }
}