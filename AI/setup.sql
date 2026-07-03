-- Activez l'extension pgvector si elle n'est pas déjà activée
create extension if not exists vector;

-- Créez la table documents (si vous ne l'avez pas déjà fait)
-- La taille de vecteur pour BAAI/bge-m3 est de 1024
create table if not exists documents (
  id bigserial primary key,
  content text,
  page_number int,
  source text,
  embedding vector(1024)
);

-- Créez la fonction RPC match_documents pour la recherche de similarité
create or replace function match_documents (
  query_embedding vector(1024),
  match_threshold float,
  match_count int
)
returns table (
  id bigint,
  content text,
  page_number int,
  source text,
  similarity float
)
language sql stable
as $$
  select
    documents.id,
    documents.content,
    documents.page_number,
    documents.source,
    1 - (documents.embedding <=> query_embedding) as similarity
  from documents
  where 1 - (documents.embedding <=> query_embedding) > match_threshold
  order by documents.embedding <=> query_embedding
  limit match_count;
$$;
