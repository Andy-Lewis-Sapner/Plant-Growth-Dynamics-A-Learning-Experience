using System.Linq;
using System.Text;

/// <summary>
/// Extension methods for string manipulation, including camel case separation, space removal, and article prefixing.
/// </summary>
public static class StringExtensions {
    /// <summary>
    /// Separates camel case strings by adding spaces before uppercase letters.
    /// </summary>
    /// <param name="str">The input string.</param>
    /// <returns>The string with spaces before uppercase letters, or original string if no changes needed.</returns>
    public static string SeparateCamelCase(this string str) {
        if (string.IsNullOrEmpty(str)) return str;

        int upperCount = str.Count(char.IsUpper);
        if (upperCount == 0 || (upperCount == 1 && char.IsUpper(str[0]))) return str;
        
        StringBuilder result = new StringBuilder(str.Length + upperCount);
        result.Append(str[0]);

        for (int i = 1; i < str.Length; i++) {
            if (char.IsUpper(str[i]))
                result.Append(' ');

            result.Append(str[i]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Removes all whitespace characters from the string.
    /// </summary>
    /// <param name="str">The input string.</param>
    /// <returns>The string with all whitespace removed.</returns>
    public static string RemoveSpaces(this string str) {
        return new string(str.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    /// <summary>
    /// Adds "a" or "an" before a word based on its first letter.
    /// </summary>
    /// <param name="word">The input word.</param>
    /// <returns>The word prefixed with "a" or "an".</returns>
    public static string AddAOrAn(this string word) {
        char firstChar = char.ToLower(word[0]);
        if (firstChar is 'a' or 'e' or 'i' or 'o' or 'u') 
            return "an " + word;
        return "a " + word;
    }
}