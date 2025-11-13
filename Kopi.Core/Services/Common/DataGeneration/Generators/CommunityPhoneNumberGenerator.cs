using Bogus;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityPhoneNumberGenerator : IDataGenerator
{
    //var phoneNumber = _faker.Phone.PhoneNumber("(###) ###-####");
    public string TypeName => "phone_number";
    
    private readonly Faker _faker = new();

    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var maxLength = DataTypeHelper.GetMaxLength(column);

        if (!isUnique)
        {
            var values = new List<object?>(count);
            for (var i = 0; i < count; i++)
            {
                values.Add(GetTruncatedPhoneNumber(maxLength));
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

        var uniquePhoneNumbers = new HashSet<string>();

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

        while (uniquePhoneNumbers.Count < targetCount && totalAttempts < maxAttempts)
        {
            var phoneNumber = GetTruncatedPhoneNumber(maxLength);
            if (uniquePhoneNumbers.Add(phoneNumber))
            {
                // Successfully added a unique phone number
            }

            totalAttempts++;
        }

        if (uniquePhoneNumbers.Count < targetCount)
        {
            Msg.Write(MessageType.Warning,
                $"Generator '{TypeName}' for column '{column.ColumnName}' could only generate {uniquePhoneNumbers.Count} unique values out of requested {count} after {totalAttempts} attempts.");
        }

        return uniquePhoneNumbers.Cast<object?>().ToList();
    }
    
    private string GetTruncatedPhoneNumber(int maxLength)
    {
        var phoneNumber = _faker.Phone.PhoneNumber("(###) ###-####");
        if (phoneNumber.Length <= maxLength)
        {
            return phoneNumber;
        }

        return phoneNumber.Substring(0, maxLength);
    }
}