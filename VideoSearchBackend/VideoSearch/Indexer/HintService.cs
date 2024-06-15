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
    private readonly DawgBuilder<bool> _builder = new();
    private readonly ThrottleDispatcher _throttleDispatcher = new(ThrottlePeriodMs);

    public async Task WarmUp()
    {
        List<VideoMeta> allMetas = await storage.GetAllIndexed();
        foreach (VideoMeta videoMeta in allMetas)
        {
            AddToIndex(videoMeta.Keywords, true);
        }

        var startTime = DateTime.UtcNow;
        _dawg = _builder.BuildDawg();
        var elapsed = DateTime.UtcNow - startTime;

        logger.LogInformation(
            "Hint indices built for {Count} keywords in {Time} ms",
            _dawg.GetNodeCount(),
            elapsed.TotalMilliseconds
        );
    }

    public void AddToIndex(IEnumerable<string> keywords, bool disableRebuild = false)
    {
        foreach (string keyword in keywords)
        {
            _builder.Insert(keyword, true);
        }

        if (!disableRebuild)
        {
            _throttleDispatcher.Throttle(() => { _dawg = _builder.BuildDawg(); });
        }
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