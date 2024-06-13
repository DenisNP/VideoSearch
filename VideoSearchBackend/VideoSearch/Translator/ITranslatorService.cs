using VideoSearch.Translator.Models;

namespace VideoSearch.Translator;

public interface ITranslatorService
{
    public Task<TranslateResponse> Translate(TranslateRequest request);
}