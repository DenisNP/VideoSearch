using System.Text.RegularExpressions;
using VideoSearch.External.KMeans;

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

    private static double? _avgDocLenNgrams;

    public static double GetAverageDocLenNgrams()
    {
        _avgDocLenNgrams ??= double.TryParse(Environment.GetEnvironmentVariable("AVG_DOC_LEN_NGRAMS") ?? "", out double d)
            ? d
            : 150.0;

        return _avgDocLenNgrams.Value;
    }

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
        double cosineDistance = 1.0 - Math.Abs(cosineSimilarity);

        return cosineDistance;
    }

    public static T PickRandom<T>(this IList<T> list)
    {
        return list[Random.Shared.Next(list.Count)];
    }

    public static double AveragePointsCount(this Cluster[] clusters)
    {
        return clusters.Select(c => c.Points.Count).Average();
    }

    public static double GetLatinCharacterRatio(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return 0.0;
        }

        int latinCharCount = word.Count(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z');
        int totalCharCount = word.Length;

        return (double)latinCharCount / totalCharCount;
    }

    public static Dictionary<string, int> GetNgrams(IList<string> words, int n)
    {
        var ngrams = new Dictionary<string, int>();

        // Проходим по каждому слову из списка
        foreach (var word in words)
        {
            // Проходим по слову с шагом 1, чтобы извлечь все возможные N-граммы
            for (int i = 0; i <= word.Length - n; i++)
            {
                // Извлекаем подстроку длиной n символов
                string ngram = word.Substring(i, n);

                // Обновляем количество найденной N-граммы в словаре
                if (!ngrams.TryAdd(ngram, 1))
                {
                    ngrams[ngram]++;
                }
            }
        }

        return ngrams;
    }
    
    public static Dictionary<string, double> GetNgrams(IList<string> words, int n, Dictionary<string, double> lowerCoefficients)
    {
        var ngrams = new Dictionary<string, double>();

        // Проходим по каждому слову из списка
        foreach (var word in words)
        {
            double coefficient = lowerCoefficients.GetValueOrDefault(word, 1.0);

            // Проходим по слову с шагом 1, чтобы извлечь все возможные N-граммы
            for (int i = 0; i <= word.Length - n; i++)
            {
                // Извлекаем подстроку длиной n символов
                string ngram = word.Substring(i, n);

                // Обновляем количество найденной N-граммы в словаре
                if (!ngrams.TryAdd(ngram, 1.0 * coefficient))
                {
                    ngrams[ngram] += coefficient;
                }
            }
        }

        return ngrams;
    }

    public static double IdfBm(int totalDocs, int docsWithNgram)
    {
        return Math.Log(((double) totalDocs - docsWithNgram + 0.5) / ((double) docsWithNgram + 0.5) + 1.0);
    }

    public static double TfBm(double currentNgramsInDoc, double totalNgramsInDoc, double avgDocLen)
    {
        const double k1 = 1.5;
        const double b = 0.75;
        const double delta = 1;
        return ((double)currentNgramsInDoc * k1) /
            (currentNgramsInDoc * k1 + (1 - b + b * totalNgramsInDoc / avgDocLen)) + delta;
    }
}