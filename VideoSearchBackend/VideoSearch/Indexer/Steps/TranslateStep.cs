﻿using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Indexer.Abstract;
using VideoSearch.Translator;
using VideoSearch.Translator.Models;

namespace VideoSearch.Indexer.Steps;

public class TranslateStep(ILogger logger) : BaseIndexStep(logger)
{
    private const double AllowedLatinRatio = 0.2;
    
    protected override VideoIndexStatus InitialStatus => VideoIndexStatus.Described;
    protected override VideoIndexStatus TargetStatus => VideoIndexStatus.Translated;

    protected override async Task InternalRun(VideoMeta record, IServiceProvider serviceProvider, IStorage storage, int nThread)
    {
        var translateService = serviceProvider.GetRequiredService<ITranslatorService>();

        var request = new TranslateRequest(
            Q: record.RawDescription.ToLower(),
            Source: "en",
            Target: "ru",
            Format: "text",
            Alternatives: 3
        );

        TranslateResponse result = await translateService.Translate(request);

        if (Utils.GetLatinCharacterRatio(result.TranslatedText) > AllowedLatinRatio)
        {
            throw new Exception("Text were not translated");
        }

        record.TranslatedDescription = result.TranslatedText + "\n\n" + string.Join("\n", result.Alternatives);
    }
}