// In a new file, e.g., Kopi.Core/Utilities/StringUtils.cs
using System.Text.RegularExpressions;

namespace Kopi.Core.Utilities;

public static class StringUtils
{
    // Regex to split on PascalCase, snake_case, spaces, OR hyphens
    private static readonly Regex WordSplitter = 
        new(@"(_)|(-)|(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])|(\s)");

    /// <summary>
    /// Splits a database identifier (like PascalCase or snake_case) into words.
    /// e.g., "ProductReviews" -> ["Product", "Reviews"]
    /// e.g., "product_reviews" -> ["product", "reviews"]
    /// e.g., "product-reviews" -> ["product", "reviews"]
    /// </summary>
    public static string[] SplitIntoWords(string identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return [];
        
        return WordSplitter.Split(identifier).Where(s => !string.IsNullOrEmpty(s) && s != "_").ToArray();
    }

    /// <summary>
    /// A simple algorithm to convert a plural word to singular.
    /// This is basic but effective for 90% of database naming conventions.
    /// </summary>
    public static string ToSingular(string word)
    {
        if (string.IsNullOrEmpty(word)) return word;

        // Don't modify short words
        if (word.Length <= 3) return word.ToLower();

        var lowerWord = word.ToLower();

        // 1. IRREGULARS & SINGULAR-LOOKING WORDS
        // "Status" ends in 's' but is singular.
        if (lowerWord == "status" || lowerWord.EndsWith("us")) return lowerWord;
        if (lowerWord == "goods") return "good";
        if (lowerWord == "wares") return "ware";
        if (lowerWord == "merchandise") return "merchandise";
        if (lowerWord == "people") return "person";
        
        // "Statuses" -> "Status"
        // "Buses" -> "Bus"
        if (lowerWord == "statuses" || lowerWord == "buses")
        {
            return lowerWord.Substring(0, lowerWord.Length - 2);
        }
        
        // Words ending in "ss" add "es" (Addresses -> Address, Classes -> Class)
        if (lowerWord.EndsWith("sses"))
        {
             return lowerWord.Substring(0, lowerWord.Length - 2);
        }
        
        // Words ending in "x", "ch", "sh" add "es" (Boxes -> Box, Searches -> Search)
        if (lowerWord.EndsWith("xes") || lowerWord.EndsWith("ches") || lowerWord.EndsWith("shes"))
        {
            return lowerWord.Substring(0, lowerWord.Length - 2);
        }

        // 2. STANDARD RULES

        // "Inventories" -> "Inventory"
        if (lowerWord.EndsWith("ies") && lowerWord.Length > 3)
        {
            return lowerWord.Substring(0, lowerWord.Length - 3) + "y";
        }

        // "Stages" -> "Stage", "Levels" -> "Level"
        // Standard 's' removal. 
        // Note: We already handled "ss" endings (like "Address") above, 
        // so this check !EndsWith("ss") is safe for standard words.
        if (lowerWord.EndsWith("s") && !lowerWord.EndsWith("ss") && lowerWord.Length > 3)
        {
            return lowerWord.Substring(0, lowerWord.Length - 1);
        }

        return lowerWord;
    }
}