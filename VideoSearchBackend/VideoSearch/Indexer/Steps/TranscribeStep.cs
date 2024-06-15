using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Indexer.Abstract;
using VideoSearch.Vectorizer.Abstract;
using VideoSearch.VideoTranscriber.Abstract;

namespace VideoSearch.Indexer.Steps;

public class TranscribeStep(ILogger logger) : BaseIndexStep(logger)
{
    protected override VideoIndexStatus InitialStatus => VideoIndexStatus.VideoIndexed;
    protected override VideoIndexStatus TargetStatus => VideoIndexStatus.FullIndexed;

    protected override async Task InternalRun(VideoMeta record, IServiceProvider serviceProvider, IStorage storage, int nThread)
    {
        throw new NotImplementedException();

        var videoTranscriberService = serviceProvider.GetRequiredService<IVideoTranscriberService>();
        var vectorizer = serviceProvider.GetRequiredService<IVectorizerService>();

        // вызвать транскрибирование
        // вызвать для этих слов вектора и оставить только те что вернулись, значит для них есть вектора
        // записать ключевые слова в record.Stt (изначально это поле NULL)
        // удалить все существующие STT-индексы для этого видео - await storage.RemoveIndicesFor(record.Id, VideoIndexType.Stt);
        // для каждого ключевого слова создать новую запись с вектором:
        /*
        await storage.AddIndex(new VideoIndex
            {
                Id = Guid.NewGuid(),
                VideoMetaId = record.Id,
                Word = word,
                Vector = вектор из результата векторизации,
                ClusterSize = 1,
                Type = VideoIndexType.Stt
            }); 
         */
    }
}