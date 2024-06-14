using System.Text.Json.Serialization;
using VideoSearch.Database.Models;

namespace VideoSearch.Indexer.Models;

public record SearchResult(VideoMeta Video, [property: JsonIgnore] List<double> Distances)
{
    private double? _avg;
    public double AvgDist => _avg ??= Distances.Average();
}