# Contributing

Thanks for helping improve Legacy Modernization Copilot. Please keep changes focused and include tests for behavior changes.

## Before Opening a Pull Request

Run the relevant checks from the repository root:

```powershell
dotnet test LegacyModernizationCopilot.slnx
dotnet test backend/src/LegacyModernization.Analyzer.Tests/LegacyModernization.Analyzer.Tests.csproj
dotnet test backend/tests/LegacyModernization.Api.Tests/LegacyModernization.Api.Tests.csproj
python -m pytest python/tests -q
Set-Location frontend
npm ci
npm run build
```

Do not commit `.env` files, API keys, generated build output, or local retrieval databases. Never use a real Gemini key in automated tests.

For API changes, preserve demo-mode path restrictions and do not add a route that can build, test, or edit projects outside the bundled `samples/` tree when `DEMO_MODE=true`.