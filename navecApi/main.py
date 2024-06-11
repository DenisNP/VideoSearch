from fastapi import FastAPI
from navec import Navec

app = FastAPI()
navec = Navec.load('navec_hudlit_v1_12B_500K_300d_100q.tar')


@app.get("/vector")
async def vector(word: str):
    w = str.lower(word)
    if w in navec:
        return [f.item() for f in navec[w]]
    else:
        return []
