using Bogus;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityDefaultBinaryGenerator : IDataGenerator
{
    public string TypeName => "default_binary";
    
    private readonly Faker _faker = new(); 
    
    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var actualMaxLength = DataTypeHelper.GetMaxLength(column);
        
        //Cap it at 1000 bytes to avoid excessive memory usage
        var maxLength = actualMaxLength > 0 && actualMaxLength <= 1000
            ? actualMaxLength
            : actualMaxLength > 1000
                ? 1000
                : 50; //Default to 50 bytes if not specified

        var values = new List<object?>(count);
        for (var i = 0; i < count; i++)
        {
            var length = _faker.Random.Int(1, maxLength);
            var byteArray = _faker.Random.Bytes(length);
            values.Add(byteArray);
        }

        
        if (!column.IsNullable) return values;

        for (var i = 0; i < values.Count; i++)
        {
            //10% chance
            if (_faker.Random.Bool(0.1f)) values[i] = null;
        }

        return values;
    }
}