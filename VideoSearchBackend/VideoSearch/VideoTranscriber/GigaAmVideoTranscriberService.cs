using System.Text;
using System.Text.Json;
using VideoSearch.VideoTranscriber.Abstract;
using VideoSearch.VideoTranscriber.Models;

namespace VideoSearch.VideoTranscriber;

public class GigaAmVideoTranscriberService(string? baseUrl) : IVideoTranscriberService
{
    private readonly string _baseUrl = baseUrl ?? throw 
        new Exception(nameof(GigaAmVideoTranscriberService) + " " + nameof(baseUrl) + " is null");

    public async Task<TranscribeVideoResponse> Transcribe(TranscribeVideoRequest request)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(1);

        string json = JsonSerializer.Serialize(
            request,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpResponseMessage httpResponse = await httpClient.PostAsync(_baseUrl + "/transcribe-keywords", content);

        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Request failed with status code {httpResponse.StatusCode}");
        }

        string jsonResponse = await httpResponse.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<TranscribeVideoResponse>(
            jsonResponse,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (result == null)
        {
            throw new Exception("Failed to deserialize response");
        }

        return result;
    }
}