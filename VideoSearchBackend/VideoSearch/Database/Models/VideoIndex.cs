using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace VideoSearch.Database.Models;

public class VideoIndex
{
    public Guid Id { get; set; }
    public Guid VideoMetaId { get; set; }
    public string Word { get; set; }

    [Column(TypeName = "vector(300)")]
    public Vector Vector { get; set; }

    public int ClusterSize { get; set; }
    public VideoIndexType Type { get; set; }
}

public enum VideoIndexType
{
    Video,
    Stt
}