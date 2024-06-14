﻿using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;

namespace VideoSearch.Indexer.Abstract;

public abstract class BaseIndexStep(ILogger logger)
{
    protected abstract VideoIndexStatus InitialStatus { get; }
    protected abstract VideoIndexStatus TargetStatus { get; }

    public async Task<bool> Run(VideoMeta record, IServiceScope scope)
    {
        if (record.Status != InitialStatus)
        {
            return false;
        }

        var storage = scope.ServiceProvider.GetRequiredService<IStorage>();

        try
        {
            await InternalRun(record, scope.ServiceProvider, storage);
            record.Status = TargetStatus;
            record.StatusChangedAt = DateTime.UtcNow;
            await storage.UpdateMeta(record);

            return true;
        }
        catch (Exception e)
        {
            record.Status = VideoIndexStatus.Error;
            record.StatusChangedAt = DateTime.UtcNow;
            await storage.UpdateMeta(record);
            logger.LogError(e.Message);
            logger.LogError(e.StackTrace);
        }

        return false;
    }

    protected abstract Task InternalRun(VideoMeta record, IServiceProvider serviceProvider, IStorage storage);
}