import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, FileCode2, MapPin, ThumbsUp, ThumbsDown, Undo2, Lightbulb, AlertTriangle } from 'lucide-react';
import { useStore } from '../store/useStore';
import { StatusBadge, SeverityBadge } from '../components/Badges';
import { DiffViewer } from '../components/DiffViewer';
import { CodeBlock } from '../components/CodeBlock';

const ruleColors: Record<string, string> = {
  LMC001: 'from-red-500 to-orange-500',
  LMC002: 'from-amber-500 to-yellow-500',
  LMC003: 'from-blue-500 to-cyan-500',
  LMC004: 'from-purple-500 to-pink-500',
};

export function IssueDetailPage() {
  const { index } = useParams<{ index: string }>();
  const navigate = useNavigate();
  const { items, setReviewStatus } = useStore();

  const idx = parseInt(index || '0', 10);
  const item = items[idx];

  if (!item) {
    return (
      <div className="flex flex-col items-center justify-center py-32">
        <h2 className="text-xl font-semibold text-slate-300 mb-2">Issue Not Found</h2>
        <button onClick={() => navigate('/results')} className="text-indigo-400 text-sm hover:underline">
          Back to Results
        </button>
      </div>
    );
  }

  const { issue, suggestion, reviewStatus } = item;
  const gradient = ruleColors[issue.ruleId] || 'from-slate-500 to-slate-600';
  const fileName = issue.filePath.split(/[/\\]/).pop() || issue.filePath;

  const actionBtnBase = 'flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold transition-all duration-200';

  return (
    <div>
      {/* Back nav */}
      <button
        onClick={() => navigate('/results')}
        className="flex items-center gap-2 text-sm text-slate-500 hover:text-indigo-400 mb-6 transition-colors"
      >
        <ArrowLeft className="w-4 h-4" />
        Back to Results
      </button>

      {/* Header */}
      <div className="rounded-2xl border border-white/5 bg-white/[0.02] overflow-hidden mb-6">
        <div className={`h-1.5 bg-gradient-to-r ${gradient}`} />
        <div className="p-6">
          <div className="flex flex-wrap items-start justify-between gap-4 mb-4">
            <div className="flex items-center gap-3">
              <span className={`px-3 py-1.5 text-sm font-bold rounded-xl bg-gradient-to-r ${gradient} text-white shadow-lg`}>
                {issue.ruleId}
              </span>
              <h1 className="text-xl font-bold text-white">{issue.title}</h1>
            </div>
            <div className="flex items-center gap-2">
              <SeverityBadge severity={issue.severity} />
              <StatusBadge status={reviewStatus} />
            </div>
          </div>

          <p className="text-sm text-slate-400 leading-relaxed mb-4">{issue.description}</p>

          <div className="flex items-center gap-4 text-xs text-slate-500">
            <span className="flex items-center gap-1.5">
              <FileCode2 className="w-4 h-4" />
              {fileName}
            </span>
            <span className="flex items-center gap-1.5">
              <MapPin className="w-4 h-4" />
              Line {issue.lineNumber}
            </span>
            <span className="text-slate-600 font-mono text-xs truncate max-w-md" title={issue.filePath}>
              {issue.filePath}
            </span>
          </div>
        </div>
      </div>

      {/* Original code snippet */}
      <div className="mb-6">
        <h2 className="text-sm font-semibold text-slate-300 mb-3 flex items-center gap-2">
          <AlertTriangle className="w-4 h-4 text-amber-400" />
          Detected Code
        </h2>
        <CodeBlock code={issue.codeSnippet} title={`${fileName} · Line ${issue.lineNumber}`} />
      </div>

      {/* AI Suggestion */}
      {suggestion && (
        <>
          {/* Explanation */}
          <div className="rounded-2xl border border-white/5 bg-white/[0.02] p-6 mb-6">
            <h2 className="text-sm font-semibold text-slate-300 mb-3 flex items-center gap-2">
              <Lightbulb className="w-4 h-4 text-amber-400" />
              AI Explanation
            </h2>
            <div className="space-y-3">
              <div className="rounded-xl bg-slate-900/50 border border-white/5 p-4">
                <h3 className="text-xs font-semibold text-indigo-400 uppercase tracking-wider mb-2">Why This Is a Problem</h3>
                <p className="text-sm text-slate-400 leading-relaxed whitespace-pre-wrap">{suggestion.reason}</p>
              </div>
              <div className="rounded-xl bg-slate-900/50 border border-white/5 p-4">
                <h3 className="text-xs font-semibold text-emerald-400 uppercase tracking-wider mb-2">Recommended Fix</h3>
                <p className="text-sm text-slate-400 leading-relaxed whitespace-pre-wrap">{suggestion.explanation}</p>
              </div>
            </div>
          </div>

          {/* Diff view */}
          {suggestion.originalCode && suggestion.refactoredCode && (
            <div className="mb-6">
              <h2 className="text-sm font-semibold text-slate-300 mb-4">Code Comparison</h2>
              <DiffViewer
                originalCode={suggestion.originalCode}
                refactoredCode={suggestion.refactoredCode}
              />
            </div>
          )}

          {/* Approve / Reject */}
          <div className="rounded-2xl border border-white/5 bg-white/[0.02] p-6">
            <h2 className="text-sm font-semibold text-slate-300 mb-4">Review Decision</h2>
            <div className="flex flex-wrap gap-3">
              <button
                onClick={() => setReviewStatus(idx, 'approved')}
                className={`${actionBtnBase} ${
                  reviewStatus === 'approved'
                    ? 'bg-emerald-500 text-white shadow-lg shadow-emerald-500/25'
                    : 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30 hover:bg-emerald-500/20'
                }`}
              >
                <ThumbsUp className="w-4 h-4" />
                {reviewStatus === 'approved' ? 'Approved' : 'Approve'}
              </button>
              <button
                onClick={() => setReviewStatus(idx, 'rejected')}
                className={`${actionBtnBase} ${
                  reviewStatus === 'rejected'
                    ? 'bg-red-500 text-white shadow-lg shadow-red-500/25'
                    : 'bg-red-500/10 text-red-400 border border-red-500/30 hover:bg-red-500/20'
                }`}
              >
                <ThumbsDown className="w-4 h-4" />
                {reviewStatus === 'rejected' ? 'Rejected' : 'Reject'}
              </button>
              {reviewStatus !== 'pending' && (
                <button
                  onClick={() => setReviewStatus(idx, 'pending')}
                  className={`${actionBtnBase} bg-white/5 text-slate-400 border border-white/10 hover:bg-white/10`}
                >
                  <Undo2 className="w-4 h-4" />
                  Reset
                </button>
              )}
            </div>
          </div>
        </>
      )}

      {/* Navigation between issues */}
      <div className="flex justify-between mt-8 pt-6 border-t border-white/5">
        <button
          onClick={() => idx > 0 && navigate(`/issue/${idx - 1}`)}
          disabled={idx === 0}
          className="px-4 py-2 text-sm text-slate-500 hover:text-indigo-400 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
        >
          ← Previous Issue
        </button>
        <span className="text-xs text-slate-600 self-center">
          {idx + 1} of {items.length}
        </span>
        <button
          onClick={() => idx < items.length - 1 && navigate(`/issue/${idx + 1}`)}
          disabled={idx >= items.length - 1}
          className="px-4 py-2 text-sm text-slate-500 hover:text-indigo-400 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
        >
          Next Issue →
        </button>
      </div>
    </div>
  );
}
