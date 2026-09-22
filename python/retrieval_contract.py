from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass(frozen=True)
class SearchResult:
    source_path: str
    content: str
    metadata: dict[str, Any] = field(default_factory=dict)
    score: float = 0.0
