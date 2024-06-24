import numpy as np
from fastapi import FastAPI, HTTPException
from navec import Navec
from typing import List, Dict, Union
from sklearn.metrics.pairwise import cosine_similarity
from pydantic import BaseModel


app = FastAPI()
navec = Navec.load('navec_hudlit_v1_12B_500K_300d_100q.tar')


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
        if word_lower not in navec:
            # results.append({"source": word, "result": [{"error": f'Word "{word}" not found in dictionary.'}]})
            continue

        word_vector = navec[word_lower].reshape(1, -1)
        words = [w for w in navec.vocab.words if w != word_lower]
        vectors = np.array([navec[w] for w in words])
        distances = cosine_similarity(word_vector, vectors).flatten()

        similar_words_and_distances = [{"word": words[i], "sim": float(distances[i])} for i in range(len(distances)) if distances[i] >= threshold]
        similar_words_and_distances.sort(key=lambda x: x["sim"], reverse=True)

        results.append({"source": word, "result": similar_words_and_distances})

    return results
