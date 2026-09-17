# RAG Implementation Plan

## Goal

Give modernization agents repository evidence before they propose changes, while keeping code facts and verification deterministic.

## Current Slice

The new `LegacyModernization.Rag` project provides:

- `IRepositoryRetriever`, an interchangeable retrieval contract.
- `FileSystemRepositoryRetriever`, a deterministic first retriever.
- `RetrievedContext` and `RetrievedDocument` models.
- Repository-root validation to prevent reading files outside the requested project.
- Primary source, nearby source, and nearby test retrieval.

`AnalyzerHost` now retrieves this context before calling `GeminiService`.

## Staged Delivery

### Stage 1: Deterministic repository retrieval

- Retrieve the finding's source file.
- Retrieve nearby C# files and tests.
- Preserve source paths and document types.
- Add tests for retrieval and path containment.

### Stage 2: Symbol-aware retrieval

- Use Roslyn to identify the containing class and method.
- Retrieve callers, callees, interfaces, implementations, and related tests.
- Add line ranges and symbol names to retrieved documents.
- Keep this retrieval independent of the LLM provider.

### Stage 3: Knowledge ingestion

- Add modernization guidance and project conventions as indexed documents.
- Record accepted refactorings with their verification results.
- Define document metadata and a stable finding fingerprint.

### Stage 4: Vector retrieval

- Add an embedding provider behind an interface.
- Add a vector store implementation, preferably PostgreSQL with `pgvector` when deployment needs persistence.
- Keep deterministic symbol lookup as a fallback and source of truth.

### Stage 5: Agent orchestration

- Add a structured modernization plan before code generation.
- Require retrieved-source references in each plan.
- Generate tests from the same retrieved context.
- Apply patches only after explicit approval.
- Verify original and refactored projects in an isolated workspace.

## Guardrails

- Retrieval may provide evidence, but it cannot establish compilation or test status.
- Roslyn remains authoritative for code structure and symbols.
- The verifier remains authoritative for behavior.
- The LLM must not choose paths outside the requested repository.
- Prompt size, file size, and retrieved-document counts must be bounded.
- API keys must move out of checked-in configuration before production use.

## Definition Of Done For The First Milestone

- A finding produces a context object containing the primary source.
- The LLM receives that context through an explicit interface.
- Retrieval is covered by automated tests.
- Existing analyzer-host build remains successful.
- No vector database is required to run the first milestone.
