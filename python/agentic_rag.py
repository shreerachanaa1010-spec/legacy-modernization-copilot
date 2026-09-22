from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import sqlite3
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from pgvector_rag import PgVectorRagStore
from lancedb_rag import LanceDbRagStore
from embedding_provider import EmbeddingProvider, configured_embedding_provider
from retrieval_contract import SearchResult


class LocalVectorStore:
    def __init__(self) -> None:
        self._documents: list[tuple[str, str, dict[str, Any]]] = []

    @property
    def is_ready(self) -> bool:
        return True

    def add_document(self, content: str, source_path: str, metadata: dict[str, Any] | None = None) -> None:
        self._documents.append((content, source_path, metadata or {}))

    def search(self, query: str, limit: int = 5) -> list[SearchResult]:
        terms = set(re.findall(r"[A-Za-z0-9_]+", query.lower()))
        scored: list[tuple[float, int]] = []

        for idx, (content, source_path, _) in enumerate(self._documents):
            searchable = f"{source_path} {content}".lower()
            score = sum(searchable.count(term) for term in terms if len(term) > 1)
            if score > 0:
                scored.append((float(score), idx))

        scored.sort(key=lambda item: item[0], reverse=True)

        results: list[SearchResult] = []
        for score, idx in scored[:limit]:
            content, source_path, metadata = self._documents[idx]
            results.append(
                SearchResult(
                    source_path=source_path,
                    content=content,
                    metadata=metadata,
                    score=score,
                )
            )

        return results


class SQLiteRagStore:
    def __init__(self, repo_root: str | Path | None = None, database_path: str | None = None) -> None:
        self.repo_root = Path(repo_root) if repo_root is not None else Path.cwd()
        self.repository_id = hashlib.sha256(str(self.repo_root.resolve()).encode("utf-8")).hexdigest()
        self.embedding_provider: EmbeddingProvider | None = configured_embedding_provider()
        self.embedding_model = self.embedding_provider.model if self.embedding_provider else None
        self.sqlite_path = database_path or os.getenv(
            "RAG_SQLITE_PATH",
            str(self.repo_root / ".legacy_rag.sqlite3"),
        )
        self._connection = sqlite3.connect(self.sqlite_path)
        self._connection.row_factory = sqlite3.Row
        self._initialize_schema()

    @property
    def is_ready(self) -> bool:
        return self._connection is not None

    def _initialize_schema(self) -> None:
        with self._connection:
            self._connection.execute(
                """
                CREATE TABLE IF NOT EXISTS repositories (
                    repository_id TEXT PRIMARY KEY,
                    root_path TEXT NOT NULL,
                    last_commit_sha TEXT,
                    created_at TEXT NOT NULL
                )
                """
            )
            self._connection.execute(
                """
                CREATE TABLE IF NOT EXISTS chunks (
                    chunk_id TEXT PRIMARY KEY,
                    repository_id TEXT NOT NULL REFERENCES repositories(repository_id),
                    source_path TEXT NOT NULL,
                    content TEXT NOT NULL,
                    content_hash TEXT NOT NULL,
                    symbol_name TEXT,
                    namespace TEXT,
                    source_type TEXT NOT NULL,
                    line_start INTEGER,
                    line_end INTEGER,
                    embedding_model TEXT,
                    embedding_dim INTEGER,
                    embedding BLOB,
                    indexed_at TEXT NOT NULL,
                    UNIQUE(repository_id, source_path, content_hash, embedding_model)
                )
                """
            )
            self._connection.execute(
                """
                CREATE VIRTUAL TABLE IF NOT EXISTS chunks_fts USING fts5(
                    source_path, symbol_name, content,
                    content='chunks', content_rowid='rowid'
                )
                """
            )
            self._connection.executescript(
                """
                CREATE TRIGGER IF NOT EXISTS chunks_fts_insert AFTER INSERT ON chunks BEGIN
                    INSERT INTO chunks_fts(rowid, source_path, symbol_name, content)
                    VALUES (new.rowid, new.source_path, new.symbol_name, new.content);
                END;
                CREATE TRIGGER IF NOT EXISTS chunks_fts_delete AFTER DELETE ON chunks BEGIN
                    INSERT INTO chunks_fts(chunks_fts, rowid, source_path, symbol_name, content)
                    VALUES ('delete', old.rowid, old.source_path, old.symbol_name, old.content);
                END;
                CREATE TRIGGER IF NOT EXISTS chunks_fts_update AFTER UPDATE ON chunks BEGIN
                    INSERT INTO chunks_fts(chunks_fts, rowid, source_path, symbol_name, content)
                    VALUES ('delete', old.rowid, old.source_path, old.symbol_name, old.content);
                    INSERT INTO chunks_fts(rowid, source_path, symbol_name, content)
                    VALUES (new.rowid, new.source_path, new.symbol_name, new.content);
                END;
                """
            )
            self._connection.execute(
                """
                INSERT INTO repositories (repository_id, root_path, created_at)
                VALUES (?, ?, ?)
                ON CONFLICT(repository_id) DO UPDATE SET root_path = excluded.root_path
                """,
                (self.repository_id, str(self.repo_root.resolve()), datetime.now(timezone.utc).isoformat()),
            )

    def add_document(self, content: str, source_path: str, metadata: dict[str, Any] | None = None) -> None:
        payload = metadata or {}
        embedding = self.embedding_provider.embed(content) if self.embedding_provider else None
        stored_embedding_model = self.embedding_model or "none"
        content_hash = hashlib.sha256(content.encode("utf-8")).hexdigest()
        chunk_id = hashlib.sha256(
            f"{self.repository_id}:{source_path}:{payload.get('chunk_index', 0)}:{content_hash}:{stored_embedding_model}".encode("utf-8")
        ).hexdigest()
        indexed_at = datetime.now(timezone.utc).isoformat()

        with self._connection:
            self._connection.execute(
                """
                INSERT INTO chunks (
                    chunk_id, repository_id, source_path, content, content_hash,
                    symbol_name, namespace, source_type, line_start, line_end,
                    embedding_model, embedding_dim, embedding, indexed_at
                )
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                ON CONFLICT(repository_id, source_path, content_hash, embedding_model)
                DO UPDATE SET
                    chunk_id = excluded.chunk_id,
                    content = excluded.content,
                    symbol_name = excluded.symbol_name,
                    namespace = excluded.namespace,
                    source_type = excluded.source_type,
                    line_start = excluded.line_start,
                    line_end = excluded.line_end,
                    embedding = excluded.embedding,
                    indexed_at = excluded.indexed_at
                """,
                (
                    chunk_id,
                    self.repository_id,
                    source_path,
                    content,
                    content_hash,
                    payload.get("symbol_name"),
                    payload.get("namespace"),
                    payload.get("source_type", "related-source"),
                    payload.get("line_start"),
                    payload.get("line_end"),
                    stored_embedding_model,
                    len(embedding) if embedding else None,
                    sqlite3.Binary(json.dumps(embedding).encode("utf-8")) if embedding else None,
                    indexed_at,
                ),
            )

    def search(self, query: str, limit: int = 5) -> list[SearchResult]:
        terms = [term for term in re.findall(r"[A-Za-z0-9_]+", query.lower()) if len(term) > 1]
        fts_query = " OR ".join(terms)
        if not fts_query:
            return []

        try:
            rows = self._connection.execute(
                """
                SELECT chunks.source_path, chunks.content, chunks.source_type,
                       chunks.symbol_name, chunks.line_start, chunks.line_end,
                       chunks.content_hash, chunks.embedding, bm25(chunks_fts) AS rank
                FROM chunks_fts
                JOIN chunks ON chunks.rowid = chunks_fts.rowid
                WHERE chunks_fts MATCH ? AND chunks.repository_id = ?
                ORDER BY rank
                LIMIT ?
                """,
                (fts_query, self.repository_id, limit),
            ).fetchall()
        except sqlite3.OperationalError:
            rows = []

        if not rows:
            like_clauses = " OR ".join(
                "lower(content) LIKE ? OR lower(source_path) LIKE ?" for _ in terms
            )
            like_parameters = [value for term in terms for value in (f"%{term}%", f"%{term}%")]
            rows = self._connection.execute(
                f"""
                SELECT source_path, content, source_type, symbol_name,
                       line_start, line_end, content_hash, NULL AS embedding, 0.0 AS rank
                FROM chunks
                WHERE repository_id = ? AND ({like_clauses})
                LIMIT ?
                """,
                [self.repository_id, *like_parameters, limit],
            ).fetchall()

        query_embedding = self.embedding_provider.embed(query) if self.embedding_provider else None
        lexical_scores = [max(0.0, -float(row["rank"])) for row in rows]
        lexical_max = max(lexical_scores, default=0.0) or 1.0
        results = []
        for row, lexical_score in zip(rows, lexical_scores):
            vector_score = 0.0
            if query_embedding and row["embedding"]:
                stored = json.loads(bytes(row["embedding"]).decode("utf-8"))
                vector_score = self._cosine_similarity(query_embedding, stored)
            score = 0.7 * (lexical_score / lexical_max) + 0.3 * max(0.0, vector_score)
            results.append(SearchResult(
                source_path=row["source_path"],
                content=row["content"],
                metadata={
                    "source_type": row["source_type"],
                    "symbol_name": row["symbol_name"],
                    "line_start": row["line_start"],
                    "line_end": row["line_end"],
                    "content_hash": row["content_hash"],
                    "retrieval_method": "sqlite-fts5+vector" if query_embedding else "sqlite-fts5",
                    "lexical_score": lexical_score,
                    "vector_score": vector_score,
                },
                score=score,
            ))
        return sorted(results, key=lambda result: result.score, reverse=True)

    def prune_missing(self, source_paths: set[str]) -> None:
        with self._connection:
            rows = self._connection.execute(
                "SELECT DISTINCT source_path FROM chunks WHERE repository_id = ?",
                (self.repository_id,),
            ).fetchall()
            for row in rows:
                if row["source_path"] not in source_paths:
                    self._connection.execute(
                        "DELETE FROM chunks WHERE repository_id = ? AND source_path = ?",
                        (self.repository_id, row["source_path"]),
                    )

    @staticmethod
    def _cosine_similarity(left: list[float], right: list[float]) -> float:
        if len(left) != len(right):
            raise ValueError(f"Embedding dimension mismatch: {len(left)} != {len(right)}")
        dot = sum(a * b for a, b in zip(left, right))
        left_norm = sum(value * value for value in left) ** 0.5
        right_norm = sum(value * value for value in right) ** 0.5
        return dot / (left_norm * right_norm) if left_norm and right_norm else 0.0

class GeminiGenerator:
    def __init__(self, api_key: str | None = None, model: str = "gemini-2.0-flash") -> None:
        self.api_key = api_key or os.getenv("GEMINI_API_KEY", "")
        self.model = model
        self._client = None

        if self.api_key:
            try:
                from google import genai

                self._client = genai.Client(api_key=self.api_key)
            except Exception:
                self._client = None

    def generate(self, prompt: str) -> str:
        if not self.api_key or self._client is None:
            return (
                "Gemini is not configured. Set GEMINI_API_KEY to enable repository-grounded generation."
            )

        try:
            response = self._client.models.generate_content(model=self.model, contents=prompt)
            return getattr(response, "text", str(response))
        except Exception:
            return "Gemini generation failed. Verify the API key and connectivity."


class AgenticRagPipeline:
    def __init__(
        self,
        repo_root: str,
        chunk_size: int = 600,
        chunk_overlap: int = 100,
        gemini_api_key: str | None = None,
        model: str = "gemini-2.0-flash",
        vector_store: Any | None = None,
    ) -> None:
        self.repo_root = Path(repo_root)
        self.chunk_size = chunk_size
        self.chunk_overlap = chunk_overlap

        preferred_store = vector_store
        store_mode = os.getenv("RAG_STORE_MODE", "sqlite").strip().lower()
        if preferred_store is None:
            if store_mode in {"", "sqlite", "local", "default"}:
                preferred_store = SQLiteRagStore(repo_root=self.repo_root)
            elif store_mode in {"required-sqlite", "required_sqlite"}:
                preferred_store = SQLiteRagStore(repo_root=self.repo_root)
                store_mode = "sqlite"
            elif store_mode in {"pgvector", "required-pgvector", "required_pgvector"}:
                preferred_store = PgVectorRagStore()
                if not preferred_store.is_ready:
                    raise RuntimeError(
                        "pgvector is required but unavailable; no fallback was applied. "
                        "Set RAG_STORE_MODE=sqlite for the local default."
                    )
            elif store_mode in {"lancedb", "required-lancedb", "required_lancedb"}:
                preferred_store = LanceDbRagStore(repo_root=self.repo_root)
            elif store_mode == "allow-fallback":
                try:
                    preferred_store = PgVectorRagStore()
                    if not preferred_store.is_ready:
                        raise RuntimeError("pgvector unavailable")
                except Exception:
                    preferred_store = SQLiteRagStore(repo_root=self.repo_root)
                    store_mode = "sqlite-fallback"
            else:
                raise ValueError(
                    f"Unsupported RAG_STORE_MODE '{store_mode}'. "
                    "Use sqlite, lancedb, pgvector, or allow-fallback."
                )

        self.store = preferred_store
        self.retrieval_mode = store_mode if vector_store is None else "injected"
        self.embedding_model = getattr(self.store, "embedding_model", None) or "none"
        self.index_version = os.getenv("RAG_INDEX_VERSION", "1")
        self.generator = GeminiGenerator(api_key=gemini_api_key, model=model)
        self._index_repository()

    def _index_repository(self) -> None:
        indexed_paths: set[str] = set()
        for file_path in sorted(self.repo_root.rglob("*")):
            if not file_path.is_file():
                continue
            if file_path.suffix.lower() not in {".cs", ".md", ".txt", ".json"}:
                continue
            relative_path = str(file_path.relative_to(self.repo_root))
            indexed_paths.add(relative_path)
            text = file_path.read_text(encoding="utf-8", errors="ignore")
            for chunk_index, chunk in enumerate(self._chunk_text(text)):
                self.store.add_document(
                    chunk,
                    relative_path,
                    {
                        "kind": "repo",
                        "source_type": "related-test" if "test" in relative_path.lower() else "related-source",
                        "chunk_index": chunk_index,
                    },
                )

        if hasattr(self.store, "prune_missing"):
            self.store.prune_missing(indexed_paths)

    def _chunk_text(self, text: str) -> list[str]:
        if len(text) <= self.chunk_size:
            return [text]

        chunks: list[str] = []
        start = 0
        while start < len(text):
            end = min(start + self.chunk_size, len(text))
            chunk = text[start:end]
            chunks.append(chunk)
            if end >= len(text):
                break
            start = max(start + self.chunk_size - self.chunk_overlap, end - self.chunk_overlap)
        return chunks

    def run(self, query: str, limit: int = 5) -> dict[str, Any]:
        results = self.store.search(query, limit=limit)
        evidence = [
            {
                "evidence_id": hashlib.sha256(
                    f"{item.source_path}:{item.content}".encode("utf-8")
                ).hexdigest()[:16],
                "source_path": item.source_path,
                "content": item.content[:400],
                "score": item.score,
                **item.metadata,
            }
            for item in results
        ]

        prompt = f"""
You are a senior modernization agent.
Use the repository evidence below to answer the user's question.

Question:
{query}

Evidence:
{chr(10).join(f'- {item["source_path"]}: {item["content"]}' for item in evidence) if evidence else 'No repository evidence was found.'}
"""

        answer = self.generator.generate(prompt)

        return {
            "answer": answer,
            "evidence": evidence,
            "retrievalMode": self.retrieval_mode,
            "embeddingModel": self.embedding_model,
            "indexVersion": self.index_version,
        }


def main() -> int:
    parser = argparse.ArgumentParser(description="Legacy modernization agentic RAG prototype")
    parser.add_argument("--repo-root", required=True, help="Repository root to index for retrieval")
    parser.add_argument("--query", help="Question or modernization task to answer")
    parser.add_argument("--server", action="store_true", help="Run a long-lived JSON-lines retrieval worker")
    parser.add_argument("--limit", type=int, default=5, help="Maximum number of evidence snippets to return")
    parser.add_argument("--gemini-key", default=None, help="Optional Gemini API key override")
    args = parser.parse_args()

    pipeline = AgenticRagPipeline(repo_root=args.repo_root, gemini_api_key=args.gemini_key)
    if args.server:
        for line in sys.stdin:
            if not line.strip():
                continue
            try:
                request = json.loads(line)
                result = pipeline.run(
                    request["query"],
                    limit=int(request.get("limit", args.limit)),
                )
                print(json.dumps(result, ensure_ascii=False), flush=True)
            except Exception as error:
                print(json.dumps({"error": str(error)}), flush=True)
        return 0

    if not args.query:
        parser.error("--query is required unless --server is used")

    result = pipeline.run(args.query, limit=args.limit)
    print(json.dumps(result, ensure_ascii=False), flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
