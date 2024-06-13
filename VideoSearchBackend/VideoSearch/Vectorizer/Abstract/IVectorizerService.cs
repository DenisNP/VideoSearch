﻿using VideoSearch.Vectorizer.Models;

namespace VideoSearch.Vectorizer.Abstract;

public interface IVectorizerService
{
    public Task<List<VectorizedWord>> Vectorize(VectorizeRequest request);
}