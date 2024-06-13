using System.Text.RegularExpressions;

namespace VideoSearch;

public static class Utils
{
    private static readonly HashSet<string> Prepositions =
    [
        "а",
        "без",
        "бы",
        "в",
        "вне",
        "во",
        "для",
        "до",
        "за",
        "и",
        "из",
        "изо",
        "или",
        "иль",
        "к",
        "ко",
        "меж",
        "на",
        "над",
        "о",
        "об",
        "обо",
        "от",
        "ото",
        "по",
        "под",
        "при",
        "про",
        "с",
        "со",
        "то",
        "у"
    ];
    
    public static string[] Tokenize(this string s)
    {
        IEnumerable<string> tokens = Regex.Split(s, @"[^А-Яа-яЁёA-Z0-9a-z\-]+")
            .Select(t => t.Trim().Trim('-').ToLower())
            .Where(t => t != "" && !Prepositions.Contains(t) && !t.All(char.IsDigit));

        return tokens.ToArray();
    }

    public static double CosineDistance(double[] a, double[] b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Vectors a and b must have the same dimensionality.");
        }

        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += Math.Pow(a[i], 2);
            normB += Math.Pow(b[i], 2);
        }

        normA = Math.Sqrt(normA);
        normB = Math.Sqrt(normB);

        if (normA == 0 || normB == 0)
        {
            throw new ArgumentException("Input vectors must not be zero vectors.");
        }

        double cosineSimilarity = dotProduct / (normA * normB);
        double cosineDistance = 1.0 - cosineSimilarity;

        return cosineDistance;
    }
}