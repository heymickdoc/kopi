using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityDefaultTimeGenerator : IDataGenerator
{
    public string TypeName => "default_time";

    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var values = new List<object?>(count);

        if (!isUnique)
        {
            for (var i = 0; i < count; i++)
            {
                var hours = Random.Shared.Next(0, 24);
                var minutes = Random.Shared.Next(0, 60);
                var seconds = Random.Shared.Next(0, 60);
                var time = new TimeSpan(hours, minutes, seconds);
                values.Add(time);
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

        var uniqueTimes = new HashSet<TimeSpan>();

        //Calculate the *true* max we can generate based on time range
        var theoreticalMax = 24 * 60 * 60; // Total seconds in a day

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

        while (uniqueTimes.Count < targetCount && totalAttempts < maxAttempts)
        {
            var hours = Random.Shared.Next(0, 24);
            var minutes = Random.Shared.Next(0, 60);
            var seconds = Random.Shared.Next(0, 60);
            var time = new TimeSpan(hours, minutes, seconds);

            if (uniqueTimes.Add(time))
            {
                values.Add(time);
            }

            totalAttempts++;
        }

        if (uniqueTimes.Count < targetCount)
        {
            Msg.Write(MessageType.Warning,
                $"Generator '{TypeName}' for column '{column.ColumnName}' " +
                $"could only generate {uniqueTimes.Count} unique values out of requested {count} " +
                $"before reaching the maximum attempts of {maxAttempts}.");
        }

        return uniqueTimes.Cast<object?>().ToList();
    }
}