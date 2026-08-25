import type { ProjectAnalysisResult, RefactorSuggestion, PipelineResult, VerificationResult, GeneratedTest } from './types';

const BASE = '/api';

async function request<T>(url: string, body: object): Promise<T> {
  const res = await fetch(`${BASE}${url}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || `Request failed: ${res.status}`);
  }
  return res.json();
}

export function analyzeProject(projectPath: string) {
  return request<ProjectAnalysisResult>('/analysis', { projectPath });
}

export function getSuggestions(projectPath: string) {
  return request<RefactorSuggestion[]>('/suggestions', { projectPath });
}

export function runVerification(testProjectPath: string) {
  return request<VerificationResult>('/verification', { testProjectPath });
}

export function generateTests(projectPath: string) {
  return request<GeneratedTest[]>('/testgeneration', { projectPath });
}

export function runPipeline(projectPath: string, testProjectPath?: string) {
  return request<PipelineResult>('/pipeline', { projectPath, testProjectPath: testProjectPath ?? '' });
}
