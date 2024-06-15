using DawgSharp;
using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.External.DebounceThrottle;
using VideoSearch.Indexer.Abstract;

namespace VideoSearch.Indexer;

public class HintService(IStorage storage, ILogger<HintService> logger) : IHintService
{
    private const int ThrottlePeriodMs = 5000;
    
    private Dawg<bool> _dawg;
    private readonly ThrottleDispatcher _throttleDispatcher = new(ThrottlePeriodMs);

    public async Task Rebuild()
    {
        var startTime = DateTime.UtcNow;
        await RebuildSilent();
        var elapsed = DateTime.UtcNow - startTime;

        logger.LogInformation(
            "Hint indices rebuilt for {Count} keywords in {Time} ms",
            _dawg.GetNodeCount(),
            elapsed.TotalMilliseconds
        );
    }

    private async Task RebuildSilent()
    {
        var builder = new DawgBuilder<bool>();

        List<VideoMeta> allMetas = await storage.GetAllIndexed();
        foreach (VideoMeta videoMeta in allMetas)
        {
            foreach (string keyword in videoMeta.Keywords)
            {
                builder.Insert(keyword, true);
            }
        }

        _dawg = builder.BuildDawg();
    }

    public void NotifyIndexUpdated()
    {
        _throttleDispatcher.ThrottleAsync(async () =>
        {
            await RebuildSilent();
        });
    }

    public List<string> GetHintsFor(string query)
    {
        if (_dawg == null || string.IsNullOrEmpty(query))
        {
            return new List<string>();
        }

        string[] tokens = query.Tokenize();
        if (tokens.Length == 0)
        {
            return new List<string>();
        }

        IEnumerable<KeyValuePair<string, bool>> items = _dawg.MatchPrefix(tokens[^1]);
        return items.Select(i => i.Key).ToList();
    }
}