using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Indexer.Abstract;
using VideoSearch.Indexer.Steps;

namespace VideoSearch.Indexer;

public class IndexerService(
    ILogger<IndexerService> logger,
    IStorage storage,
    IServiceScopeFactory serviceScopeFactory
    ) : BackgroundService
{
    private const int Parallel = 1;
    private const int SequentialErrorsAllowed = 10;
    private int _attempts = 0;

    private readonly BaseIndexStep[] _steps =
    [
        new TryFixErrorStep(logger),
        new DescribeStep(logger),
        new TranslateStep(logger),
        new CreateIndexStep(logger),
        new TranscribeStep(logger)
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting background indexer in 10 seconds...");
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        logger.LogInformation("Background indexer is running...");
        await storage.ClearProcessing();
        await ExecuteIndexing(stoppingToken);
    }

    private async Task ExecuteIndexing(CancellationToken stoppingToken)
    {
        IEnumerable<Task> tasks = Enumerable.Range(1, Parallel).Select(n => Task.Run(async () =>
        {
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            while (!stoppingToken.IsCancellationRequested)
            {
                var scopedStorage = scope.ServiceProvider.GetRequiredService<IStorage>();
                VideoMeta record = scopedStorage.LockNextUnprocessed();
                await TryIndex(record, scope, n - 1);
                await Task.Delay(TimeSpan.FromMilliseconds(50), stoppingToken);
            }
        }, stoppingToken));

        await Task.WhenAll(tasks);
    }

    /*private async Task ExecuteSequentialFullIndex(CancellationToken stoppingToken)
    {
        logger.LogInformation("Sequential full index running...");
        
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        while (!stoppingToken.IsCancellationRequested)
        {
            var scopedStorage = scope.ServiceProvider.GetRequiredService<IStorage>();
            VideoMeta record = await scopedStorage.GetNextPartialIndexed();
            await TryIndex(record, scope, -1);
            await Task.Delay(TimeSpan.FromMilliseconds(50), stoppingToken);
        }
    }*/

    private async Task TryIndex(VideoMeta record, IServiceScope scope, int nThread)
    {
        if (record != null)
        {
            foreach (BaseIndexStep step in _steps)
            {
                if (!await step.Run(record, scope, nThread))
                {
                    /*_attempts++;
                    if (_attempts >= SequentialErrorsAllowed)
                    {
                        await this.StopAsync(default);
                    }*/
                }
            }

            _attempts = 0;
        }
    }
}