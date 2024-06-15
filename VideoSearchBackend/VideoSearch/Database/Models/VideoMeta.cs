using System.Text.Json.Serialization;

namespace VideoSearch.Database.Models;

public class VideoMeta
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime StatusChangedAt { get; set; }
    public VideoIndexStatus Status { get; set; }
    public string Url { get; set; }
    [JsonIgnore]
    public string RawDescription { get; set; }
    [JsonIgnore]
    public string TranslatedDescription { get; set; }
    [JsonIgnore]
    public string Stt { get; set; }
    public List<string> Keywords { get; set; }
    public List<string> Centroids { get; set; }
}

public enum VideoIndexStatus
{
    Queued,
    Processing,
    Described,
    Translated,
    VideoIndexed,
    FullIndexed = 99,
    Error = -1
}