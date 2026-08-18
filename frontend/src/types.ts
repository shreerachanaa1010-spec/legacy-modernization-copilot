export interface AnalysisIssue {
  ruleId: string;
  title: string;
  description: string;
  severity: string;
  filePath: string;
  lineNumber: number;
  codeSnippet: string;
}

export interface ClassInfo {
  name: string;
  namespace: string;
  methods: { name: string; returnType: string }[];
}

export interface ProjectAnalysisResult {
  projectName: string;
  classes: ClassInfo[];
  issues: AnalysisIssue[];
}

export interface RefactorSuggestion {
  ruleId: string;
  issueTitle: string;
  reason: string;
  originalCode: string;
  refactoredCode: string;
  explanation: string;
  isSafe: boolean;
}

export interface VerificationResult {
  originalTestPassed: boolean;
  refactoredTestPassed: boolean;
  status: string;
  originalOutput: string;
  refactoredOutput: string;
  explanation: string;
}

export interface PipelineResult {
  analysis: ProjectAnalysisResult;
  suggestions: RefactorSuggestion[];
  verification: VerificationResult | null;
}

export type ReviewStatus = 'pending' | 'approved' | 'rejected';

export interface IssueWithSuggestion {
  issue: AnalysisIssue;
  suggestion?: RefactorSuggestion;
  reviewStatus: ReviewStatus;
}
