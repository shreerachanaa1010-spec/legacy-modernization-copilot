import { useNavigate } from 'react-router-dom';
import { useStore } from '../store/useStore';
import { IssueCard } from '../components/IssueCard';
import { StatsBar } from '../components/StatsBar';
import { ArrowLeft, Filter } from 'lucide-react';
import { useState } from 'react';

type FilterType = 'all' | 'approved' | 'pending' | 'rejected';

export function ResultsPage() {
  const navigate = useNavigate();
  const { items, pipelineResult } = useStore();
  const [filter, setFilter] = useState<FilterType>('all');
  const [severityFilter, setSeverityFilter] = useState<string>('all');

  if (!pipelineResult) {
    return (
      <div className="flex flex-col items-center justify-center py-32">
        <div className="w-16 h-16 rounded-2xl bg-slate-800 flex items-center justify-center mb-4">
          <Filter className="w-8 h-8 text-slate-600" />
        </div>
        <h2 className="text-xl font-semibold text-slate-300 mb-2">No Analysis Results</h2>
        <p className="text-sm text-slate-500 mb-6">Run the pipeline on a .NET project to see results here.</p>
        <button
          onClick={() => navigate('/')}
          className="px-5 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white text-sm font-medium transition-colors"
        >
          Go to Analyzer
        </button>
      </div>
    );
  }

  const filtered = items.filter(item => {
    const statusMatch = filter === 'all' || item.reviewStatus === filter;
    const sevMatch = severityFilter === 'all' || item.issue.severity.toLowerCase() === severityFilter.toLowerCase();
    return statusMatch && sevMatch;
  });

  const filterBtnClass = (f: FilterType) =>
    `px-3 py-1.5 text-xs font-medium rounded-lg transition-all ${
      filter === f
        ? 'bg-indigo-500/20 text-indigo-300 border border-indigo-500/30'
        : 'text-slate-500 hover:text-slate-300 border border-transparent hover:bg-white/5'
    }`;

  const severities = ['all', ...new Set(items.map(i => i.issue.severity))];

  return (
    <div>
      <button
        onClick={() => navigate('/')}
        className="flex items-center gap-2 text-sm text-slate-500 hover:text-indigo-400 mb-6 transition-colors"
      >
        <ArrowLeft className="w-4 h-4" />
        New Analysis
      </button>

      <StatsBar items={items} projectName={pipelineResult.analysis.projectName} />

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-6 p-4 rounded-xl border border-white/5 bg-white/[0.02]">
        <span className="text-xs text-slate-500 uppercase tracking-wider font-medium">Status:</span>
        <div className="flex gap-1">
          {(['all', 'approved', 'pending', 'rejected'] as FilterType[]).map(f => (
            <button key={f} onClick={() => setFilter(f)} className={filterBtnClass(f)}>
              {f === 'all' ? 'All' : f === 'approved' ? 'Verified Safe' : f === 'pending' ? 'Needs Review' : 'Rejected'}
            </button>
          ))}
        </div>
        <div className="w-px h-6 bg-white/10 mx-2 hidden sm:block" />
        <span className="text-xs text-slate-500 uppercase tracking-wider font-medium">Severity:</span>
        <div className="flex gap-1">
          {severities.map(s => (
            <button
              key={s}
              onClick={() => setSeverityFilter(s)}
              className={`px-3 py-1.5 text-xs font-medium rounded-lg transition-all ${
                severityFilter === s
                  ? 'bg-indigo-500/20 text-indigo-300 border border-indigo-500/30'
                  : 'text-slate-500 hover:text-slate-300 border border-transparent hover:bg-white/5'
              }`}
            >
              {s === 'all' ? 'All' : s}
            </button>
          ))}
        </div>
      </div>

      {/* Issue list */}
      <div className="grid grid-cols-1 gap-3">
        {filtered.length === 0 ? (
          <div className="text-center py-16 text-slate-500 text-sm">
            No issues match the current filters.
          </div>
        ) : (
          filtered.map((item) => {
            const originalIndex = items.indexOf(item);
            return <IssueCard key={originalIndex} item={item} index={originalIndex} />;
          })
        )}
      </div>
    </div>
  );
}
