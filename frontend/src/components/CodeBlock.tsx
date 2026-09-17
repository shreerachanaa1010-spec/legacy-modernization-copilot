import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { oneDark } from 'react-syntax-highlighter/dist/esm/styles/prism';

interface CodeBlockProps {
  code: string;
  title?: string;
  language?: string;
}

export function CodeBlock({ code, title, language = 'csharp' }: CodeBlockProps) {
  return (
    <div className="rounded-xl overflow-hidden border border-white/5 bg-slate-900/50">
      {title && (
        <div className="px-4 py-2 bg-white/5 border-b border-white/5 text-xs font-medium text-slate-400 uppercase tracking-wider">
          {title}
        </div>
      )}
      <SyntaxHighlighter
        language={language}
        style={oneDark}
        customStyle={{
          margin: 0,
          padding: '1rem',
          background: 'transparent',
          fontSize: '0.8rem',
          lineHeight: '1.6',
        }}
        wrapLongLines
      >
        {code.trim()}
      </SyntaxHighlighter>
    </div>
  );
}
