using System.Text.RegularExpressions;

namespace VideoSearch;

public static class Utils
{
    public static string[] Tokenize(this string s)
    {
        IEnumerable<string> tokens = Regex.Split(s, @"[^А-Яа-яЁё0-9A-Za-z\-]+")
            .Select(t => t.Trim().Trim('-'))
            .Where(t => t != "");

        return tokens.ToArray();
    }
}