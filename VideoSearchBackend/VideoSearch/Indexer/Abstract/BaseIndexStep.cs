using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;

namespace VideoSearch.Indexer.Abstract;

public abstract class BaseIndexStep(ILogger logger)
{
    protected abstract VideoIndexStatus InitialStatus { get; }
    protected abstract VideoIndexStatus TargetStatus { get; }

    protected IStorage Storage { get; set; }
    protected IServiceProvider ServiceProvider { get; set; }

    public async Task<bool> Run(VideoMeta record, IServiceScope scope)
    {
        if (record.Status != InitialStatus)
        {
            return false;
        }

        ServiceProvider = scope.ServiceProvider;
        Storage = ServiceProvider.GetRequiredService<IStorage>();

        try
        {
            await InternalRun(record);
            record.Status = TargetStatus;
            await Storage.UpdateMeta(record);

            return true;
        }
        catch (Exception e)
        {
            record.Status = VideoIndexStatus.Error;
            await Storage.UpdateMeta(record);
            logger.LogError(e.Message);
            logger.LogError(e.StackTrace);
        }

        return false;
    }

    protected abstract Task InternalRun(VideoMeta record);
}