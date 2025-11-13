using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityDefaultBooleanGenerator : IDataGenerator
{
    public string TypeName => "default_boolean";

    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var results = new List<object?>();
        
        for (var i = 0; i < count; i++)
        {
            results.Add(Random.Shared.Next(0, 2) == 1);
        }

        return results;
    }
}