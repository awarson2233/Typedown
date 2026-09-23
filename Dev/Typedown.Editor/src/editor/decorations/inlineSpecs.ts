import type { Text } from '@codemirror/state';
import type { SyntaxNode, Tree } from '@lezer/common';

/**
 * 行内显形的决策层：给定语法树、文档、要覆盖的区间与「显形位置」（选区端点），
 * 产出与视图无关的纯数据描述；ViewPlugin 只负责把它们换成 Decoration。
 *
 * 规则（webview-wysiwyg-engine.md 第 1、4 节；wysiwyg-engine-survey.md 第 1、6.2 节）：
 * - 行内 span（强调、删除线、高亮、行内代码、链接、行内公式、转义）：任一显形位置落在 [from, to]（含两端）
 *   则标记变灰（`cm-td-syntax`），否则隐藏（replace，并登记 atomicRanges）。
 * - ATX 标题按 Vditor IR：显形位置在标题所在行内时 `#` 变灰，否则连同其后空白隐藏；标题字号按行无条件施加。
 * - 列表符号、任务框、引用 `>` 始终隐藏：列表符号换成圆点或编号 widget，任务项换成复选框 widget，
 *   引用改为按行的竖线装饰。
 * - 块级样式（标题字号、引用、代码块）只看语法，不随显形变化，避免行高跳动。
 */

export type InlineWidgetSpec =
  | { type: 'bullet'; depth: number }
  | { type: 'ordered'; text: string }
  | { type: 'task'; checked: boolean }
  | { type: 'inline-math'; src: string };

export type InlineSpec =
  | { kind: 'mark'; from: number; to: number; cls: string }
  | { kind: 'hide'; from: number; to: number }
  | { kind: 'widget'; from: number; to: number; widget: InlineWidgetSpec }
  | { kind: 'line'; at: number; cls: string; quoteDepth: number };

const SPAN_CLASS: Record<string, string> = {
  Emphasis: 'cm-td-em',
  StrongEmphasis: 'cm-td-strong',
  Strikethrough: 'cm-td-del',
  Highlight: 'cm-td-highlight',
  InlineCode: 'cm-td-code-inline',
};
const SPAN_MARK: Record<string, string> = {
  Emphasis: 'EmphasisMark',
  StrongEmphasis: 'EmphasisMark',
  Strikethrough: 'StrikethroughMark',
  Highlight: 'HighlightMark',
  InlineCode: 'CodeMark',
};
/** 在父节点处统一处理、遍历到时直接跳过的标记节点 */
const HANDLED_BY_PARENT = new Set(['EmphasisMark', 'StrikethroughMark', 'HighlightMark', 'CodeMark', 'LinkMark', 'LinkTitle', 'LinkLabel', 'TaskMarker', 'HeaderMark']);

export const SYNTAX = 'cm-td-syntax';

export interface Range { from: number; to: number }

export const revealedBy = (reveal: readonly number[], from: number, to: number) => {
  for (const p of reveal) if (p >= from && p <= to) return true;
  return false;
};

const isSpace = (c: string) => c === ' ' || c === '\t';

function children(node: SyntaxNode, name?: string): SyntaxNode[] {
  const out: SyntaxNode[] = [];
  for (let c = node.firstChild; c; c = c.nextSibling) if (!name || c.name === name) out.push(c);
  return out;
}

export function buildInlineSpecs(doc: Text, tree: Tree, range: Range, reveal: readonly number[]): InlineSpec[] {
  const out: InlineSpec[] = [];
  const lines = new Map<number, { cls: Set<string>; quote: number }>();
  const lineCls = (pos: number, cls: string | null, quoteDepth = 0) => {
    const at = doc.lineAt(pos).from;
    let e = lines.get(at);
    if (!e) lines.set(at, (e = { cls: new Set(), quote: 0 }));
    if (cls) e.cls.add(cls);
    if (quoteDepth > e.quote) e.quote = quoteDepth;
  };
  /** 对节点覆盖、且落在可见区间内的每一行调用 f */
  const eachLine = (from: number, to: number, f: (lineFrom: number, first: boolean, last: boolean) => void) => {
    const a = Math.max(from, range.from), b = Math.min(to, range.to);
    if (a > b) return;
    const firstLine = doc.lineAt(from).number, lastLine = doc.lineAt(to).number;
    for (let l = doc.lineAt(a); ; l = doc.line(l.number + 1)) {
      f(l.from, l.number === firstLine, l.number === lastLine);
      if (l.to >= b || l.number >= doc.lines) break;
    }
  };
  const mark = (from: number, to: number, cls: string) => { if (to > from) out.push({ kind: 'mark', from, to, cls }); };
  // ViewPlugin 的替换装饰不能跨行（CM6 的硬性限制）：跨行的标记（例如链接目标里有换行）退为变灰显示
  const crossesLine = (from: number, to: number) => doc.lineAt(from).to < to;
  const hide = (from: number, to: number) => {
    if (to <= from) return;
    if (crossesLine(from, to)) mark(from, to, SYNTAX);
    else out.push({ kind: 'hide', from, to });
  };
  const spaceAfter = (pos: number) => (pos < doc.length && isSpace(doc.sliceString(pos, pos + 1)) ? 1 : 0);
  let quoteDepth = 0;
  const quoteStack: number[] = [];

  tree.iterate({
    from: range.from,
    to: range.to,
    enter(ref) {
      const name = ref.name;
      if (ref.type.isTop || name === 'Body') return true;
      if (HANDLED_BY_PARENT.has(name)) return false;
      while (quoteStack.length && quoteStack[quoteStack.length - 1] <= ref.from) { quoteStack.pop(); quoteDepth--; }

      switch (name) {
        case 'Frontmatter':
          eachLine(ref.from, ref.to, at => lineCls(at, 'cm-td-frontmatter'));
          return false;
        case 'Table': {
          // 顶层表格由块组件 StateField 换成网格；嵌套在列表、引用里的表格暂按源码显示
          const node = ref.node;
          if (node.parent?.type.isTop) return false;
          return true;
        }
        case 'BlockMath':
          eachLine(ref.from, ref.to, at => lineCls(at, 'cm-td-math-src'));
          for (const m of children(ref.node, 'BlockMathMark')) mark(m.from, m.to, SYNTAX);
          return false;
        case 'FencedCode':
        case 'CodeBlock': {
          eachLine(ref.from, ref.to, (at, first, last) => {
            lineCls(at, 'cm-td-code');
            if (first) lineCls(at, 'cm-td-code-first');
            if (last) lineCls(at, 'cm-td-code-last');
          });
          for (const c of children(ref.node)) if (c.name === 'CodeMark' || c.name === 'CodeInfo') mark(c.from, c.to, SYNTAX);
          return false;
        }
        case 'HTMLBlock':
        case 'CommentBlock':
        case 'ProcessingInstructionBlock':
          eachLine(ref.from, ref.to, at => lineCls(at, 'cm-td-html'));
          return false;
        case 'Blockquote':
          quoteDepth++;
          quoteStack.push(ref.to);
          eachLine(ref.from, ref.to, at => lineCls(at, 'cm-td-quote', quoteDepth));
          return true;
        case 'QuoteMark':
          hide(ref.from, ref.to + spaceAfter(ref.to));
          return false;
        case 'ListMark': {
          const item = ref.node.parent;
          const list = item?.parent;
          const task = item?.getChild('Task');
          const marker = task?.getChild('TaskMarker');
          if (marker && marker.from - ref.to <= 4 && !crossesLine(ref.from, marker.to)) {
            const checked = /x/i.test(doc.sliceString(marker.from, marker.to));
            out.push({ kind: 'widget', from: ref.from, to: marker.to + spaceAfter(marker.to), widget: { type: 'task', checked } });
            return false;
          }
          const end = ref.to + spaceAfter(ref.to);
          if (list?.name === 'OrderedList') {
            out.push({ kind: 'widget', from: ref.from, to: end, widget: { type: 'ordered', text: doc.sliceString(ref.from, ref.to) } });
          } else {
            let depth = 0;
            for (let p = list; p; p = p.parent) if (p.name === 'BulletList' || p.name === 'OrderedList') depth++;
            out.push({ kind: 'widget', from: ref.from, to: end, widget: { type: 'bullet', depth } });
          }
          return false;
        }
        case 'ATXHeading1': case 'ATXHeading2': case 'ATXHeading3':
        case 'ATXHeading4': case 'ATXHeading5': case 'ATXHeading6': {
          const level = name.charCodeAt(10) - 48;
          const line = doc.lineAt(ref.from);
          lineCls(line.from, `cm-td-h cm-td-h${level}`);
          const shown = revealedBy(reveal, line.from, line.to);
          const marks = children(ref.node, 'HeaderMark');
          marks.forEach((m, i) => {
            if (shown) { mark(m.from, m.to, SYNTAX); return; }
            if (i === 0 && m.from === ref.from) {
              let end = m.to;
              while (end < line.to && isSpace(doc.sliceString(end, end + 1))) end++;
              hide(m.from, end);
            } else {
              let start = m.from;
              while (start > line.from && isSpace(doc.sliceString(start - 1, start))) start--;
              hide(start, m.to);
            }
          });
          return true;
        }
        case 'SetextHeading1': case 'SetextHeading2': {
          const level = name.endsWith('1') ? 1 : 2;
          const underline = ref.node.getChild('HeaderMark');
          eachLine(ref.from, ref.to, at => {
            if (underline && at === doc.lineAt(underline.from).from) lineCls(at, 'cm-td-setext-underline');
            else lineCls(at, `cm-td-h cm-td-h${level}`);
          });
          if (underline) mark(underline.from, underline.to, SYNTAX);
          return true;
        }
        case 'HorizontalRule': {
          const line = doc.lineAt(ref.from);
          lineCls(line.from, 'cm-td-hr');
          if (revealedBy(reveal, line.from, line.to)) mark(ref.from, ref.to, SYNTAX);
          else hide(ref.from, ref.to);
          return false;
        }
        case 'Emphasis': case 'StrongEmphasis': case 'Strikethrough': case 'Highlight': case 'InlineCode': {
          mark(ref.from, ref.to, SPAN_CLASS[name]);
          const shown = revealedBy(reveal, ref.from, ref.to);
          for (const m of children(ref.node, SPAN_MARK[name])) {
            if (shown) mark(m.from, m.to, SYNTAX); else hide(m.from, m.to);
          }
          return name !== 'InlineCode';
        }
        case 'Link': {
          const node = ref.node;
          const marks = children(node, 'LinkMark');
          if (marks.length < 2) return true;
          const shown = revealedBy(reveal, ref.from, ref.to);
          mark(marks[0].to, marks[1].from, 'cm-td-link');
          if (shown) {
            for (const c of children(node)) {
              if (c.name === 'LinkMark' || c.name === 'LinkTitle' || c.name === 'LinkLabel') mark(c.from, c.to, SYNTAX);
              else if (c.name === 'URL') mark(c.from, c.to, `${SYNTAX} cm-td-url`);
            }
          } else {
            hide(marks[0].from, marks[0].to);
            hide(marks[1].from, ref.to);
          }
          return true;
        }
        case 'Autolink': {
          const marks = children(ref.node, 'LinkMark');
          mark(ref.from, ref.to, 'cm-td-link');
          const shown = revealedBy(reveal, ref.from, ref.to);
          for (const m of marks) { if (shown) mark(m.from, m.to, SYNTAX); else hide(m.from, m.to); }
          return false;
        }
        case 'Image':
          // 图片 widget 属于 C2；C1 只把整段源码作为图片源码样式显示
          mark(ref.from, ref.to, 'cm-td-image-src');
          return false;
        case 'InlineMath': {
          const marks = children(ref.node, 'InlineMathMark');
          if (marks.length < 2) return false;
          if (revealedBy(reveal, ref.from, ref.to) || crossesLine(ref.from, ref.to)) {
            mark(ref.from, ref.to, 'cm-td-math-inline-src');
            for (const m of marks) mark(m.from, m.to, SYNTAX);
          } else {
            out.push({ kind: 'widget', from: ref.from, to: ref.to, widget: { type: 'inline-math', src: doc.sliceString(marks[0].to, marks[1].from) } });
          }
          return false;
        }
        case 'Escape':
          if (revealedBy(reveal, ref.from, ref.to)) mark(ref.from, ref.from + 1, SYNTAX);
          else hide(ref.from, ref.from + 1);
          return false;
        case 'URL': {
          // 链接内的 URL 已在父节点处理，这里只给 GFM 裸链接上样式
          const p = ref.node.parent?.name;
          if (p !== 'Link' && p !== 'Image' && p !== 'Autolink') mark(ref.from, ref.to, 'cm-td-link');
          return false;
        }
      }
      return true;
    },
  });

  for (const [at, e] of lines) {
    out.push({ kind: 'line', at, cls: [...e.cls].join(' '), quoteDepth: e.quote });
  }
  return out;
}
