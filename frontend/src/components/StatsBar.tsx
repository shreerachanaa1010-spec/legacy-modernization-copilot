import { ShieldCheck, AlertTriangle, XCircle, BarChart3 } from 'lucide-react';
import type { IssueWithSuggestion } from '../types';

interface StatsBarProps {
  items: IssueWithSuggestion[];
  projectName: string;
}

export function StatsBar({ items, projectName }: StatsBarProps) {
  const total = items.length;
  const safe = items.filter(i => i.reviewStatus === 'approved').length;
  const pending = items.filter(i => i.reviewStatus === 'pending').length;
  const rejected = items.filter(i => i.reviewStatus === 'rejected').length;
  const highSev = items.filter(i => i.issue.severity.toLowerCase() === 'high').length;

  const cards = [
    { label: 'Total Issues', value: total, icon: BarChart3, color: 'from-indigo-500 to-purple-500', text: 'text-indigo-400' },
    { label: 'Verified Safe', value: safe, icon: ShieldCheck, color: 'from-emerald-500 to-green-500', text: 'text-emerald-400' },
    { label: 'Needs Review', value: pending, icon: AlertTriangle, color: 'from-amber-500 to-yellow-500', text: 'text-amber-400' },
    { label: 'High Severity', value: highSev, icon: XCircle, color: 'from-red-500 to-orange-500', text: 'text-red-400' },
  ];

  return (
    <div className="mb-8">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-white">
          Analysis Results
        </h1>
        <p className="text-sm text-slate-400 mt-1">
          Project: <span className="text-indigo-400 font-medium">{projectName}</span>
          {rejected > 0 && <span className="ml-2 text-red-400">· {rejected} rejected</span>}
        </p>
      </div>
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {cards.map(card => {
          const Icon = card.icon;
          return (
            <div key={card.label} className="relative overflow-hidden rounded-2xl border border-white/5 bg-white/[0.02] p-5">
              <div className={`absolute top-0 right-0 w-20 h-20 bg-gradient-to-br ${card.color} opacity-5 rounded-bl-full`} />
              <Icon className={`w-5 h-5 ${card.text} mb-2`} />
              <p className="text-3xl font-bold text-white">{card.value}</p>
              <p className="text-xs text-slate-500 mt-1">{card.label}</p>
            </div>
          );
        })}
      </div>
    </div>
  );
}
