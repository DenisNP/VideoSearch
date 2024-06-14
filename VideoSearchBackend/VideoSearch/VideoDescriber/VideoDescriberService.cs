using System.Text;
using System.Text.Json;
using VideoSearch.VideoDescriber.Abstract;
using VideoSearch.VideoDescriber.Models;

namespace VideoSearch.VideoDescriber;

public class VideoDescriberService(string? baseUrls) : IVideoDescriberService
{
    private readonly string[] _baseUrls = baseUrls == null ?
        throw new Exception(nameof(VideoDescriberService) + " " + nameof(baseUrls) + " is null")
        : baseUrls.Split(";");

    public async Task<DescribeVideoResponse> Describe(DescribeVideoRequest request)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(5);

        string json = JsonSerializer.Serialize(
            request,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpResponseMessage httpResponse = await httpClient.PostAsync(_baseUrls.PickRandom() + "/describe", content);

        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Request failed with status code {httpResponse.StatusCode}");
        }

        string jsonResponse = await httpResponse.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<DescribeVideoResponse>(
            jsonResponse,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? throw new Exception("Failed to deserialize response");
    }
}