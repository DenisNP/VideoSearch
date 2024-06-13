namespace VideoSearch.Database.Models;

public class VideoMeta
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime StatusChangedAt { get; set; }
    public VideoIndexStatus Status { get; set; }
    public string Url { get; set; }
    public string RawDescription { get; set; }
    public string TranslatedDescription { get; set; }
    public List<string> Keywords { get; set; }
}

public enum VideoIndexStatus
{
    Added,
    Ready,
    Described,
    Translated,
    Indexed,
    Error
}