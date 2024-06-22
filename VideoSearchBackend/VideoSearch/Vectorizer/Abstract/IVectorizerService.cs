using VideoSearch.Vectorizer.Models;

namespace VideoSearch.Vectorizer.Abstract;

public interface IVectorizerService
{
    public Task<List<VectorizedWord>> Vectorize(VectorizeRequest request, bool keepEmptyVectors = false);
    public Task<List<SimilarWordsResult>> FindSimilarWords(SimilarWordsRequest request);
}