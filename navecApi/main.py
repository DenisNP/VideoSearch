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




class ClosestWordsRequest(BaseModel):
    word: str
    similarity_threshold: float = 0.0

@app.post("/find_closest_words")
async def find_closest_words(request: ClosestWordsRequest):
    word = request.word.lower()  # Convert input word to lowercase
    threshold = request.similarity_threshold
    if word not in navec:
        return {"error": f'Word "{word}" not found in dictionary.'}
    
    word_vector = navec[word].reshape(1, -1)  # Reshape for cosine_similarity
    words = [w for w in navec.vocab.words if w != word]
    vectors = np.array([navec[w] for w in words])
    distances = cosine_similarity(word_vector, vectors).flatten()

    closest_words_and_distances = [{"word": words[i], "sim": float(distances[i])} for i in range(len(distances)) if distances[i] >= threshold]
    closest_words_and_distances.sort(key=lambda x: x["sim"], reverse=True)
    
    return closest_words_and_distances[:10]
