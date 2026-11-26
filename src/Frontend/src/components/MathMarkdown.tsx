import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import remarkMath from 'remark-math'
import rehypeKatex from 'rehype-katex'
import 'katex/dist/katex.min.css'

/**
 * remark-math only understands $...$ and $$...$$. Models sometimes use \( \) and \[ \] anyway,
 * so convert those first.
 */
export function normalizeMathDelimiters(markdown: string): string {
  return markdown
    .replace(/\\\[([\s\S]*?)\\\]/g, (_, math) => `\n$$\n${math.trim()}\n$$\n`)
    .replace(/\\\(([\s\S]*?)\\\)/g, (_, math) => `$${math.trim()}$`)
}

export function MathMarkdown({ content }: { content: string }) {
  return (
    <div className="tutor-markdown">
      <ReactMarkdown
        remarkPlugins={[remarkGfm, remarkMath]}
        // throwOnError: false shows a formula that is still incomplete mid-stream in red instead of crashing.
        rehypePlugins={[[rehypeKatex, { throwOnError: false, strict: false }]]}
        components={{
          a: ({ node: _node, ...props }) => <a {...props} target="_blank" rel="noopener noreferrer" />,
        }}
      >
        {normalizeMathDelimiters(content)}
      </ReactMarkdown>
    </div>
  )
}
