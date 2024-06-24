import numpy as np
from fastapi import FastAPI, HTTPException
from navec import Navec
from typing import List, Dict, Union
from sklearn.metrics.pairwise import cosine_similarity
from sklearn.neighbors import NearestNeighbors
from pydantic import BaseModel


app = FastAPI()
navec = Navec.load('navec_hudlit_v1_12B_500K_300d_100q.tar')

# Precompute all vectors and store them in an array
words = list(navec.vocab.words)
word_vectors = np.array([navec[word] for word in words])

# Create a set for fast membership checking
words_set = set(words)

# Use NearestNeighbors with the 'brute' algorithm and 'cosine' metric for similarity search
nn_model = NearestNeighbors(metric='cosine', algorithm='brute')
nn_model.fit(word_vectors)


class SimilarWordsRequest(BaseModel):
    words: List[str]
    similarity_threshold: float = 0.0


@app.post("/find_similar_words")
async def find_similar_words(request: SimilarWordsRequest):
    threshold = request.similarity_threshold
    results = []

    for word in request.words:
        word_lower = word.lower()
        if word_lower not in words_set:
            # results.append({"source": word, "result": [{"error": f'Word "{word}" not found in dictionary.'}]})
            continue

        # Retrieve the vector for the current word
        word_vector = navec[word_lower].reshape(1, -1)

        # Perform a nearest neighbor search for the current word vector
        distances, indices = nn_model.kneighbors(word_vector, n_neighbors=len(words))

        # Convert distance to similarity
        similarities = 1 - distances.flatten()

        # Filter out the words below the similarity threshold and sort them
        similar_words_and_distances = [
            {"word": words[idx], "sim": float(similarities[i])}
            for i, idx in enumerate(indices[0])
            if similarities[i] >= threshold and words[idx] != word_lower
        ]
        similar_words_and_distances.sort(key=lambda x: x["sim"], reverse=True)

        results.append({"source": word, "result": similar_words_and_distances})

    return results




@app.post("/vectors")
async def vectors(request: Dict[str, List[str]]):
    if "words" not in request:
        raise HTTPException(status_code=400, detail="Request body must contain 'words' field")

    words = request["words"]
    response = []
    for word in words:
        w = word.lower()
        if w in navec:
            vector = [f.item() for f in navec[w]]
        else:
            vector = []
        response.append({'word': word, 'vector': vector})
    return response
