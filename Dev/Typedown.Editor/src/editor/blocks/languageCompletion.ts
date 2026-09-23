import { autocompletion, type CompletionContext, type CompletionResult } from '@codemirror/autocomplete';
import { EditorState, type Extension } from '@codemirror/state';
import { syntaxTree } from '@codemirror/language';
import { languageOptions } from './codeLanguage';

/**
 * 代码块语言名的补全列表：光标在围栏代码首行的信息串上（或紧跟 ``` 之后）时，列出语言清单。
 * 列表留在页面内（docs/editor-protocol.md 第 7 节：XAML Flyout 会拿走键盘焦点、打断组字），样式照抄 Muya 的 codePicker。
 * 补全源挂在 languageData 上，不用 autocompletion 的 override，别的补全（:emoji: 等）可以各自再挂。
 */

export function languageCompletionSource(cx: CompletionContext): CompletionResult | null {
  const { state, pos } = cx;
  const line = state.doc.lineAt(pos);
  const m = /^( {0,3})(`{3,}|~{3,})([ \t]*)(\S*)$/.exec(line.text.slice(0, pos - line.from));
  if (!m) return null;
  // 必须真的是围栏代码的开围栏行（不是闭围栏、不是段落里的反引号）
  const node = syntaxTree(state).resolveInner(line.from, 1);
  let fenced = null;
  for (let n: typeof node | null = node; n; n = n.parent) if (n.name === 'FencedCode') { fenced = n; break; }
  if (!fenced || state.doc.lineAt(fenced.from).number !== line.number) return null;
  if (m[2][0] === '`' && line.text.slice(pos - line.from).includes('`')) return null;
  const from = pos - m[4].length;
  if (!cx.explicit && !m[4] && !cx.matchBefore(/[`~][ \t]*$/)) return null;
  return {
    from,
    to: from + (/^\S*/.exec(line.text.slice(from - line.from))?.[0].length ?? 0),
    options: languageOptions.map((o, i) => ({ label: o.label, displayLabel: o.name, boost: -i / 1000, type: 'language' })),
    validFor: /^[\w#+.-]*$/,
  };
}

export const languageCompletion: Extension = [
  EditorState.languageData.of(() => [{ autocomplete: languageCompletionSource }]),
  autocompletion({ icons: false, activateOnTyping: true, maxRenderedOptions: 30, tooltipClass: () => 'cm-td-picker' }),
];
