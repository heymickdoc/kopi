using Bogus;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityDefaultBooleanGenerator : IDataGenerator
{
    public string TypeName => "default_boolean";
    
    private readonly Faker _faker = new(); 

    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var values = new List<object?>(count);
        
        for (var i = 0; i < count; i++) values.Add(_faker.Random.Bool());

        if (!column.IsNullable) return values;

        for (var i = 0; i < values.Count; i++)
        {
            //10% chance
            if (_faker.Random.Bool(0.1f)) values[i] = null;
        }

        return values;
    }
}