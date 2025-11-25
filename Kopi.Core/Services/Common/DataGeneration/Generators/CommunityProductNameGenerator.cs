using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityProductNameGenerator : IDataGenerator
{
    public string TypeName => "product_name";
    
    private readonly Bogus.Faker _faker = new();

    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        var maxLength = DataTypeHelper.GetMaxLength(column);

        if (!isUnique)
        {
            var values = new List<object?>(count);
            for (var i = 0; i < count; i++)
            {
                values.Add(GetTruncatedProductName(maxLength));
            }
            
            if (!column.IsNullable) return values;

            for (var i = 0; i < values.Count; i++)
            {
                //10% chance
                if (_faker.Random.Bool(0.1f)) values[i] = null;
            }

            return values;
        }

        var uniqueProductNames = new HashSet<string>();

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

        while (uniqueProductNames.Count < targetCount && totalAttempts < maxAttempts)
        {
            var productName = GetTruncatedProductName(maxLength);
            uniqueProductNames.Add(productName);
            totalAttempts++;
        }

        if (uniqueProductNames.Count < targetCount)
        {
            Msg.Write(MessageType.Warning,
                $"Generator '{TypeName}' for column '{column.ColumnName}' could only generate {uniqueProductNames.Count} unique values after {totalAttempts} attempts.");
        }
        
        return uniqueProductNames.Cast<object?>().ToList();
    }
    
    private string GetTruncatedProductName(int maxLength)
    {
        var productName = _faker.Commerce.ProductName();
        
        if (productName.Length > maxLength)
        {
            productName = productName.Substring(0, maxLength);
        }
        return productName;
    }
}