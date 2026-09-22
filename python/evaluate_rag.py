from __future__ import annotations

import argparse
import json
import time
from pathlib import Path

from agentic_rag import AgenticRagPipeline


def main() -> int:
    parser = argparse.ArgumentParser(description="Evaluate local RAG retrieval against a JSONL dataset")
    parser.add_argument("--repo-root", required=True)
    parser.add_argument("--dataset", required=True, help="JSONL rows with query and expected_paths")
    parser.add_argument("--output", default="reports/rag-eval-report.json")
    args = parser.parse_args()

    pipeline = AgenticRagPipeline(repo_root=args.repo_root)
    rows = [json.loads(line) for line in Path(args.dataset).read_text(encoding="utf-8").splitlines() if line.strip()]
    reciprocal_ranks: list[float] = []
    hits = 0
    started = time.perf_counter()

    for row in rows:
        expected = set(row.get("expected_paths", []))
        evidence = pipeline.run(row["query"], limit=10)["evidence"]
        paths = [item["source_path"] for item in evidence]
        rank = next((index + 1 for index, path in enumerate(paths) if path in expected), 0)
        if rank:
            hits += 1
            reciprocal_ranks.append(1.0 / rank)
        else:
            reciprocal_ranks.append(0.0)

    report = {
        "retrievalMode": pipeline.retrieval_mode,
        "embeddingModel": pipeline.embedding_model,
        "indexVersion": pipeline.index_version,
        "queryCount": len(rows),
        "recallAt10": hits / len(rows) if rows else 0.0,
        "mrr": sum(reciprocal_ranks) / len(rows) if rows else 0.0,
        "elapsedSeconds": round(time.perf_counter() - started, 3),
    }
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
