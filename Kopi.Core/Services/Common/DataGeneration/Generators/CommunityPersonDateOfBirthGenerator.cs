using Bogus;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityPersonDateOfBirthGenerator : IDataGenerator
{
    public string TypeName => "person_dob";

    // Uses the global seed if set in Enterprise mode
    private readonly Faker _faker = new();

    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var values = new List<object?>(count);

        // Non-Unique (Standard) Path
        if (!isUnique)
        {
            for (int i = 0; i < count; i++)
            {
                // Generate a past date between 18 and 90 years ago
                values.Add(_faker.Date.Past(72, DateTime.Now.AddYears(-18)));
            }

            if (column.IsNullable)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (_faker.Random.Bool(0.1f)) values[i] = null;
                }
            }
            return values;
        }

        // Unique Path
        var uniqueDates = new HashSet<DateTime>();
        
        // There are plenty of dates in a 72-year range, but we still cap safety
        var targetCount = count; 
        var maxAttempts = targetCount * 10;
        var attempts = 0;

        while (uniqueDates.Count < targetCount && attempts < maxAttempts)
        {
            var dob = _faker.Date.Past(72, DateTime.Now.AddYears(-18));
            
            // SQL Server "date" type doesn't have time, so we might need to strip time 
            // to ensure uniqueness is checked against the DB format.
            if (column.DataType.ToLower() == "date")
            {
                dob = dob.Date;
            }

            uniqueDates.Add(dob);
            attempts++;
        }

        return uniqueDates.Cast<object?>().ToList();
    }
}