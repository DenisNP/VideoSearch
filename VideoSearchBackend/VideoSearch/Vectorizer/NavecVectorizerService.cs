using System.Text;
using System.Text.Json;
using VideoSearch.Vectorizer.Abstract;
using VideoSearch.Vectorizer.Models;

namespace VideoSearch.Vectorizer;

public class NavecVectorizerService(string? baseUrl) : IVectorizerService
{
    private readonly string _baseUrl = baseUrl
        ?? throw new Exception(nameof(NavecVectorizerService) + " " + nameof(baseUrl) + " is null");

    public async Task<List<VectorizedWord>> Vectorize(VectorizeRequest request, bool keepEmptyVectors = false)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(5);

        var json = JsonSerializer.Serialize(
            request,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpResponseMessage httpResponse = await httpClient.PostAsync(_baseUrl + "/vectors", content);

        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Request failed with status code {httpResponse.StatusCode}");
        }

        string jsonResponse = await httpResponse.Content.ReadAsStringAsync();
        List<VectorizedWord> result = JsonSerializer.Deserialize<List<VectorizedWord>>(
            jsonResponse,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? throw new Exception("Failed to deserialize response");

        if (!keepEmptyVectors)
        {
            result.RemoveAll(v => v.Vector.Length == 0);
        }
        return result;
    }
}