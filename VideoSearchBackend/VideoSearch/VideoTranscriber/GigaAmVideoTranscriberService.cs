using System.Text;
using System.Text.Json;
using VideoSearch.VideoTranscriber.Abstract;
using VideoSearch.VideoTranscriber.Models;

namespace VideoSearch.VideoTranscriber;

public class GigaAmVideoTranscriberService(string? baseUrl) : IVideoTranscriberService
{
    public bool IsActivated()
    {
        return !string.IsNullOrEmpty(baseUrl);
    }

    public async Task<TranscribeVideoResponse> Transcribe(TranscribeVideoRequest request)
    {
        if (!IsActivated())
        {
            throw new Exception(nameof(GigaAmVideoTranscriberService) + " " + nameof(baseUrl) + " is null");
        }

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(1);

        string json = JsonSerializer.Serialize(
            request,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpResponseMessage httpResponse = await httpClient.PostAsync(baseUrl + "/transcribe-keywords", content);

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