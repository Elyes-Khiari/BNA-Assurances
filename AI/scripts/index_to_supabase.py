import os
from dotenv import load_dotenv
from supabase import create_client
from sentence_transformers import SentenceTransformer
import fitz  # PyMuPDF

# -------------------------
# LOAD ENV
# -------------------------
load_dotenv()

SUPABASE_URL = os.getenv("SUPABASE_URL")
SUPABASE_KEY = os.getenv("SUPABASE_KEY")

supabase = create_client(SUPABASE_URL, SUPABASE_KEY)

# -------------------------
# MODEL
# -------------------------
print("Loading embedding model...")
model = SentenceTransformer("BAAI/bge-m3")
print("Model loaded!")

# -------------------------
# PDF PATH
# -------------------------
PDF_PATH = "data/pdfs/SupportAuto.pdf"

doc = fitz.open(PDF_PATH)

for page_number, page in enumerate(doc, start=1):

    text = page.get_text().strip()

    if not text:
        continue

    embedding = model.encode(text, normalize_embeddings=True)

    supabase.table("documents").insert({
        "content": text,
        "page_number": page_number,
        "source": "SupportAuto.pdf",
        "embedding": embedding.tolist()
    }).execute()

    print(f"Inserted page {page_number}")

print("DONE 🚀")