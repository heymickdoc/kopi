using System.Security.Cryptography;
using System.Text;

namespace Kopi.Core.Utilities;

public class CryptoHelper
{
    /// <summary>
    /// Calculates a SHA256 hash of the input string and returns it as a Base64 string
    /// </summary>
    /// <param name="input">The connection string</param>
    /// <param name="removeSpecialChars">True if we want to remove the special chars like / " etc.</param>
    /// <returns>Base64 string</returns>
    public static string ComputeHash(string input, bool removeSpecialChars = false)
    {
        try
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            var output = Convert.ToBase64String(bytes);

            return removeSpecialChars ? RemoveSpecialChars(output) : output;
        }
        catch (ArgumentNullException ex)
        {
            Msg.Write(MessageType.Error, $"Cannot compute hash: input string is null: {ex.Message}");
            return string.Empty;
        }
        catch (Exception ex)
        {
            Msg.Write(MessageType.Error, $"Cannot compute hash: {ex.Message}");
            return string.Empty;
        }
    }
    
    /// <summary>
    /// Removes any characters from the input string that are not letters and not numbers.
    /// </summary>
    /// <param name="input">The input string</param>
    /// <returns>A string without special chars</returns>
    private static string RemoveSpecialChars(string input)
    {
        var outputString = new string(input.Where(char.IsLetterOrDigit).ToArray());
        return outputString.ToLower();
    }
}