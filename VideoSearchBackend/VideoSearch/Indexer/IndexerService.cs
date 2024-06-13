using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Indexer.Abstract;
using VideoSearch.Indexer.Steps;

namespace VideoSearch.Indexer;

public class IndexerService(ILogger<IndexerService> logger, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private bool _isIndexingNow = false;
    private int _attempts = 0;

    private readonly BaseIndexStep[] _steps =
    [
        new DescribeStep(logger),
        new TranslateStep(logger),
        new CreateIndexStep(logger)
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_isIndexingNow)
            {
                continue;
            }

            if (_attempts >= 10)
            {
                break;
            }

            await TryIndex(scope);
            await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken);
        }
    }

    private async Task TryIndex(IServiceScope scope)
    {
        if (_isIndexingNow) return;
        _isIndexingNow = true;

        try
        {
            var storage = scope.ServiceProvider.GetRequiredService<IStorage>();
            VideoMeta record = await storage.GetNextNotIndexed();
            if (record != null)
            {
                foreach (BaseIndexStep step in _steps)
                {
                    await step.Run(record, scope);
                }
            }

            _attempts = 0;
            _isIndexingNow = false;
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            logger.LogError(e.StackTrace);
            _attempts++;
        }
    }
}