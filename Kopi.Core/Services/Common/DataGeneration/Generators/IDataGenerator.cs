using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

/// <summary>
/// Defines a "tool" to generate a specific type of fake data.
/// </summary>
public interface IDataGenerator
{
    /// <summary>
    /// The unique key for this generator (e.g., "credit_card").
    /// Must match a key from an IColumnMatcher.
    /// </summary>
    string TypeName { get; }

    /// <summary>
    /// Generates a batch of data for a specific column.
    /// </summary>
    /// <param name="column">The column metadata.</param>
    /// <param name="count">The number of values to generate.</param>
    /// <param name="isUnique">True if the data must be unique, e.g. Countries</param>
    /// <returns>A list of generated values.</returns>
    List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false);
}