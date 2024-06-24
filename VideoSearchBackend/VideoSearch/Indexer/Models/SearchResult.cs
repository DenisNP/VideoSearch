using VideoSearch.Database.Models;

namespace VideoSearch.Indexer.Models;

public record SearchResult(VideoMeta Video, double Score);