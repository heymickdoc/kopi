using Bogus;
using Bogus.DataSets;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityCountryISO3Generator : IDataGenerator
{
    public string TypeName => "country_iso3";
    
    private readonly Faker _faker = new(); 
    
    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var maxLength = DataTypeHelper.GetMaxLength(column);
        
        if (!isUnique)
        {
            var values = new List<object?>(count);
            for (var i = 0; i < count; i++)
            {
                values.Add(GetTruncatedCountryISO3(maxLength));
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
        
        var uniqueCountries = new HashSet<string>();
        
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
        while (uniqueCountries.Count < targetCount && totalAttempts < maxAttempts)
        {
            var country = GetTruncatedCountryISO3(maxLength);
            uniqueCountries.Add(country); // Add() returns bool, but we just check Count
            totalAttempts++;
        }
        
        // 4. Handle failure (if we hit maxAttempts before targetCount)
        if (uniqueCountries.Count < targetCount)
        {
            Msg.Write(MessageType.Warning, 
                $"Generator '{TypeName}' for column '{column.ColumnName}' " +
                $"could only generate {uniqueCountries.Count} unique values out of requested {targetCount} after {totalAttempts} attempts.");
        }
        
        return uniqueCountries.Cast<object?>().ToList();
    }
    
    private string GetTruncatedCountryISO3(int maxLength)
    {
        var countryISO3 = _faker.Address.CountryCode(Iso3166Format.Alpha3);
        if (countryISO3.Length > maxLength)
        {
            countryISO3 = countryISO3.Substring(0, maxLength);
        }
        return countryISO3;
    }
}