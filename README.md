# Legacy Modernization Copilot

A .NET-based modernization assistant that combines static analysis, retrieval-augmented generation, and verification to help modernize legacy codebases with grounded evidence.

## Overview

This project analyzes legacy code, identifies modernization issues, retrieves the most relevant repository evidence, and then uses an LLM to propose refactorings. The verifier ultimately checks whether the generated solution is valid and safe.

The system is designed to keep the model anchored to real repository context instead of generating code based only on the issue description.

## High-level architecture

```mermaid
flowchart TD
    A[Legacy codebase] --> B[Analyzer]
    B --> C[AnalysisIssue]
    C --> D[Repository Retriever]
    D --> E[Vector Search / Evidence Lookup]
    E --> F[Gemini Generation]
    F --> G[RefactorSuggestion]
    G --> H[Verifier]
    H --> I[Validated modernization output]
    D --> J[Source files + tests + docs]
    J --> E
```

## Current technology stack

### Core platform
- .NET 10
- ASP.NET Core
- C#

### Retrieval and agentic context layer
- Python service for retrieval orchestration
- Local in-memory vector store for prototype/testing
- PostgreSQL + pgvector via Docker for production-oriented storage

### LLM layer
- Gemini API for grounded generation
- Retrieval context is passed to the model before code generation

### Verification layer
- .NET verifier checks generated outputs against project behavior and safety constraints

## Repository structure

- `backend/` - main .NET backend services
- `python/` - Python-based retrieval and agentic RAG prototype
- `docs/` - design and planning documents
- `samples/` - sample legacy and modern projects
- `tools/` - supporting tooling and runner utilities

## Python RAG prototype

The Python layer provides the retrieval engine and the evidence-grounding workflow.

### Example usage

```bash
cd python
python -m pytest tests/test_agentic_rag.py -q
```

### Sample flow

```python
from agentic_rag import AgenticRagPipeline

pipeline = AgenticRagPipeline(repo_root=".")
result = pipeline.run("Explain how refund processing works in this codebase")
print(result["answer"])
print(result["evidence"])
```

## Docker setup for vector storage

A PostgreSQL + pgvector container is included for future production indexing.

```bash
docker-compose up -d
```

This starts a local PostgreSQL instance with pgvector support on port `5432`.

## Environment configuration

Copy the example environment file and fill in your real values:

```bash
copy .env.example .env
```

Then set:

- `GEMINI_API_KEY` for the generation layer
- `PGVECTOR_CONNECTION_STRING` for the PostgreSQL + pgvector connector

The Python RAG service reads these values automatically when present.

## Recommended architecture for production

1. Analyzer emits a modernization issue.
2. Retriever loads the primary source file plus related files and tests.
3. Python RAG service indexes and queries repository evidence.
4. Gemini receives the issue plus evidence and generates a refactor suggestion.
5. Verifier confirms build/test validity and safety.
6. Approved changes are applied only after verification.

## Setup steps

### 1. Clone the repository

```bash
git clone <repo-url>
cd legacy-modernization-copilot
```

### 2. Start vector storage

```bash
docker-compose up -d
```

### 3. Configure environment variables

Create a local environment file from the sample:

```bash
copy .env.example .env
```

Then edit `.env` and set:

```env
GEMINI_API_KEY=your_api_key_here
PGVECTOR_CONNECTION_STRING=postgresql://postgres:postgres@localhost:5432/legacy_rag
```

On Linux/macOS:

```bash
export GEMINI_API_KEY="your_api_key_here"
export PGVECTOR_CONNECTION_STRING="postgresql://postgres:postgres@localhost:5432/legacy_rag"
```

On Windows PowerShell:

```powershell
$env:GEMINI_API_KEY="your_api_key_here"
$env:PGVECTOR_CONNECTION_STRING="postgresql://postgres:postgres@localhost:5432/legacy_rag"
```

### 4. Build the .NET solution

```bash
cd backend
dotnet build
```

### 5. Run Python validation tests

```bash
cd python
python -m pytest tests/test_agentic_rag.py -q
```

## Design principles

- Retrieval is grounded in repository evidence.
- LLM output is treated as a suggestion, not as fact.
- Verification is required before accepting modernization changes.
- The agent must stay within the allowed repository scope.
- Deterministic file and symbol retrieval is preferred before vector search.

## Future roadmap

### Phase 1: Deterministic retrieval
- primary file retrieval
- related source/test lookup
- repository containment checks
- evidence packaging for prompts

### Phase 2: Symbol-aware retrieval
- class/method context
- call graph awareness
- related file and symbol relevance scoring

### Phase 3: Vector database integration
- persistent pgvector indexing
- chunk metadata and filtering
- hybrid keyword + semantic retrieval

### Phase 4: Agent orchestration
- multi-step reasoning flows
- plan generation
- patch approval and verification loops

## License

This project is provided under the repository license terms.
