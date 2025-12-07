using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Common.DataGeneration.Generators;
using Kopi.Core.Services.Matching.Matchers;

namespace Kopi.Core.Services.Common;


/// <summary>
/// The "Switchboard" service. It's injected with all rules (Matchers)
/// and all tools (Generators) and finds the right tool for the job.
/// </summary>
public class DataGeneratorService
{
    private readonly Dictionary<string, IDataGenerator> _generators;
    private readonly List<IColumnMatcher> _matchers;
    private readonly IDataGenerator _defaultGenerator;

    public DataGeneratorService(IEnumerable<IDataGenerator> allGenerators, IEnumerable<IColumnMatcher> allMatchers)
    {
        _generators = allGenerators.ToDictionary(g => g.TypeName);
        _matchers = allMatchers.OrderByDescending(m => m.Priority).ToList();
        
        // FAIL-SAFE: Look for your specific fallback generator
        if (_generators.TryGetValue("default_string", out var defGen))
        {
            _defaultGenerator = defGen;
        }
        else
        {
            // If even the default is missing, that's a critical configuration error.
            // You might want to grab the first available one or throw.
            _defaultGenerator = _generators.Values.First(); 
        }
    }
    
    
    /// <summary>
    /// Determines the appropriate generator type key for a given column within its table context.
    /// This is called ONCE per column per table.
    /// </summary>
    public string FindGeneratorTypeFor(ColumnModel column, TableModel table)
    {
        foreach (var matcher in _matchers)
        {
            if (matcher.IsMatch(column, table))
            {
                return matcher.GeneratorTypeKey;
            }
        }
        return "default_string"; // Fallback key
    }
    
    /// <summary>
    /// Gets the actual generator instance based on its type key.
    /// Returns the default generator if the key is not found.
    /// </summary>
    public IDataGenerator GetGeneratorByKey(string generatorTypeKey)
    {
        return _generators.TryGetValue(generatorTypeKey, out var generator) 
            ? generator 
            : _defaultGenerator;
    }
}
