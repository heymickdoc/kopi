using Bogus;
using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityDefaultGeographyGenerator : IDataGenerator
{
    public string TypeName => "default_geography";
    
    private readonly Faker _faker = new(); 
    
    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var values = new List<object?>(count);
        for (var i = 0; i < count; i++)
        {
            var latitude = _faker.Address.Latitude();
            var longitude = _faker.Address.Longitude();
            var latLong = $"POINT({longitude} {latitude})";
            values.Add(latLong);
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
}