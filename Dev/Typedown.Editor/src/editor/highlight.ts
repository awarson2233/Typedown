import { HighlightStyle } from '@codemirror/language';
import { tags as t } from '@lezer/highlight';

/**
 * 代码高亮配色（围栏内嵌套语言）与源码模式下的 markdown 标记颜色。
 * 正文里的强调、标题等视觉由显形装饰的类名负责，这里不给 markdown 的 emphasis/heading 上样式，避免双重样式。
 */
export const codeHighlightStyle = HighlightStyle.define([
  { tag: [t.keyword, t.operatorKeyword, t.modifier, t.controlKeyword], color: 'var(--td-hl-keyword)' },
  { tag: [t.string, t.special(t.string), t.regexp], color: 'var(--td-hl-string)' },
  { tag: [t.comment, t.blockComment, t.lineComment], color: 'var(--td-hl-comment)', fontStyle: 'italic' },
  { tag: [t.number, t.bool, t.null, t.atom], color: 'var(--td-hl-number)' },
  { tag: [t.typeName, t.className, t.namespace], color: 'var(--td-hl-type)' },
  { tag: [t.function(t.variableName), t.function(t.propertyName)], color: 'var(--td-hl-function)' },
  { tag: [t.propertyName, t.attributeName], color: 'var(--td-hl-property)' },
  { tag: [t.tagName], color: 'var(--td-hl-keyword)' },
  { tag: [t.meta, t.processingInstruction], color: 'var(--td-syntax)' },
]);
