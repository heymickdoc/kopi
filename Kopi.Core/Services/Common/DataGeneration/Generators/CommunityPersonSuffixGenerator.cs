using Bogus;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityPersonSuffixGenerator : IDataGenerator
{
    public string TypeName => "person_suffix";
    
    private readonly Faker _faker = new(); 
    
    private readonly List<string> _suffixes = new()
    {
        "Jr.",
        "Sr.",
        "II",
        "III",
        "IV",
        "PhD",
        "MD",
        "Esq.",
        "CPA",
        "DDS",
        "DVM",
        "CFA",
        "RN",
        "GTF"
    };
    
    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        if (count > _suffixes.Count) count = _suffixes.Count;
        var values = new List<object?>(count);
        
        var uniqueValues = _faker.Random.Shuffle(_suffixes)
            .Take(count)
            .Cast<object?>()
            .ToList();
        values.AddRange(uniqueValues);
        
        if (!column.IsNullable) return values;

        for (var i = 0; i < values.Count; i++)
        {
            //10% chance
            if (_faker.Random.Bool(0.1f)) values[i] = null;
        }

        return values;
    }
}