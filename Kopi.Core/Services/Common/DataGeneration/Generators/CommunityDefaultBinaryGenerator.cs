using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityDefaultBinaryGenerator : IDataGenerator
{
    public string TypeName => "default_binary";
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
            var length = Random.Shared.Next(1, maxLength + 1); //Random length between 1 and maxLength
            var byteArray = new byte[length];
            Random.Shared.NextBytes(byteArray);
            values.Add(byteArray);
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