import { Language, syntaxTreeAvailable } from '@codemirror/language';
import type { EditorState, StateField } from '@codemirror/state';

interface LanguageStateInternals { context: { treeLen: number } }
const languageField = (Language as unknown as { state?: StateField<LanguageStateInternals> }).state;

/**
 * 语法树已完整覆盖到的文档长度。@codemirror/language 默认后台解析只推进到视口后 10 万字符，
 * 所以大文档上这个值通常小于文档长度；块组件只处理这之前的部分，解析推进后再补齐。
 * 优先读 ParseContext.treeLen（内部字段），读不到时用公开的 syntaxTreeAvailable 二分。
 */
export function parsedLength(state: EditorState): number {
  const len = state.doc.length;
  const field = languageField && state.field(languageField, false);
  if (field && typeof field.context?.treeLen === 'number') return Math.min(field.context.treeLen, len);
  if (syntaxTreeAvailable(state, len)) return len;
  let lo = 0, hi = len;
  while (lo < hi) {
    const mid = (lo + hi + 1) >> 1;
    if (syntaxTreeAvailable(state, mid)) lo = mid; else hi = mid - 1;
  }
  return lo;
}
