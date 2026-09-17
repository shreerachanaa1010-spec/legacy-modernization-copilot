from __future__ import annotations

import argparse
import json
import math
import os
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Iterable

from pgvector_rag import PgVectorRagStore


@dataclass
class SearchResult:
    source_path: str
    content: str
    metadata: dict[str, Any] = field(default_factory=dict)
    score: float = 0.0


class LocalVectorStore:
    def __init__(self) -> None:
        self._documents: list[tuple[str, str, dict[str, Any]]] = []
        self._vectors: list[list[float]] = []

    @property
    def is_ready(self) -> bool:
        return True

    def _tokenize(self, text: str) -> list[str]:
        return [token.lower() for token in text.replace("\n", " ").split() if token.strip()]

    def _vectorize(self, text: str) -> list[float]:
        tokens = self._tokenize(text)
        if not tokens:
            return [0.0]

        counts: dict[str, float] = {}
        for token in tokens:
            counts[token] = counts.get(token, 0.0) + 1.0

        norm = math.sqrt(sum(value * value for value in counts.values()))
        if norm == 0:
            return [0.0]

        return [value / norm for value in counts.values()]

    def add_document(self, content: str, source_path: str, metadata: dict[str, Any] | None = None) -> None:
        self._documents.append((content, source_path, metadata or {}))
        self._vectors.append(self._vectorize(content))

    def search(self, query: str, limit: int = 5) -> list[SearchResult]:
        query_vector = self._vectorize(query)
        scored: list[tuple[float, int]] = []

        for idx, stored_vector in enumerate(self._vectors):
            score = self._dot_product(query_vector, stored_vector)
            scored.append((score, idx))

        scored.sort(key=lambda item: item[0], reverse=True)

        results: list[SearchResult] = []
        for _, idx in scored[:limit]:
            content, source_path, metadata = self._documents[idx]
            results.append(
                SearchResult(
                    source_path=source_path,
                    content=content,
                    metadata=metadata,
                    score=0.0,
                )
            )

        return results

    @staticmethod
    def _dot_product(left: Iterable[float], right: Iterable[float]) -> float:
        return sum(a * b for a, b in zip(left, right))


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
        if preferred_store is None:
            try:
                preferred_store = PgVectorRagStore()
                if not preferred_store.is_ready:
                    preferred_store = LocalVectorStore()
            except Exception:
                preferred_store = LocalVectorStore()

        self.store = preferred_store
        self.generator = GeminiGenerator(api_key=gemini_api_key, model=model)
        self._index_repository()

    def _index_repository(self) -> None:
        for file_path in sorted(self.repo_root.rglob("*")):
            if not file_path.is_file():
                continue
            if file_path.suffix.lower() not in {".cs", ".md", ".txt", ".json"}:
                continue
            text = file_path.read_text(encoding="utf-8", errors="ignore")
            for chunk in self._chunk_text(text):
                self.store.add_document(chunk, str(file_path.relative_to(self.repo_root)), {"kind": "repo"})

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
            {"source_path": item.source_path, "content": item.content[:400], "score": item.score}
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

        return {"answer": answer, "evidence": evidence}


def main() -> int:
    parser = argparse.ArgumentParser(description="Legacy modernization agentic RAG prototype")
    parser.add_argument("--repo-root", required=True, help="Repository root to index for retrieval")
    parser.add_argument("--query", required=True, help="Question or modernization task to answer")
    parser.add_argument("--limit", type=int, default=5, help="Maximum number of evidence snippets to return")
    parser.add_argument("--gemini-key", default=None, help="Optional Gemini API key override")
    args = parser.parse_args()

    pipeline = AgenticRagPipeline(
        repo_root=args.repo_root,
        gemini_api_key=args.gemini_key,
    )
    result = pipeline.run(args.query, limit=args.limit)
    print(json.dumps(result, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
