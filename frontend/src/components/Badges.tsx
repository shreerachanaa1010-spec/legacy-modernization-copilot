import type { ReviewStatus } from '../types';
import { ShieldCheck, AlertTriangle, XCircle, Clock } from 'lucide-react';

const config: Record<string, { label: string; icon: typeof ShieldCheck; classes: string }> = {
  approved: {
    label: 'Verified Safe',
    icon: ShieldCheck,
    classes: 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30',
  },
  pending: {
    label: 'Needs Review',
    icon: Clock,
    classes: 'bg-amber-500/15 text-amber-400 border-amber-500/30',
  },
  rejected: {
    label: 'Rejected',
    icon: XCircle,
    classes: 'bg-red-500/15 text-red-400 border-red-500/30',
  },
};

export function StatusBadge({ status }: { status: ReviewStatus }) {
  const c = config[status];
  const Icon = c.icon;
  return (
    <span className={`inline-flex items-center gap-1.5 px-3 py-1 text-xs font-semibold rounded-full border ${c.classes}`}>
      <Icon className="w-3.5 h-3.5" />
      {c.label}
    </span>
  );
}

export function SeverityBadge({ severity }: { severity: string }) {
  const s = severity.toLowerCase();
  const cls =
    s === 'high' || s === 'error'
      ? 'bg-red-500/15 text-red-400 border-red-500/30'
      : s === 'medium' || s === 'warning'
        ? 'bg-amber-500/15 text-amber-400 border-amber-500/30'
        : 'bg-blue-500/15 text-blue-400 border-blue-500/30';

  const Icon = s === 'high' || s === 'error' ? XCircle : AlertTriangle;

  return (
    <span className={`inline-flex items-center gap-1.5 px-2.5 py-0.5 text-xs font-semibold rounded-full border ${cls}`}>
      <Icon className="w-3 h-3" />
      {severity}
    </span>
  );
}
