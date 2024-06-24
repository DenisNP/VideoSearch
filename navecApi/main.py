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
    
# Use NearestNeighbors for efficient similarity search
nn_model = NearestNeighbors(metric='cosine', algorithm='auto')
nn_model.fit(word_vectors)


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


class SimilarWordsRequest(BaseModel):
    words: List[str]
    similarityThreshold: float = 0.0


@app.post("/find_similar_words")
async def find_similar_words(request: SimilarWordsRequest):
    threshold = request.similarityThreshold
    results = []

    for word in request.words:
        word_lower = word.lower()
        if word_lower not in navec.vocab.words_set:
            results.append({"source": word, "result": [{"error": f'Word "{word}" not found in dictionary.'}]})
            continue

        word_vector = navec[word_lower].reshape(1, -1)

        distances, indices = nn_model.kneighbors(word_vector, n_neighbors=len(words))
        
        similar_words_and_distances = [{"word": words[idx], "sim": float(1 - distances[0][i])} for i, idx in enumerate(indices[0]) if 1 - distances[0][i] >= threshold and words[idx] != word_lower]
        similar_words_and_distances.sort(key=lambda x: x["sim"], reverse=True)

        results.append({"source": word, "result": similar_words_and_distances})

    return results