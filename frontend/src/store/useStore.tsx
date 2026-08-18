import { createContext, useContext, useState, type ReactNode } from 'react';
import type { PipelineResult, ReviewStatus, IssueWithSuggestion } from '../types';

interface StoreState {
  pipelineResult: PipelineResult | null;
  items: IssueWithSuggestion[];
  projectPath: string;
  isLoading: boolean;
  error: string | null;
  setPipelineResult: (result: PipelineResult) => void;
  setReviewStatus: (index: number, status: ReviewStatus) => void;
  setProjectPath: (path: string) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  reset: () => void;
}

const StoreContext = createContext<StoreState | null>(null);

export function StoreProvider({ children }: { children: ReactNode }) {
  const [pipelineResult, _setPipelineResult] = useState<PipelineResult | null>(null);
  const [items, setItems] = useState<IssueWithSuggestion[]>([]);
  const [projectPath, setProjectPath] = useState('');
  const [isLoading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function setPipelineResult(result: PipelineResult) {
    _setPipelineResult(result);
    const mapped: IssueWithSuggestion[] = result.analysis.issues.map((issue, i) => ({
      issue,
      suggestion: result.suggestions[i],
      reviewStatus: result.suggestions[i]?.isSafe ? 'approved' as ReviewStatus : 'pending' as ReviewStatus,
    }));
    setItems(mapped);
  }

  function setReviewStatus(index: number, status: ReviewStatus) {
    setItems(prev => prev.map((item, i) => i === index ? { ...item, reviewStatus: status } : item));
  }

  function reset() {
    _setPipelineResult(null);
    setItems([]);
    setError(null);
  }

  return (
    <StoreContext.Provider value={{
      pipelineResult, items, projectPath, isLoading, error,
      setPipelineResult, setReviewStatus, setProjectPath, setLoading, setError, reset,
    }}>
      {children}
    </StoreContext.Provider>
  );
}

export function useStore() {
  const ctx = useContext(StoreContext);
  if (!ctx) throw new Error('useStore must be used within StoreProvider');
  return ctx;
}
