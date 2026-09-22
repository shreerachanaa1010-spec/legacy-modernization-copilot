from __future__ import annotations

import hashlib
import os
from dataclasses import dataclass
from typing import Protocol


class EmbeddingProvider(Protocol):
    @property
    def model(self) -> str:
        ...

    @property
    def dimension(self) -> int:
        ...

    def embed(self, text: str) -> list[float]:
        ...


@dataclass(frozen=True)
class FakeEmbeddingProvider:
    """Deterministic provider for tests only; never selected by default."""

    dimension: int = 32
    model: str = "fake-test-v1"

    def embed(self, text: str) -> list[float]:
        values: list[float] = []
        seed = text.encode("utf-8")
        counter = 0
        while len(values) < self.dimension:
            digest = hashlib.sha256(seed + counter.to_bytes(4, "big")).digest()
            values.extend((byte / 255.0) * 2.0 - 1.0 for byte in digest)
            counter += 1
        return values[: self.dimension]


class GeminiEmbeddingProvider:
    def __init__(
        self,
        api_key: str | None = None,
        model: str | None = None,
        dimension: int | None = None,
    ) -> None:
        self._api_key = api_key or os.getenv("GEMINI_API_KEY", "")
        self._model = model or os.getenv("RAG_EMBEDDING_MODEL", "gemini-embedding-001")
        self._dimension = dimension or int(os.getenv("RAG_EMBEDDING_DIM", "768"))
        if not self._api_key:
            raise RuntimeError("GEMINI_API_KEY is required for Gemini embeddings.")

        from google import genai

        self._client = genai.Client(api_key=self._api_key)

    @property
    def model(self) -> str:
        return self._model

    @property
    def dimension(self) -> int:
        return self._dimension

    def embed(self, text: str) -> list[float]:
        response = self._client.models.embed_content(
            model=self._model,
            contents=text,
            config={"output_dimensionality": self._dimension},
        )
        embeddings = getattr(response, "embeddings", None) or []
        if not embeddings:
            raise RuntimeError("Gemini returned no embedding values.")

        values = list(getattr(embeddings[0], "values", []) or [])
        if len(values) != self._dimension:
            raise ValueError(
                f"Embedding dimension mismatch: expected {self._dimension}, got {len(values)}."
            )
        return [float(value) for value in values]


def configured_embedding_provider() -> EmbeddingProvider | None:
    provider_name = os.getenv("RAG_EMBEDDING_PROVIDER", "none").strip().lower()
    if provider_name in {"", "none", "lexical"}:
        return None
    if provider_name == "gemini":
        return GeminiEmbeddingProvider()
    if provider_name == "fake-test":
        raise RuntimeError("fake-test embeddings may only be injected by tests.")
    raise ValueError(
        f"Unsupported RAG_EMBEDDING_PROVIDER '{provider_name}'. "
        "Use none or gemini."
    )
