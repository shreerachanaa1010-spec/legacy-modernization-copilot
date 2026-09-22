from __future__ import annotations

import hashlib
import os
from pathlib import Path
from typing import Any

from embedding_provider import EmbeddingProvider, configured_embedding_provider
from retrieval_contract import SearchResult


class LanceDbRagStore:
    """Optional local LanceDB adapter. LanceDB stores data on the local filesystem."""

    def __init__(
        self,
        repo_root: str | Path,
        embedding_provider: EmbeddingProvider | None = None,
    ) -> None:
        try:
            import lancedb
        except ImportError as error:
            raise RuntimeError("LanceDB mode requires the open-source 'lancedb' package.") from error

        self.repo_root = Path(repo_root)
        self.database_path = os.getenv("RAG_LANCEDB_PATH", str(self.repo_root / ".lancedb"))
        self.embedding_provider = embedding_provider or configured_embedding_provider()
        if self.embedding_provider is None:
            raise RuntimeError("LanceDB mode requires a configured real embedding provider.")
        self.embedding_model = self.embedding_provider.model
        self._database = lancedb.connect(self.database_path)
        self._table = self._open_table()

    @property
    def is_ready(self) -> bool:
        return self._table is not None

    def _open_table(self) -> Any:
        table_name = "chunks"
        try:
            return self._database.open_table(table_name)
        except Exception:
            return self._database.create_table(
                table_name,
                data=[
                    {
                        "id": "seed",
                        "repository_id": self._repository_id,
                        "source_path": "",
                        "content": "",
                        "metadata": "{}",
                        "vector": [0.0] * self.embedding_provider.dimension,
                    }
                ],
            )

    @property
    def _repository_id(self) -> str:
        return hashlib.sha256(str(self.repo_root.resolve()).encode("utf-8")).hexdigest()

    def add_document(self, content: str, source_path: str, metadata: dict[str, Any] | None = None) -> None:
        payload = metadata or {}
        self._table.add(
            [
                {
                    "id": hashlib.sha256(f"{source_path}:{content}".encode("utf-8")).hexdigest(),
                    "repository_id": self._repository_id,
                    "source_path": source_path,
                    "content": content,
                    "metadata": payload,
                    "vector": self.embedding_provider.embed(content),
                }
            ]
        )

    def search(self, query: str, limit: int = 5) -> list[SearchResult]:
        rows = self._table.search(self.embedding_provider.embed(query)).limit(limit).to_list()
        return [
            SearchResult(
                source_path=row.get("source_path", ""),
                content=row.get("content", ""),
                metadata={
                    **(row.get("metadata") or {}),
                    "retrieval_method": "lancedb-vector",
                },
                score=float(row.get("_distance", 0.0)),
            )
            for row in rows
            if row.get("id") != "seed"
        ]
