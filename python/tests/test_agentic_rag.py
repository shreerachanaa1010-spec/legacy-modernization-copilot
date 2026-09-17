from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from agentic_rag import AgenticRagPipeline, LocalVectorStore


def test_local_vector_store_matches_relevant_document() -> None:
    vector_store = LocalVectorStore()
    vector_store.add_document(
        "Payment service handles refund and invoice reconciliation.",
        "src/PaymentService.cs",
        {"kind": "source"},
    )
    vector_store.add_document(
        "This doc explains onboarding and user registration flow.",
        "docs/user-guide.md",
        {"kind": "doc"},
    )

    results = vector_store.search("refund invoice processing", limit=1)

    assert results
    assert results[0].source_path == "src/PaymentService.cs"


def test_agentic_pipeline_returns_evidence_and_summary(tmp_path: Path) -> None:
    repo_root = tmp_path / "repo"
    repo_root.mkdir()

    source_file = repo_root / "PaymentService.cs"
    source_file.write_text(
        "class PaymentService { public void ProcessRefund() { /* refund logic */ } }",
        encoding="utf-8",
    )

    docs_dir = repo_root / "docs"
    docs_dir.mkdir()
    (docs_dir / "notes.md").write_text(
        "The payment service is responsible for refund and invoice updates.",
        encoding="utf-8",
    )

    pipeline = AgenticRagPipeline(repo_root=str(repo_root), chunk_size=300, chunk_overlap=50)
    output = pipeline.run("Explain how the payment service handles refund processing.")

    assert output["answer"]
    assert output["evidence"]
    assert any("PaymentService" in item["source_path"] for item in output["evidence"])


def test_cli_returns_json_for_rag_query(tmp_path: Path) -> None:
    repo_root = tmp_path / "repo"
    repo_root.mkdir()
    (repo_root / "PaymentService.cs").write_text(
        "class PaymentService { public void ProcessRefund() { } }",
        encoding="utf-8",
    )

    script_path = Path(__file__).resolve().parents[1] / "agentic_rag.py"
    result = subprocess.run(
        [
            sys.executable,
            str(script_path),
            "--repo-root",
            str(repo_root),
            "--query",
            "refund processing",
            "--limit",
            "3",
        ],
        capture_output=True,
        text=True,
        check=False,
    )

    assert result.returncode == 0, result.stderr
    payload = json.loads(result.stdout)
    assert payload["answer"]
    assert payload["evidence"]
    assert any("PaymentService" in item["source_path"] for item in payload["evidence"])


def test_pgvector_store_falls_back_to_local_when_database_is_unavailable() -> None:
    store = LocalVectorStore()
    store.add_document("Refund processing uses payment reconciliation.", "src/PaymentService.cs")

    query_result = store.search("payment reconciliation", limit=1)

    assert query_result
    assert query_result[0].source_path == "src/PaymentService.cs"
