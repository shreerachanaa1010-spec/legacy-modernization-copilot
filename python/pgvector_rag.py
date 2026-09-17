from __future__ import annotations

import math
import os
from dataclasses import dataclass, field
from typing import Any


@dataclass
class VectorDocument:
    source_path: str
    content: str
    metadata: dict[str, Any] = field(default_factory=dict)
    vector: list[float] | None = None


class PgVectorRagStore:
    def __init__(self, connection_string: str | None = None) -> None:
        self.connection_string = connection_string or os.getenv(
            "PGVECTOR_CONNECTION_STRING",
            "postgresql://postgres:postgres@localhost:5432/legacy_rag",
        )
        self._client = None
        self._ready = False
        self._connect()

    def _connect(self) -> None:
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
                        embedding vector(384)
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
        tokens = [token.lower() for token in text.replace("\n", " ").split() if token.strip()]
        if not tokens:
            return [0.0] * 384

        counts: dict[str, float] = {}
        for token in tokens:
            counts[token] = counts.get(token, 0.0) + 1.0

        normalized = [value / math.sqrt(sum(v * v for v in counts.values())) for value in counts.values()]
        if not normalized:
            return [0.0] * 384

        vector = [0.0] * 384
        for index, value in enumerate(normalized[:384]):
            vector[index] = value
        return vector

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

    def search(self, query: str, limit: int = 5) -> list[dict[str, Any]]:
        if not self.is_ready:
            raise RuntimeError("pgvector database is not available. Use the local fallback store instead.")

        with self._client.cursor() as cur:
            cur.execute(
                """
                SELECT source_path, content, metadata
                FROM repo_vectors
                ORDER BY embedding <=> %s
                LIMIT %s
                """,
                (self._embedding(query), limit),
            )
            rows = cur.fetchall()

        return [
            {
                "source_path": source_path,
                "content": content,
                "metadata": metadata,
            }
            for source_path, content, metadata in rows
        ]
