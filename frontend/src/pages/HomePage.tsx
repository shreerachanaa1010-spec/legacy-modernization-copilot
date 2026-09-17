import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, Zap, Shield, GitBranch, ArrowRight, Loader2, Sparkles } from 'lucide-react';
import { useStore } from '../store/useStore';
import { runPipeline } from '../api';

export function HomePage() {
  const navigate = useNavigate();
  const { projectPath, setProjectPath, setPipelineResult, setLoading, setError, isLoading, error } = useStore();
  const [testProjectPath, setTestProjectPath] = useState('');

  async function handleAnalyze() {
    if (!projectPath.trim()) return;
    setLoading(true);
    setError(null);
    try {
      const result = await runPipeline(projectPath.trim(), testProjectPath.trim() || undefined);
      setPipelineResult(result);
      navigate('/results');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Analysis failed');
    } finally {
      setLoading(false);
    }
  }

  const features = [
    {
      icon: Search,
      title: 'Roslyn Analysis',
      desc: 'Deep AST scanning detects sync-over-async, WebClient usage, missing ConfigureAwait, and IDisposable anti-patterns.',
      gradient: 'from-blue-500 to-cyan-500',
    },
    {
      icon: Sparkles,
      title: 'AI Refactoring',
      desc: 'Gemini generates modern replacements with full explanations of why each pattern is problematic.',
      gradient: 'from-purple-500 to-pink-500',
    },
    {
      icon: Shield,
      title: 'Verified Safe',
      desc: 'Auto-generated xUnit tests validate refactored code before you merge. Every suggestion earns trust.',
      gradient: 'from-emerald-500 to-green-500',
    },
    {
      icon: GitBranch,
      title: 'Before / After',
      desc: 'Side-by-side diff viewer shows exactly what changes and why. Approve or reject each suggestion.',
      gradient: 'from-amber-500 to-orange-500',
    },
  ];

  return (
    <div className="flex flex-col items-center">
      {/* Hero */}
      <div className="text-center mt-8 mb-12 max-w-3xl">
        <div className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-indigo-500/10 border border-indigo-500/20 text-indigo-300 text-xs font-medium mb-6">
          <Zap className="w-3.5 h-3.5" />
          AI-Powered .NET Modernization
        </div>
        <h1 className="text-4xl sm:text-5xl lg:text-6xl font-bold tracking-tight mb-6">
          <span className="text-white">Modernize Legacy </span>
          <span className="bg-gradient-to-r from-indigo-400 via-purple-400 to-pink-400 bg-clip-text text-transparent">.NET Code</span>
          <span className="text-white"> with Confidence</span>
        </h1>
        <p className="text-lg text-slate-400 leading-relaxed max-w-2xl mx-auto">
          Point at any .NET project. Get AI-powered refactoring suggestions with automated verification.
          Every fix is tested before you touch a line of code.
        </p>
      </div>

      {/* Input card */}
      <div className="w-full max-w-2xl mb-16">
        <div className="rounded-2xl border border-white/10 bg-white/[0.03] p-6 backdrop-blur-sm shadow-2xl shadow-indigo-500/5">
          <label className="block text-sm font-medium text-slate-300 mb-2">
            Project Path
          </label>
          <div className="relative mb-3">
            <input
              type="text"
              placeholder="C:\path\to\YourProject.csproj"
              value={projectPath}
              onChange={e => setProjectPath(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleAnalyze()}
              disabled={isLoading}
              className="w-full px-4 py-3 rounded-xl bg-slate-900/80 border border-white/10 text-slate-200 placeholder-slate-600 focus:outline-none focus:ring-2 focus:ring-indigo-500/50 focus:border-indigo-500/50 transition-all text-sm font-mono disabled:opacity-50"
            />
          </div>
          <label className="block text-sm font-medium text-slate-300 mb-2">
            Test Project Path <span className="text-slate-600">(optional)</span>
          </label>
          <div className="relative mb-4">
            <input
              type="text"
              placeholder="C:\path\to\YourProject.Tests.csproj"
              value={testProjectPath}
              onChange={e => setTestProjectPath(e.target.value)}
              disabled={isLoading}
              className="w-full px-4 py-3 rounded-xl bg-slate-900/80 border border-white/10 text-slate-200 placeholder-slate-600 focus:outline-none focus:ring-2 focus:ring-indigo-500/50 focus:border-indigo-500/50 transition-all text-sm font-mono disabled:opacity-50"
            />
          </div>

          <button
            onClick={handleAnalyze}
            disabled={isLoading || !projectPath.trim()}
            className="w-full flex items-center justify-center gap-2 px-6 py-3 rounded-xl bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 text-white font-semibold text-sm shadow-lg shadow-indigo-500/25 hover:shadow-indigo-500/40 transition-all duration-300 disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:shadow-indigo-500/25"
          >
            {isLoading ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" />
                Running Full Pipeline...
              </>
            ) : (
              <>
                Analyze & Generate Fixes
                <ArrowRight className="w-4 h-4" />
              </>
            )}
          </button>

          {error && (
            <div className="mt-4 p-3 rounded-xl bg-red-500/10 border border-red-500/20 text-red-400 text-sm">
              {error}
            </div>
          )}
        </div>
      </div>

      {/* Feature grid */}
      <div className="w-full grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-16">
        {features.map(f => {
          const Icon = f.icon;
          return (
            <div key={f.title} className="rounded-2xl border border-white/5 bg-white/[0.02] p-6 hover:bg-white/[0.04] transition-colors">
              <div className={`w-10 h-10 rounded-xl bg-gradient-to-br ${f.gradient} flex items-center justify-center mb-4 shadow-lg`}>
                <Icon className="w-5 h-5 text-white" />
              </div>
              <h3 className="text-sm font-semibold text-white mb-2">{f.title}</h3>
              <p className="text-xs text-slate-500 leading-relaxed">{f.desc}</p>
            </div>
          );
        })}
      </div>
    </div>
  );
}
