using Bogus;
using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityDefaultGuidGenerator : IDataGenerator
{
    public string TypeName => "default_guid";

    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var values = new List<object?>();
        for (var i = 0; i < count; i++)
        {
            values.Add(Guid.NewGuid());
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