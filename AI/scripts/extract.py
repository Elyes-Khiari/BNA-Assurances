import fitz  # PyMuPDF

def extract_pdf_pages(path):
    doc = fitz.open(path)
    pages = []

    for i, page in enumerate(doc):
        text = page.get_text().strip()

        if text:
            pages.append({
                "page": i + 1,
                "text": text
            })

    return pages