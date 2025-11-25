using Bogus;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityEmailAddressGenerator : IDataGenerator
{
    public string TypeName => "email_address";
    
    private readonly Faker _faker = new(); 
    
    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var maxLength = DataTypeHelper.GetMaxLength(column);
        
        if (!isUnique)
        {
            var values = new List<object?>(count);
            for (var i = 0; i < count; i++)
            {
                values.Add(GetTruncatedEmail(maxLength));
            }
            
            
            if (!column.IsNullable) return values;
            
            for (var i = 0; i < values.Count; i++)
            {
                //10% chance
                if (_faker.Random.Bool(0.1f)) values[i] = null;
            }
            
            return values;
        }
        
        var uniqueEmails = new HashSet<string>();
        
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
        while (uniqueEmails.Count < targetCount && totalAttempts < maxAttempts)
        {
            var email = GetTruncatedEmail(maxLength);
            uniqueEmails.Add(email); // Add() returns bool, but we just check Count
            totalAttempts++;
        }
        
        // 4. Handle failure (if we hit maxAttempts before targetCount)
        if (uniqueEmails.Count < targetCount)
        {
            Msg.Write(MessageType.Warning, 
                $"Generator '{TypeName}' for column '{column.ColumnName}' " +
                $"could only generate {uniqueEmails.Count} unique values out of requested {count} after {totalAttempts} attempts.");
        }
        
        return uniqueEmails.Cast<object?>().ToList();
    }
    
    private string GetTruncatedEmail(int maxLength)
    {
        var email = _faker.Internet.Email();
        if (email.Length <= maxLength)
        {
            return email;
        }
        // Truncate while preserving the domain
        var atIndex = email.IndexOf('@');
        if (atIndex == -1 || atIndex >= maxLength)
        {
            return email.Substring(0, maxLength);
        }
        var domain = email.Substring(atIndex);
        var localPartMaxLength = maxLength - domain.Length;
        var localPart = email.Substring(0, localPartMaxLength);
        return localPart + domain;
    }
}