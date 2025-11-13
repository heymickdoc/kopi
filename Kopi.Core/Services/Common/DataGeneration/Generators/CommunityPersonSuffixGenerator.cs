using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityPersonSuffixGenerator : IDataGenerator
{
    public string TypeName => "person_suffix";
    
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
        
        var uniqueValues = _suffixes.OrderBy(x => Random.Shared.Next())
            .Take(count)
            .Cast<object?>();
        values.AddRange(uniqueValues);
        
        //Check for nullability. If so, make a maximum of 10% nulls
        if (!column.IsNullable) return values;

        for (var i = 0; i < values.Count; i++)
        {
            if (Random.Shared.NextDouble() < 0.5) //50% chance
            {
                values[i] = null;
            }
        }

        return values;
    }
}