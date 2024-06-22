namespace VideoSearch.Vectorizer.Models;

public record SimilarWordsRequest(string[] Words, double SimilarityThreshold);