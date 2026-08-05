# Day 4 – Real Repository Analysis

## Repository 1

**Name:** WebAPIContrib

**Repository URL:** https://github.com/WebApiContrib/WebAPIContrib

**Project Analyzed:**

samples/WebAPIContrib/src/WebApiContrib/WebApiContrib.csproj

---

## Detection Results

| Rule | Description | Result |
|------|-------------|--------|
| LMC001 | Sync-over-Async (.Result / .Wait()) | No issues found |
| LMC002 | WebClient Usage | No issues found |
| LMC003 | Missing ConfigureAwait(false) | ✅ Issues detected |
| LMC004 | Improper Dispose Pattern | No issues found |

---

## Summary

The analyzer successfully processed the WebAPIContrib project using Roslyn syntax analysis and executed all four implemented modernization rules.

The analysis identified multiple instances of missing `ConfigureAwait(false)` in asynchronous library code. No occurrences of `Task.Result`, `Task.Wait()`, obsolete `WebClient` usage, or incorrect `IDisposable` patterns were found in the analyzed project.

This demonstrates that the analyzer correctly identifies applicable modernization opportunities while avoiding false positives for rules that do not match the codebase.

---

## Outcome

- ✅ Project loaded successfully
- ✅ Roslyn syntax trees generated
- ✅ All four rules executed
- ✅ LMC003 issues reported with file paths and line numbers
- ✅ JSON analysis report generated








---

# Repository 2

**Name:** Entropy

**Repository URL:** https://github.com/aspnet/Entropy

**Project Analyzed:**

samples/Entropy/test/Entropy.FunctionalTests/Entropy.FunctionalTests.csproj

---

## Detection Results

| Rule | Description | Result |
|------|-------------|--------|
| LMC001 | Sync-over-Async (.Result / .Wait()) | No issues found |
| LMC002 | WebClient Usage | No issues found |
| LMC003 | Missing ConfigureAwait(false) | No issues found |
| LMC004 | Improper Dispose Pattern | No issues found |

---

## Summary

The analyzer successfully parsed and analyzed the Entropy FunctionalTests project. None of the implemented modernization rules were triggered, indicating that the analyzed codebase does not contain the targeted legacy patterns.

This validates that the analyzer correctly reports zero findings when the analyzed project already follows modern .NET coding practices, demonstrating low false-positive behavior.

---

## Outcome

- ✅ Project loaded successfully
- ✅ Analysis completed successfully
- ✅ All four rules executed
- ✅ No matching legacy patterns detected
- ✅ JSON analysis report generated