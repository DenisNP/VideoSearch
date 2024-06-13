from fastapi import FastAPI, HTTPException
from navec import Navec
from typing import List, Dict

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
