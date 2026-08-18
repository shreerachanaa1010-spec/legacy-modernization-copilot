import { Link, useLocation } from 'react-router-dom';
import { Cpu, Home, BarChart3 } from 'lucide-react';
import { useStore } from '../store/useStore';

export function Navbar() {
  const location = useLocation();
  const { items } = useStore();
  const issueCount = items.length;

  const linkClass = (path: string) =>
    `flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-all duration-200 ${
      location.pathname === path
        ? 'bg-indigo-500/20 text-indigo-300 shadow-lg shadow-indigo-500/10'
        : 'text-slate-400 hover:text-slate-200 hover:bg-white/5'
    }`;

  return (
    <nav className="sticky top-0 z-50 backdrop-blur-xl bg-slate-950/70 border-b border-white/5">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          <Link to="/" className="flex items-center gap-3 group">
            <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center shadow-lg shadow-indigo-500/25 group-hover:shadow-indigo-500/40 transition-shadow">
              <Cpu className="w-5 h-5 text-white" />
            </div>
            <div>
              <span className="text-lg font-bold bg-gradient-to-r from-indigo-300 to-purple-300 bg-clip-text text-transparent">
                LMC
              </span>
              <span className="hidden sm:inline text-xs text-slate-500 ml-2">Legacy Modernization Copilot</span>
            </div>
          </Link>
          <div className="flex items-center gap-1">
            <Link to="/" className={linkClass('/')}>
              <Home className="w-4 h-4" />
              <span className="hidden sm:inline">Analyze</span>
            </Link>
            <Link to="/results" className={linkClass('/results')}>
              <BarChart3 className="w-4 h-4" />
              <span className="hidden sm:inline">Results</span>
              {issueCount > 0 && (
                <span className="ml-1 px-2 py-0.5 text-xs rounded-full bg-indigo-500/30 text-indigo-300 font-semibold">
                  {issueCount}
                </span>
              )}
            </Link>
          </div>
        </div>
      </div>
    </nav>
  );
}
