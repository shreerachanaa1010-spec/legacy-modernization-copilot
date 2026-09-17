import { CodeBlock } from './CodeBlock';

interface DiffViewerProps {
  originalCode: string;
  refactoredCode: string;
}

export function DiffViewer({ originalCode, refactoredCode }: DiffViewerProps) {
  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
      <div className="relative">
        <div className="absolute -top-3 left-4 z-10">
          <span className="px-3 py-1 text-xs font-bold rounded-full bg-red-500/20 text-red-400 border border-red-500/30 uppercase tracking-wider">
            Before — Legacy Code
          </span>
        </div>
        <div className="mt-2 ring-1 ring-red-500/20 rounded-xl">
          <CodeBlock code={originalCode} />
        </div>
      </div>
      <div className="relative">
        <div className="absolute -top-3 left-4 z-10">
          <span className="px-3 py-1 text-xs font-bold rounded-full bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 uppercase tracking-wider">
            After — Modern Code
          </span>
        </div>
        <div className="mt-2 ring-1 ring-emerald-500/20 rounded-xl">
          <CodeBlock code={refactoredCode} />
        </div>
      </div>
    </div>
  );
}
