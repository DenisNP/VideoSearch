using System.Text.Json.Serialization;

namespace VideoSearch.Database.Models;

public class VideoMeta
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime StatusChangedAt { get; set; }
    public VideoIndexStatus Status { get; set; }
    public bool Processing { get; set; } = false;
    public string Url { get; set; }
    [JsonIgnore]
    public string RawDescription { get; set; }
    [JsonIgnore]
    public string TranslatedDescription { get; set; }

    public List<string> SttKeywords { get; set; }
    public List<string> Keywords { get; set; }
    public List<string> Cloud { get; set; }
}

public enum VideoIndexStatus
{
    Queued = 0,
    Described = 2,
    Translated = 3,
    VideoIndexed = 4,
    FullIndexed = 99,
    Error = -1
}