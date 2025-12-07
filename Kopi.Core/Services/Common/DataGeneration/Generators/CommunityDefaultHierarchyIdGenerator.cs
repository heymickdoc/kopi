using Bogus;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityDefaultHierarchyIdGenerator : IDataGenerator
{
    public string TypeName => "default_hierarchyid";
    
    private readonly Faker _faker = new(); 
    
    private static readonly List<string> _hierarchyIds =
    [
        "/1/", "/1/1/", "/1/1/1/", "/1/1/2/", "/1/1/3/", "/1/1/4/",
        "/1/1/4/1/", "/1/1/4/2/", "/1/1/4/3/", "/1/1/5/", "/1/1/5/1/",
        "/1/1/5/2/", "/1/1/6/", "/1/1/7/", "/2/", "/2/1/", "/2/2/", "/2/3/",
        "/2/4/", "/2/5/", "/2/6/", "/2/7/", "/2/8/", "/3/", "/3/1/",
        "/3/1/1/", "/3/1/1/1/", "/3/1/1/2/", "/3/1/1/3/", "/3/1/1/4/",
        "/3/1/1/5/", "/3/1/1/6/", "/3/1/1/7/", "/3/1/1/8/", "/3/1/1/9/",
        "/3/1/1/10/", "/3/1/1/11/", "/3/1/1/12/", "/3/1/2/", "/3/1/2/1/",
        "/3/1/2/2/", "/3/1/2/3/", "/3/1/2/4/", "/3/1/2/5/", "/3/1/2/6/",
        "/3/1/3/", "/3/1/3/1/", "/3/1/3/2/", "/3/1/3/3/", "/3/1/3/4/",
        "/3/1/3/5/", "/3/1/3/6/", "/3/1/3/7/", "/3/1/4/", "/3/1/4/1/",
        "/3/1/4/2/", "/3/1/4/3/", "/3/1/4/4/", "/3/1/4/5/", "/3/1/4/6/",
        "/3/1/5/", "/3/1/5/1/", "/3/1/5/2/", "/3/1/5/3/", "/3/1/5/4/",
        "/3/1/5/5/", "/3/1/5/6/", "/3/1/5/7/", "/3/1/5/8/", "/3/1/6/",
        "/3/1/6/1/", "/3/1/6/2/", "/3/1/6/3/", "/3/1/6/4/", "/3/1/6/5/",
        "/3/1/6/6/", "/3/1/7/", "/3/1/7/1/", "/3/1/7/2/", "/3/1/7/3/",
        "/3/1/7/4/", "/3/1/7/5/", "/3/1/7/6/", "/3/1/7/7/", "/3/1/7/8/",
        "/3/1/8/", "/3/1/8/1/", "/3/1/8/2/", "/3/1/8/3/", "/3/1/8/4/",
        "/3/1/8/5/", "/3/1/9/", "/3/1/9/1/", "/3/1/9/2/", "/3/1/9/3/",
        "/3/1/9/4/", "/3/1/9/5/", "/3/1/9/6/", "/3/1/9/7/", "/3/2/1/9/8/"
    ];

    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        if (count > _hierarchyIds.Count) count = _hierarchyIds.Count;
        var values = new List<object?>(count);
        
        var uniqueValues = _faker.Random.Shuffle(_hierarchyIds)
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