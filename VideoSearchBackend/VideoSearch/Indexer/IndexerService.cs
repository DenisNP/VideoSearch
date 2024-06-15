using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Indexer.Abstract;
using VideoSearch.Indexer.Steps;

namespace VideoSearch.Indexer;

public class IndexerService(ILogger<IndexerService> logger, IStorage storage, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private const int Parallel = 3;
    
    private readonly BaseIndexStep[] _steps =
    [
        new DescribeStep(logger),
        new TranslateStep(logger),
        new CreateIndexStep(logger),
        // new TranscribeStep(logger)
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting background indexer in 10 seconds...");
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        logger.LogInformation("Background indexer is running...");
        await storage.ClearQueued();

        await ExecuteParallelInitialIndex(stoppingToken);
        // await ExecuteBackgroundFullIndex(stoppingToken);
    }

    private async Task ExecuteParallelInitialIndex(CancellationToken stoppingToken)
    {
        IEnumerable<Task> tasks = Enumerable.Range(1, Parallel).Select(n => Task.Run(async () =>
        {
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            while (!stoppingToken.IsCancellationRequested)
            {
                var scopedStorage = scope.ServiceProvider.GetRequiredService<IStorage>();
                VideoMeta record = await scopedStorage.GetNextQueued();
                await TryIndex(record, scope, n - 1);
                await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken);
            }
        }, stoppingToken));

        await Task.WhenAll(tasks);
    }

    private async Task ExecuteBackgroundFullIndex(CancellationToken stoppingToken)
    {
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        while (!stoppingToken.IsCancellationRequested)
        {
            var scopedStorage = scope.ServiceProvider.GetRequiredService<IStorage>();
            VideoMeta record = await scopedStorage.GetNextPartialIndexed();
            await TryIndex(record, scope, -1);
            await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken);
        }
    }

    private async Task TryIndex(VideoMeta record, IServiceScope scope, int nThread)
    {
        try
        {
            if (record != null)
            {
                foreach (BaseIndexStep step in _steps)
                {
                    await step.Run(record, scope, nThread);
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            logger.LogError(e.StackTrace);
        }
    }
}