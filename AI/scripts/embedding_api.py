from fastapi import FastAPI
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer

app = FastAPI()

model = SentenceTransformer("BAAI/bge-m3")

class Request(BaseModel):
    text: str

@app.post("/embed")
def embed(req: Request):
    vector = model.encode(req.text, normalize_embeddings=True)
    return {"embedding": vector.tolist()}