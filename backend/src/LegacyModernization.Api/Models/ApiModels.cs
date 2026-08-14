namespace LegacyModernization.Api.Models;

public class AnalyzeRequest
{
    public string ProjectPath { get; set; } = "";
}

public class SuggestionsRequest
{
    public string ProjectPath { get; set; } = "";
}

public class VerifyRequest
{
    public string TestProjectPath { get; set; } = "";
}

public class PipelineRequest
{
    public string ProjectPath { get; set; } = "";
    public string TestProjectPath { get; set; } = "";
}

public class PipelineResult
{
    public Analyzer.Models.ProjectAnalysisResult Analysis { get; set; } = new();
    public List<LLM.Models.RefactorSuggestion> Suggestions { get; set; } = new();
    public Verifier.Models.VerificationResult? Verification { get; set; }
}
