import { Link } from 'react-router-dom';
import { FileCode2, MapPin } from 'lucide-react';
import { StatusBadge, SeverityBadge } from './Badges';
import type { IssueWithSuggestion } from '../types';

interface IssueCardProps {
  item: IssueWithSuggestion;
  index: number;
}

const ruleColors: Record<string, string> = {
  LMC001: 'from-red-500 to-orange-500',
  LMC002: 'from-amber-500 to-yellow-500',
  LMC003: 'from-blue-500 to-cyan-500',
  LMC004: 'from-purple-500 to-pink-500',
};

export function IssueCard({ item, index }: IssueCardProps) {
  const { issue, suggestion, reviewStatus } = item;
  const gradient = ruleColors[issue.ruleId] || 'from-slate-500 to-slate-600';
  const fileName = issue.filePath.split(/[/\\]/).pop() || issue.filePath;

  return (
    <Link
      to={`/issue/${index}`}
      className="group block rounded-2xl border border-white/5 bg-white/[0.02] hover:bg-white/[0.05] hover:border-indigo-500/30 transition-all duration-300 overflow-hidden hover:shadow-xl hover:shadow-indigo-500/5"
    >
      {/* Top gradient accent */}
      <div className={`h-1 bg-gradient-to-r ${gradient}`} />

      <div className="p-5">
        <div className="flex items-start justify-between gap-3 mb-3">
          <div className="flex items-center gap-2.5">
            <span className={`px-2 py-1 text-xs font-bold rounded-lg bg-gradient-to-r ${gradient} text-white shadow-sm`}>
              {issue.ruleId}
            </span>
            <h3 className="text-sm font-semibold text-slate-200 group-hover:text-white transition-colors line-clamp-1">
              {issue.title}
            </h3>
          </div>
          <SeverityBadge severity={issue.severity} />
        </div>

        <p className="text-xs text-slate-400 mb-4 line-clamp-2 leading-relaxed">
          {issue.description}
        </p>

        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3 text-xs text-slate-500">
            <span className="flex items-center gap-1">
              <FileCode2 className="w-3.5 h-3.5" />
              {fileName}
            </span>
            <span className="flex items-center gap-1">
              <MapPin className="w-3.5 h-3.5" />
              Line {issue.lineNumber}
            </span>
          </div>
          <StatusBadge status={reviewStatus} />
        </div>

        {suggestion && (
          <div className="mt-3 pt-3 border-t border-white/5">
            <p className="text-xs text-slate-500 line-clamp-1">
              <span className="text-indigo-400 font-medium">AI fix: </span>
              {suggestion.explanation}
            </p>
          </div>
        )}
      </div>
    </Link>
  );
}
