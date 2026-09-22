from __future__ import annotations

import os
from typing import Any

from embedding_provider import EmbeddingProvider, configured_embedding_provider
from retrieval_contract import SearchResult


class PgVectorRagStore:
    def __init__(
        self,
        connection_string: str | None = None,
        embedding_provider: EmbeddingProvider | None = None,
    ) -> None:
        self.connection_string = connection_string or os.getenv(
            "PGVECTOR_CONNECTION_STRING",
            "postgresql://postgres:postgres@localhost:5432/legacy_rag",
        )
        self.embedding_provider = embedding_provider or configured_embedding_provider()
        self._client = None
        self._ready = False
        self._connect()

    def _connect(self) -> None:
        if self.embedding_provider is None:
            return

        try:
            import psycopg

            self._client = psycopg.connect(self.connection_string)
            with self._client.cursor() as cur:
                cur.execute("CREATE EXTENSION IF NOT EXISTS vector")
                cur.execute(
                    """
                    CREATE TABLE IF NOT EXISTS repo_vectors (
                        id SERIAL PRIMARY KEY,
                        source_path TEXT NOT NULL,
                        content TEXT NOT NULL,
                        metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
                        embedding vector({self.embedding_provider.dimension})
                    )
                    """
                )
            self._client.commit()
            self._ready = True
        except Exception:
            self._client = None
            self._ready = False

    @property
    def is_ready(self) -> bool:
        return self._ready and self._client is not None

    def _embedding(self, text: str) -> list[float]:
        if self.embedding_provider is None:
            raise RuntimeError(
                "A real embedding provider is required for pgvector. "
                "Set RAG_EMBEDDING_PROVIDER=gemini or use RAG_STORE_MODE=sqlite."
            )
        return self.embedding_provider.embed(text)

    def add_document(self, content: str, source_path: str, metadata: dict[str, Any] | None = None) -> None:
        if not self.is_ready:
            raise RuntimeError("pgvector database is not available. Use the local fallback store instead.")

        with self._client.cursor() as cur:
            cur.execute(
                """
                INSERT INTO repo_vectors (source_path, content, metadata, embedding)
                VALUES (%s, %s, %s, %s)
                """,
                (
                    source_path,
                    content,
                    metadata or {},
                    self._embedding(content),
                ),
            )
        self._client.commit()

    def search(self, query: str, limit: int = 5) -> list[SearchResult]:
        if not self.is_ready:
            raise RuntimeError("pgvector database is not available. Use the local fallback store instead.")

        with self._client.cursor() as cur:
            cur.execute(
                """
                  SELECT source_path, content, metadata,
                      1 - (embedding <=> %s) AS score
                FROM repo_vectors
                ORDER BY embedding <=> %s
                LIMIT %s
                """,
                  (self._embedding(query), self._embedding(query), limit),
            )
            rows = cur.fetchall()

        return [
            SearchResult(
                source_path=source_path,
                content=content,
                metadata=metadata or {},
                score=float(score),
            )
            for source_path, content, metadata, score in rows
        ]
