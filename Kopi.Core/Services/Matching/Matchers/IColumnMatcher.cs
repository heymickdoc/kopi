using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Matching.Matchers;

public interface IColumnMatcher
{
    /// <summary>
    /// Priority of the rule. Higher numbers run first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// The unique key for the generator to use (e.g., "credit_card").
    /// </summary>
    string GeneratorTypeKey { get; }

    /// <summary>
    /// The logic to check if this rule matches the column.
    /// </summary>
    /// <param name="column">The specific column we are trying to find a match for.</param>
    /// <param name="tableContext">The full table model, including all other columns.</param>
    bool IsMatch(ColumnModel column, TableModel tableContext);
}