from sentence_transformers import SentenceTransformer
from extract import extract_pdf_pages

PDF_PATH = "data/pdfs/SupportAuto.pdf"

print("Loading model...")
model = SentenceTransformer("BAAI/bge-m3")
print("Model loaded!")

print("Extracting PDF...")
pages = extract_pdf_pages(PDF_PATH)

print(f"Pages found: {len(pages)}")

vectors = []

for page in pages:
    embedding = model.encode(page["text"], normalize_embeddings=True)

    vectors.append({
        "page": page["page"],
        "text": page["text"],
        "embedding": embedding
    })

    print(f"Embedded page {page['page']}")

print("Done!")